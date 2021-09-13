namespace PlanningPoker.Domain

open System

open CommonTypes
open FSharp.UMX

[<Measure>]
type CardValue

type Card = string<CardValue>
type Cards = Card array

type Vote = { Card: Card; VotedAt: DateTime }

type VotedUser = {User: User; Duration: TimeSpan}

type VoteResult = { Percent: float; Voters: VotedUser array; }

type ActiveStory = { Votes: Map<User, Vote>; }

type ClosedStory =
    { Result: string<CardValue>
      Statistics: Map<Card, VoteResult>
      FinishedAt: DateTime }

type StoryState =
    | ActiveStory of ActiveStory
    | ClosedStory of ClosedStory
    static member createActive = ActiveStory { Votes = Map.empty }


type StartedState =
    | NotStarted
    | Started of dateTime: DateTime
    | Paused of TimeSpan

[<CLIMutable>]
type StoryObj =
    { Owner: User option
      Title: string
      State: StoryState
      Cards: Cards
      StartedAt: StartedState
    }

    static member zero =
        {
          Owner = None
          Title = String.Empty
          State = StoryState.createActive
          Cards = [||]
          StartedAt = NotStarted
        }

[<RequireQualifiedAccess>]
module Story =

    module Validation =

        let validateOwnerAccess user story =
            match story.Owner with
            | Some u when u = user -> Ok story
            | _ -> Error Errors.UnauthorizedAccess


        let validateActiveStoryState story =
            match story.State with
            | ActiveStory s -> Ok s
            | _ -> Error Errors.StoryIsClosed

        let validateVotesCount (state: ActiveStory) =
            if Map.isEmpty state.Votes then
                Error Errors.StoryHasNotVotes
            else
                Ok state

    type Command =
        | Configure of user: User * title: string * cards: Cards
        | CloseStory of user: User * finishedAt: DateTime
        | Vote of user: User * Card: Card * VotedAt: DateTime
        | RemoveVote of user: User
        | SetActive of user: User * startAt: DateTime
        | Clear of user: User * startAt: DateTime
        | Pause of user: User * timeStamp: DateTime

    type Event =
        | StoryConfigured of user: User * title: string * cards: Cards
        | StoryClosed of result: string<CardValue> * statistics: Map<Card, VoteResult> * finishedAt: DateTime
        | Voted of user: User * vote: Vote
        | VoteRemoved of user: User
        | ActiveSet of startedAt: DateTime
        | Cleared of startedAt: DateTime
        | Paused of duration: TimeSpan


    let makeVote user card votedAt cards =
        if Array.contains card cards then
            Ok
            <| Voted(user, { Card = card; VotedAt = votedAt })
        else
            Error Errors.UnexpectedCardValue

    let getActiveStartAt (startAt: DateTime) (story: StoryObj) =
        match story.StartedAt with
        | Started dt -> dt
        | StartedState.Paused dt -> startAt - dt
        | _ -> startAt

    let getPausedDuration (pausedAt: DateTime) (story: StoryObj) =
        match story.StartedAt with
        | Started dt -> pausedAt - dt
        | _ -> TimeSpan.Zero

    let calculateStatistics (story: ActiveStory) (Started startedAt) =
        if (story.Votes.Count = 0) then
            Map.ofArray [| % "", { Percent = 100.0; Voters = [||] } |], % ""
        else
            let stats =
                story.Votes
                |> Seq.groupBy (fun v -> v.Value.Card)
                |> Seq.map
                    (fun (card, items) ->
                        (card,
                         { Percent =
                               Math.Round(
                                   (Seq.length items |> float)
                                   / (story.Votes.Count |> float)
                                   * 100.0,
                                   1
                               )
                           Voters = items
                                    |> Seq.map (fun pair->  { User = pair.Key; Duration = pair.Value.VotedAt  - startedAt})
                                    |> Array.ofSeq }))

            let result =
                stats
                |> Seq.sortByDescending (fun v -> (snd v).Percent)
                |> Seq.head

            (stats |> Map.ofSeq, fst result)

    let validateCards (cards: Cards) =
        if cards.Length = 0 then
            Error Errors.CardsHasNotValues
        else
            let hasDuplicates =
                cards
                |> Array.groupBy id
                |> Array.map snd
                |> Array.map (fun v -> v.Length)
                |> Array.exists (fun v -> v > 1)

            if not hasDuplicates then
                Ok cards
            else
                Error Errors.CardsHasDuplicatesValues

    let producer (state: StoryObj) command =
        match command with
        | Configure (user, title, cards) ->
            validateCards cards
            |> Result.map (fun _ -> StoryConfigured(user, title, cards))

        | CloseStory (user, dt) ->
                    Validation.validateOwnerAccess user state
                    |> Result.bind Validation.validateActiveStoryState
                    |> Result.bind Validation.validateVotesCount
                    |> Result.map(fun s -> calculateStatistics s state.StartedAt)
                    |> Result.map(fun stats -> StoryClosed(snd stats, fst stats, dt))

        | Vote (user, card, votedAt) ->
           Validation.validateActiveStoryState state
           |> Result.bind (fun _ -> makeVote user card votedAt state.Cards)

        | RemoveVote user ->
            Validation.validateActiveStoryState state
            |> Result.map(fun _ -> VoteRemoved(user))

        | SetActive (user, startAt) ->
                    Validation.validateOwnerAccess user state
                    |> Result.map(fun _ -> getActiveStartAt startAt state |> ActiveSet )

        | Clear (user, startedAt) ->
                            Validation.validateOwnerAccess user state
                            |> Result.map(fun _ -> Cleared startedAt)

        | Pause (user, dt) ->
                            Validation.validateOwnerAccess user state
                            |> Result.map(getPausedDuration dt)
                            |> Result.map Paused

    let reducer (state: StoryObj) event =
        match event with
        | StoryConfigured (user, title, cards) ->
            { state with
                  Owner = Some user
                  Title = title
                  Cards = cards }

        | StoryClosed (result, stats, dt) ->
            { state with
                  StartedAt = NotStarted
                  State =
                      ClosedStory
                          { Result = result
                            Statistics = stats
                            FinishedAt = dt } }

        | Voted (user, vote) ->
            match state.State with
            | ActiveStory s ->
                { state with
                      State =
                          ActiveStory
                          <| { s with
                                   Votes = s.Votes.Add(user, vote) } }
            | _ -> state

        | VoteRemoved user ->
            match state.State with
            | ActiveStory s ->
                { state with
                      State =
                          ActiveStory
                          <| { s with Votes = s.Votes.Remove user } }
            | _ -> state

        | ActiveSet dt ->
            match state.State with
            | ClosedStory _ -> state
            |  _ ->
                { state with
                      StartedAt = Started dt
                }

        | Cleared dt ->
            match state.State with
            | ClosedStory _ ->
                { state with
                      State = StoryState.createActive
                      StartedAt = Started dt
                }
            | _ -> state

        | Paused duration ->
            {state with StartedAt = StartedState.Paused duration}


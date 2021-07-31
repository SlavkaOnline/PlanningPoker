namespace PlanningPoker.Domain

open System

open CommonTypes
open PlanningPoker.Domain
open FSharp.UMX


[<Measure>]
type CardValue

type Card = string<CardValue>
type Cards = Card array

type Vote = { Card: Card; VotedAt: DateTime }

type VoteResult = { Percent: float; Voters: User list }

type ActiveStory = { Votes: Map<User, Vote> }

type ClosedStory =
    { Result: string<CardValue>
      Statistics: Map<Card, VoteResult>
      FinishedAt: DateTime }

type StoryState =
    | ActiveStory of ActiveStory
    | ClosedStory of ClosedStory
    static member createActive = ActiveStory { Votes = Map.empty }

[<CLIMutable>]
type StoryObj =
    { Owner: User option
      Title: string
      State: StoryState
      Cards: Cards
      StartedAt: DateTime }
    member this.hasAccessToChange user =
        match this.Owner with
        | Some u -> u = user
        | None -> false

    static member zero =
        { Owner = None
          Title = String.Empty
          State = StoryState.createActive
          Cards = [||]
          StartedAt = DateTime.MinValue }

[<RequireQualifiedAccess>]
module Story =

    type Command =
        | StartStory of user: User * title: string * cards: Cards * startedAt: DateTime
        | CloseStory of user: User * finishedAt: DateTime
        | Vote of user: User * Card: Card * VotedAt: DateTime
        | RemoveVote of user: User
        | Clear of user: User * startedAt: DateTime

    type Event =
        | StoryStarted of user: User * title: string * cards: Cards * startedAt: DateTime
        | StoryClosed of result: string<CardValue> * statistics: Map<Card, VoteResult> * finishedAt: DateTime
        | Voted of user: User * vote: Vote
        | VoteRemoved of user: User
        | Cleared of startedAt: DateTime


    let makeVote user card votedAt cards =
        if Array.contains card cards then
            Ok
            <| Voted(user, { Card = card; VotedAt = votedAt })
        else
            Error Errors.UnexpectedCardValue

    let calculateStatistics (story: ActiveStory) =
        if (story.Votes.Count = 0) then
            Map.ofArray [| % "", { Percent = 100.0; Voters = [] } |], % ""
        else
            let stats =
                story.Votes
                |> Seq.groupBy (fun v -> v.Value.Card)
                |> Seq.map (fun (card, items) -> (card, items |> Seq.map (fun i -> i.Key)))
                |> Seq.map
                    (fun (card, users) ->
                        (card,
                         { Percent =
                               Math.Round(
                                   (Seq.length users |> float)
                                   / (story.Votes.Count |> float)
                                   * 100.0,
                                   1
                               )
                           Voters = List.ofSeq users }))

            let result =
                stats
                |> Seq.sortByDescending (fun v -> (snd v).Percent)
                |> Seq.head

            (stats |> Map.ofSeq, fst result)

    let closeStory user closedAt (story: StoryObj) =
        if story.hasAccessToChange user then
            match story.State with
            | ActiveStory s ->
                if s.Votes.Count > 0 then
                    let stats = calculateStatistics s
                    Ok <| StoryClosed(snd stats, fst stats, closedAt)
                else
                    Error <| Errors.StoryHasNotVotes
            | ClosedStory _ -> Error <| Errors.StoryIsClosed
        else
            Error <| Errors.UnauthorizedAccess

    let clear user closedAt (story: StoryObj) =
        if story.hasAccessToChange user then
            match story.State with
            | ClosedStory _ -> Ok <| Cleared closedAt
            | ActiveStory _ -> Error <| Errors.StoryIsNotClosed
        else
            Error <| Errors.UnauthorizedAccess

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
        | StartStory (user, title, cards, dt) ->
            validateCards cards
            |> Result.map (fun _ -> StoryStarted(user, title, cards, dt))

        | CloseStory (user, dt) -> closeStory user dt state
        | Vote (user, card, votedAt) ->
            match state.State with
            | ActiveStory _ -> makeVote user card votedAt state.Cards
            | ClosedStory _ -> Error <| Errors.StoryIsClosed

        | RemoveVote user ->
            match state.State with
            | ActiveStory _ -> Ok <| VoteRemoved(user)
            | ClosedStory _ -> Error <| Errors.StoryIsClosed

        | Clear (user, dt) -> clear user dt state


    let reducer (state: StoryObj) event =
        match event with
        | StoryStarted (user, title, cards, dt) ->
            { state with
                  Owner = Some user
                  Title = title
                  Cards = cards
                  StartedAt = dt }
        | StoryClosed (result, stats, dt) ->
            { state with
                  State =
                      ClosedStory
                          { Result = result
                            Statistics = stats
                            FinishedAt = dt } }
        | Voted (user, vote) ->
            match state.State with
            | ClosedStory _ -> state
            | ActiveStory s ->
                { state with
                      State =
                          ActiveStory
                          <| { s with
                                   Votes = s.Votes.Add(user, vote) } }

        | VoteRemoved user ->
            match state.State with
            | ClosedStory _ -> state
            | ActiveStory s ->
                { state with
                      State =
                          ActiveStory
                          <| { s with Votes = s.Votes.Remove user } }

        | Cleared dt ->
            match state.State with
            | ClosedStory _ ->
                { state with
                      State = StoryState.createActive
                      StartedAt = dt }
            | ActiveStory _ -> state

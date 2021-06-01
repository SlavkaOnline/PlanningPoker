namespace PlanningPoker.Domain

open System
open System.Collections.Generic

open CommonTypes

type Card =
    | XXS
    | XS
    | S
    | M
    | L
    | XL
    | XXL
    | Question

type Vote = { Card: Card; VotedAt: DateTime }

type VoteResult = { Percent: float; Voters: User list }

type ActiveStory = { Votes: Map<User, Vote> }

type ClosedStory =
    { Result: Card
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
      StartedAt: DateTime }
    member this.hasAccessToChange user =
        match this.Owner with
        | Some u -> u = user
        | None -> false

    static member zero =
        { Owner = None
          Title = String.Empty
          State = StoryState.createActive
          StartedAt = DateTime.MinValue }

[<RequireQualifiedAccess>]
module Story =

    type Command =
        | StartStory of user: User * title: string * startedAt: DateTime
        | CloseStory of user: User * finishedAt: DateTime
        | Vote of user: User * Card: Card * VotedAt: DateTime
        | RemoveVote of user: User

    type Event =
        | StoryStarted of user: User * title: string * startedAt: DateTime
        | StoryClosed of result: Card * statistics: Map<Card, VoteResult> * finishedAt: DateTime
        | Voted of user: User * vote: Vote
        | VoteRemoved of user: User


    let makeVote user vote (story: ActiveStory) =
        { story with
              Votes = story.Votes.Add(user, vote) }

    let removeVote user (story: ActiveStory) =
        { story with
              Votes = story.Votes.Remove user }

    let calculateStatistics (story: ActiveStory) =
        let votes =
            story.Votes
            |> Seq.map (fun kv -> kv.Value.Card)
            |> Seq.distinct
            |> Seq.map (fun c -> (c, { Percent = 0.0; Voters = [] }))

        let stats =
            story.Votes
            |> Seq.fold
                (fun (state: Dictionary<Card, VoteResult>) vote ->
                    let voted = state.[vote.Value.Card]

                    let percent =
                        Math.Round((voted.Percent + 1.0) / (float story.Votes.Count) * 100.0, 1)

                    let voters = vote.Key :: voted.Voters
                    state.[vote.Value.Card] <- { Percent = percent; Voters = voters }

                    state)
                (votes |> dict |> Dictionary)
            |> Seq.map (|KeyValue|)

        let result =
            stats
            |> Seq.sortByDescending (fun v -> (snd v).Percent)
            |> Seq.head

        (stats |> Map.ofSeq, fst result)

    let producer (state: StoryObj) command =
        match command with
        | StartStory (user, title, dt) -> Ok <| StoryStarted(user, title, dt)
        | CloseStory (user, dt) ->
            if state.hasAccessToChange user then
                match state.State with
                | ActiveStory s ->
                    if s.Votes.Count > 0 then
                        let stats = calculateStatistics s
                        Ok <| StoryClosed(snd stats, fst stats, dt)
                    else
                        Error <| Errors.StoryHasntVotes
                | ClosedStory _ -> Error <| Errors.StoryIsClosed
            else
                Error <| Errors.UnauthorizedAccess
        | Vote (user, card, votedAt) ->
            match state.State with
            | ActiveStory _ ->
                Ok
                <| Voted(user, { Card = card; VotedAt = votedAt })
            | ClosedStory _ -> Error <| Errors.StoryIsClosed

        | RemoveVote user ->
            match state.State with
            | ActiveStory _ -> Ok <| VoteRemoved(user)
            | ClosedStory _ -> Error <| Errors.StoryIsClosed

    let reducer (state: StoryObj) event =
        match event with
        | StoryStarted (user, title, dt) ->
            { state with
                  Owner = Some user
                  Title = title
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
                      State = ActiveStory(makeVote user vote s) }

        | VoteRemoved user ->
            match state.State with
            | ClosedStory _ -> state
            | ActiveStory s ->
                { state with
                      State = ActiveStory(removeVote user s) }
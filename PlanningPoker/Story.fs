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

type ActiveStory = { Votes: Map<User, Vote> }

type ClosedStory =
    { Votes: Map<User, Vote>
      Statistics: Map<Card, float>
      FinishedAt: DateTime }

type StoryState =
    | ActiveStory of ActiveStory
    | ClosedStory of ClosedStory
    static member createActive =
        ActiveStory { Votes = Map.empty }

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
        | StoryClosed of votes: Map<User, Vote> * statistics: Map<Card, float> * finishedAt: DateTime
        | Voted of user: User * vote: Vote
        | VoteRemoved of user: User


    let makeVote user vote (story: ActiveStory) =
        { story with Votes = story.Votes.Add (user, vote)}

    let removeVote user (story: ActiveStory) =
        { story with Votes = story.Votes.Remove user}

    let calculateStatistics (story: ActiveStory) =
        let votes =
            story.Votes
            |> Seq.map (fun kv -> kv.Value.Card)
            |> Seq.distinct
            |> Seq.map (fun c -> (c, 0.0))

        story.Votes
        |> Seq.fold
            (fun (state: IDictionary<Card, float>) vote ->
                state.[vote.Value.Card] <-
                    Math.Round(
                        (state.[vote.Value.Card] + 1.0)
                        / (float story.Votes.Count),
                        1
                    )

                state)
            (votes |> dict)
        |> Seq.map (|KeyValue|)
        |> Map.ofSeq

    let producer (state: StoryObj) command =
        match command with
        | StartStory (user, title, dt) -> Ok <| StoryStarted(user, title, dt)
        | CloseStory (user, dt) ->
            if state.hasAccessToChange user then
                match state.State with
                | ActiveStory s ->
                    let stats = calculateStatistics s
                    Ok <| StoryClosed(s.Votes, stats, dt)
                | ClosedStory _ -> Error <| Errors.StoryIsClosed
            else
                Error <| Errors.UnauthorizedAccess
        | Vote (user, card, votedAt) ->
            match state.State with
            | ActiveStory _ -> Ok <| Voted(user, {Card = card; VotedAt = votedAt})
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
        | StoryClosed (votes, stats, dt) ->
            { state with
                  State =
                      ClosedStory
                          { Votes = votes
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
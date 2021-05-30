namespace Gateway

open System
open System.Collections.Generic
open PlanningPoker.Domain
open FSharp.UMX
open Microsoft.FSharp.Reflection
open PlanningPoker.Domain
open PlanningPoker.Domain
open PlanningPoker.Domain
open PlanningPoker.Domain
open PlanningPoker.Domain
open PlanningPoker.Domain

module Views =
    let toString (x: 'a) =
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

    let fromString<'a> (s: string) =
        match FSharpType.GetUnionCases typeof<'a>
              |> Array.filter (fun case -> case.Name = s) with
        | [| case |] -> Some(FSharpValue.MakeUnion(case, [||]) :?> 'a)
        | _ -> None


    type ParticipantView = { Id: Guid; Name: string }

    type UserView = { Id: Guid; Name: string }

    type VoteResultView =
        { Percent: float
          Voters: UserView array }

    type SessionView =
        { Id: Guid
          Title: string
          Version: int32
          OwnerId: Guid
          OwnerName: string
          ActiveStory: string
          Participants: ParticipantView array
          Stories: Guid array }
        static member create (id: Guid) (version: int32) (session: SessionObj) =
            { Id = id
              Title = session.Title
              Version = version
              OwnerId = %session.Owner.Value.Id
              OwnerName = session.Owner.Value.Name
              ActiveStory =
                  session.ActiveStory
                  |> Option.map (fun id -> (%id).ToString())
                  |> Option.defaultValue Unchecked.defaultof<_>
              Participants =
                  session.Participants
                  |> List.map
                      (fun p ->
                          { ParticipantView.Id = (%p.Id)
                            Name = p.Name })
                  |> List.toArray
              Stories =
                  session.Stories
                  |> List.map UMX.untag
                  |> List.toArray }

    type StoryView =
        { Id: Guid
          Title: string
          Version: int32
          OwnerId: Guid
          OwnerName: string
          IsClosed: bool
          Voted: UserView array
          Result: string
          Statistics: Dictionary<string, VoteResultView>
          StartedAt: DateTime
          FinishedAt: DateTime Nullable }
        static member create (id: Guid) (version: int32) (story: StoryObj) : StoryView =
            { Id = id
              Title = story.Title
              Version = version
              OwnerId = %story.Owner.Value.Id
              OwnerName = %story.Owner.Value.Name
              IsClosed =
                  match story.State with
                  | ActiveStory _ -> false
                  | ClosedStory _ -> true
              Result =
                  match story.State with
                  | ActiveStory _ -> Unchecked.defaultof<_>
                  | ClosedStory s -> toString s.Result
              Voted =
                  match story.State with
                  | ActiveStory s ->
                      s.Votes
                      |> Map.toSeq
                      |> Seq.map (fst)
                      |> Seq.map (fun v -> { UserView.Id = %v.Id; Name = v.Name })
                      |> Seq.toArray
                  | ClosedStory s ->
                      seq {
                          for st in s.Statistics |> Map.toSeq do
                              let results = snd st

                              for v in results.Voters do
                                  { UserView.Id = %v.Id; Name = v.Name }
                      }
                      |> Array.ofSeq

              Statistics =
                  match story.State with
                  | ActiveStory s -> Dictionary()
                  | ClosedStory s ->
                      s.Statistics
                      |> Map.toSeq
                      |> Seq.map
                          (fun s ->
                              (toString (fst s),
                               { VoteResultView.Percent = (snd s).Percent
                                 Voters =
                                     (snd s).Voters
                                     |> List.map (fun v -> { UserView.Id = %v.Id; Name = v.Name })
                                     |> Array.ofList }))
                      |> dict
                      |> Dictionary

              StartedAt = story.StartedAt

              FinishedAt =
                  match story.State with
                  | ActiveStory _ -> Unchecked.defaultof<DateTime Nullable>
                  | ClosedStory s -> Nullable s.FinishedAt

            }

    type EventView<'TPayload> = { Order: int32; Payload: 'TPayload }
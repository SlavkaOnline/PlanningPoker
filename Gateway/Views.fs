namespace Gateway

open System
open System.Collections.Generic
open PlanningPoker.Domain
open FSharp.UMX
open Microsoft.FSharp.Reflection
open PlanningPoker.Domain.CommonTypes

module Views =
    let toString (x: 'a) =
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

    let fromString<'a> (s: string) =
        match FSharpType.GetUnionCases typeof<'a>
              |> Array.filter (fun case -> String.Equals(case.Name, s, StringComparison.OrdinalIgnoreCase)) with
        | [| case |] -> Some(FSharpValue.MakeUnion(case, [||]) :?> 'a)
        | _ -> None


    type ParticipantView = { Id: Guid; Name: string; Picture: string }

    type VoteResultView =
        { Percent: float
          Voters: ParticipantView array }

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
                          { Id = (%p.Id)
                            Name = p.Name
                            Picture = p.Picture |> Option.defaultValue ""
                            })
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
          UserCard: string
          IsClosed: bool
          Voted: ParticipantView array
          Result: string
          Statistics: Dictionary<string, VoteResultView>
          StartedAt: DateTime
          FinishedAt: DateTime Nullable }
        static member create (id: Guid) (version: int32) (story: StoryObj) (user: User) : StoryView =
            { Id = id
              Title = story.Title
              Version = version
              OwnerId = %story.Owner.Value.Id
              OwnerName = %story.Owner.Value.Name
              UserCard =
                  match story.State with
                  | ActiveStory s ->
                      s.Votes.TryFind user
                      |> Option.map (fun v -> toString v.Card)
                      |> Option.defaultValue ""
                  | ClosedStory s ->
                      s.Statistics
                      |> Map.toSeq
                      |> Seq.filter (fun s -> List.contains user (snd s).Voters)
                      |> Seq.tryHead
                      |> Option.map (fun v -> toString (fst v))
                      |> Option.defaultValue ""
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
                      |> Seq.map
                          (fun v ->
                              { Id = %v.Id
                                Name = v.Name
                                Picture = v.Picture |> Option.defaultValue "" })
                      |> Seq.toArray
                  | ClosedStory s ->
                      seq {
                          for st in s.Statistics |> Map.toSeq do
                              let results = snd st

                              for v in results.Voters do
                                  { Id = %v.Id
                                    Name = v.Name
                                    Picture = v.Picture |> Option.defaultValue "" }
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
                                     |> List.map
                                         (fun v ->
                                             { Id = %v.Id
                                               Name = v.Name
                                               Picture = v.Picture |> Option.defaultValue "" })
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

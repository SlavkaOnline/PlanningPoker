namespace Gateway

open System
open System.Collections.Generic
open PlanningPoker.Domain
open FSharp.UMX
open Microsoft.FSharp.Reflection

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

    type SessionView =
        { Id: Guid
          Version: int32
          OwnerId: Guid
          OwnerName: string
          Participants: ParticipantView array
          Stories: Guid array }
        static member create (id: Guid) (version: int32) (session: SessionObj) =
            { Id = id
              Version = version
              OwnerId = %session.Owner.Value.Id
              OwnerName = session.Owner.Value.Name
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
          Version: int32
          OwnerId: Guid
          OwnerName: string
          Voted: UserView array
          Statistics: Dictionary<string, float>
          StartedAt: DateTime
          FinishedAt: DateTime Nullable }
        static member create (id: Guid) (version: int32) (story: StoryObj) : StoryView =
            { Id = id
              Version = version
              OwnerId = %story.Owner.Value.Id
              OwnerName = %story.Owner.Value.Name
              Voted =
                  match story.State with
                  | ActiveStory s ->
                      s.Votes
                      |> Map.toSeq
                      |> Seq.map (fst)
                      |> Seq.map (fun v -> { UserView.Id = %v.Id; Name = v.Name })
                      |> Seq.toArray
                  | ClosedStory s ->
                      s.Votes
                      |> Map.toSeq
                      |> Seq.map (fst)
                      |> Seq.map (fun v -> { UserView.Id = %v.Id; Name = v.Name })
                      |> Seq.toArray

              Statistics =
                  match story.State with
                  | ActiveStory s -> Dictionary()
                  | ClosedStory s ->
                      s.Statistics
                      |> Map.toSeq
                      |> Seq.map (fun s -> (toString (fst s), (snd s)))
                      |> dict
                      |> Dictionary

              StartedAt = story.StartedAt

              FinishedAt =
                  match story.State with
                  | ActiveStory _ -> Unchecked.defaultof<DateTime Nullable>
                  | ClosedStory s -> Nullable s.FinishedAt

            }
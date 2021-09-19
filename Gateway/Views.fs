namespace Gateway

open System
open System.Collections.Generic
open PlanningPoker.Domain
open FSharp.UMX
open PlanningPoker.Domain.CommonTypes

module Views =

    type ParticipantView =
        { Id: Guid
          Name: string
          Picture: string
          GroupId: Guid
           }

    type VotedParticipantView =
        {
            Name: string
            Duration: String
        }

    type VoteResultView =
        { Percent: float
          Voters: VotedParticipantView array }

    type CardsType = { Id: string; Caption: string }

    type GroupView =
        {
            Id: Guid
            Name: string
        }

    type SessionView =
        { Id: Guid
          Title: string
          Version: int32
          OwnerId: Guid
          OwnerName: string
          ActiveStory: string
          Participants: ParticipantView array
          Groups: GroupView array
          DefaultGroupId: Guid
          Stories: Guid array }
        static member create (id: Guid) (version: int32) (session: SessionObj) =
            { Id = id
              Title = session.Title
              Version = version
              OwnerId = %session.Owner.Value.Id
              OwnerName = session.Owner.Value.Name
              DefaultGroupId = %session.DefaultGroupId
              Groups = session.Groups
                       |> List.map(fun g -> {
                           Id = %g.Id
                           Name = g.Name
                       })
                       |> Array.ofList
              ActiveStory =
                  session.ActiveStory
                  |> Option.map (fun id -> (%id).ToString())
                  |> Option.defaultValue Unchecked.defaultof<_>
              Participants =
                  session.Participants
                  |> Map.toList
                  |> List.map snd
                  |> List.map
                      (fun p ->
                          { Id = %p.User.Id
                            Name = p.User.Name
                            Picture = p.User.Picture |> Option.defaultValue ""
                            GroupId = %p.GroupId
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
          Cards: string array
          IsClosed: bool
          Voted: Guid array
          Result: string
          Statistics: Dictionary<string, VoteResultView>
          StartedAt: DateTime Nullable
          Duration: string }
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
                      |> Option.map (fun v -> %v.Card)
                      |> Option.defaultValue ""
                  | ClosedStory s ->
                      s.Statistics
                      |> Map.toSeq
                      |> Seq.filter (fun s -> Array.contains user ((snd s).Voters |> Array.map(fun v -> v.User)))
                      |> Seq.tryHead
                      |> Option.map (fun v -> %(fst v))
                      |> Option.defaultValue ""

              Cards = story.Cards |> Array.map UMX.untag
              IsClosed =
                  match story.State with
                  | ClosedStory _ -> true
                  |  _ -> false
              Result =
                  match story.State with
                  | ClosedStory s -> %s.Result
                  |  _ -> Unchecked.defaultof<_>
              Voted =
                  match story.State with
                  | ActiveStory s ->
                      s.Votes
                      |> Map.toSeq
                      |> Seq.map (fst)
                      |> Seq.map(fun v -> %v.Id)
                      |> Seq.toArray
                  | ClosedStory s ->
                      seq {
                          for st in s.Statistics |> Map.toSeq do
                              let results = snd st

                              for v in results.Voters ->
                                  %v.User.Id

                      }
                      |> Array.ofSeq

              Statistics =
                  match story.State with
                  | ClosedStory s ->
                      s.Statistics
                      |> Map.toSeq
                      |> Seq.map
                          (fun s ->
                              (%(fst s),
                               { VoteResultView.Percent = (snd s).Percent
                                 Voters =
                                     (snd s).Voters
                                     |> Array.map
                                         (fun v ->
                                             { Name = v.User.Name
                                               Duration = v.Duration.ToString(@"hh\:mm\:ss") })
                                     }))
                      |> dict
                      |> Dictionary
                  | _ -> Dictionary()

              StartedAt =
                  match story.StartedAt with
                  | Started dt -> Nullable dt
                  | _ -> Unchecked.defaultof<DateTime Nullable>

              Duration =
                  match (story.State, story.StartedAt)  with
                  | ClosedStory(c), Started s -> (c.FinishedAt - s).ToString(@"hh\:mm\:ss")
                  | _ -> Unchecked.defaultof<string>

            }

    type EventView<'TPayload> = { Order: int32; Payload: 'TPayload }

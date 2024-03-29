namespace Gateway

open System
open System.Collections.Generic
open PlanningPoker.Domain
open FSharp.UMX
open PlanningPoker.Domain.CommonTypes

module Views =

    type AuthUser = { Token: string }


    type Participant =
        { Id: Guid
          Name: string
          Picture: string
          GroupId: Guid }

    type ChatUser =
        { Id: Guid
          Name: string
          Picture: string }

    type VotedParticipant = { Name: string; Duration: String }

    type VoteResult =
        { Percent: float
          Voters: VotedParticipant array }

    type CardsType = { Id: string; Caption: string }

    type Group = { Id: Guid; Name: string }

    type Statistics =
        { Id: Nullable<Guid>
          Result: Dictionary<string, VoteResult> }

    type Session =
        { Id: Guid
          Title: string
          Version: int32
          OwnerId: Guid
          OwnerName: string
          ActiveStory: string
          Participants: Participant array
          Groups: Group array
          DefaultGroupId: Guid
          Stories: Guid array }
        static member create (id: Guid) (version: int32) (session: SessionObj) =
            { Id = id
              Title = session.Title
              Version = version
              OwnerId = %session.Owner.Value.Id
              OwnerName = session.Owner.Value.Name
              DefaultGroupId = %session.DefaultGroupId
              Groups =
                  session.Groups
                  |> List.map (fun g -> { Id = %g.Id; Name = g.Name })
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
                          { Id = %p.Id
                            Name = p.Name
                            Picture = p.Picture |> Option.defaultValue ""
                            GroupId =
                                %(session.UserGroupMap.TryFind p.Id
                                  |> Option.defaultValue session.DefaultGroupId) })
                  |> List.toArray
              Stories =
                  session.Stories
                  |> List.map UMX.untag
                  |> List.toArray }

    type Story =
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
          Statistics: Statistics array
          StartedAt: DateTime Nullable
          Duration: string }
        static member create (id: Guid) (version: int32) (story: StoryObj) (user: User) : Story =
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
                      fst s.Statistics.[0].Result
                      |> Map.toSeq
                      |> Seq.filter (fun s -> Array.contains user ((snd s).Voters |> Array.map (fun v -> v.User)))
                      |> Seq.tryHead
                      |> Option.map (fun v -> %(fst v))
                      |> Option.defaultValue ""

              Cards = story.Cards |> Array.map UMX.untag
              IsClosed =
                  match story.State with
                  | ClosedStory _ -> true
                  | _ -> false
              Result =
                  match story.State with
                  | ClosedStory s -> %s.Result
                  | _ -> Unchecked.defaultof<_>
              Voted =
                  match story.State with
                  | ActiveStory s ->
                      s.Votes
                      |> Map.toSeq
                      |> Seq.map (fst)
                      |> Seq.map (fun v -> %v.Id)
                      |> Seq.toArray
                  | ClosedStory s ->
                      seq {
                          for st in fst s.Statistics.[0].Result |> Map.toSeq do
                              let results = snd st

                              for v in results.Voters -> %v.User.Id

                      }
                      |> Array.ofSeq

              Statistics =
                  match story.State with
                  | ClosedStory s ->
                      s.Statistics
                      |> Array.map
                          (fun s ->
                              { Statistics.Id =
                                    s.Id
                                    |> Option.map Nullable
                                    |> Option.defaultValue (Unchecked.defaultof<Guid Nullable>)
                                Result =
                                    fst s.Result
                                    |> Map.toSeq
                                    |> Seq.map
                                        (fun m ->
                                            (%(fst m),
                                             { VoteResult.Percent = (snd m).Percent
                                               Voters =
                                                   (snd m).Voters
                                                   |> Array.map
                                                       (fun v ->
                                                           { Name = v.User.Name
                                                             Duration = v.Duration.ToString(@"hh\:mm\:ss") }) }))
                                    |> dict
                                    |> Dictionary })
                  | _ -> Array.empty

              StartedAt =
                  match story.StartedAt with
                  | Started dt -> Nullable dt
                  | _ -> Unchecked.defaultof<DateTime Nullable>

              Duration =
                  match story.State with
                  | ClosedStory _ -> story.Duration.ToString(@"hh\:mm\:ss")
                  | _ -> TimeSpan.Zero.ToString(@"hh\:mm\:ss")

            }

    type Event<'TPayload> = { Order: int32; Payload: 'TPayload }

    type ChatMessage =
        { Id: string
          Group: string
          User: ChatUser
          Text: string }

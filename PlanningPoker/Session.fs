namespace PlanningPoker.Domain

open System
open CommonTypes
open FSharp.UMX

[<Measure>]
type GroupId

type Group = {
    Id: Guid<GroupId>
    Name: string
}

type Participant = {
    User: User
    GroupId: Guid<GroupId>
}


[<CLIMutable>]
type SessionObj =
    { Title: string
      Owner: User option
      Participants:  Map<Guid<UserId>,Participant>
      Groups: Group list
      DefaultGroupId: Guid<GroupId>
      ActiveStory: Guid<StoryId> option
      Stories: Guid<StoryId> list }

    static member zero() =
        let defaultGroupId = Guid.NewGuid()
        { Title = ""
          Owner = None
          ActiveStory = None
          DefaultGroupId = %defaultGroupId
          Groups = [{Name = "Others"; Id = %defaultGroupId}]
          Participants = Map.empty
          Stories = [] }


[<RequireQualifiedAccess>]
module Session =

    module Validation =

         let validateOwnerAccess user session =
            match session.Owner with
            | Some u when u = user -> Ok session
            | _ -> Error Errors.UnauthorizedAccess

         let validateGroupNotExists groupName groups =
            if List.exists (fun g -> g.Name = groupName) groups then
                Error Errors.GroupAlreadyExist
            else
                Ok ()

         let validateGroupCanRemoved groupId session =
            match List.tryFind (fun g -> g.Id = groupId) session.Groups with
            | Some g when g.Id <> session.DefaultGroupId -> Ok g
            | Some g when g.Id = session.DefaultGroupId -> Error Errors.CantRemoveGroup
            | _ -> Error Errors.GroupNotExist

         let validateParticipantExist userId (participants: Map<_,_>) =
             participants.TryFind userId
             |> Option.map(fun p -> Ok p.User)
             |> Option.defaultValue (Error <| Errors.ParticipantNotExist)

    type Command =
        | Start of title: string * user: User
        | AddStory of user: User * storyId: Guid<StoryId>
        | AddParticipant of participant: User
        | RemoveParticipant of id: Guid<UserId>
        | SetActiveStory of user: User * id: Guid<StoryId>
        | AddGroup of user: User * group: Group
        | RemoveGroup of  user: User * groupId: Guid<GroupId>
        | MoveParticipantToGroup of user: User * userId: Guid<UserId> * groupId: Guid<GroupId>

    type Event =
        | Started of title: string * user: User
        | StoryAdded of story: Guid<StoryId>
        | ParticipantAdded of participant: Participant
        | ParticipantRemoved of participant: User
        | ActiveStorySet of id: Guid<StoryId>
        | GroupAdded of Group
        | GroupRemoved of Group
        | ParticipantMovedToGroup of user: User * group: Group

    let addParticipant participant session =
        { session with
              Participants = session.Participants.Add(participant.User.Id, participant) }

    let removeParticipant (participant: User) session =
        { session with
              Participants =
                  session.Participants.Remove participant.Id
                  }

    let addStory story session =
        { session with
              Stories = story :: session.Stories }

    let removeGroup groupId session =
       let rec updateGroup participants rest =
           match participants with
           | [] -> rest
           | head::tail ->
               if head.GroupId = groupId then
                   {head with GroupId = session.DefaultGroupId} :: rest
               else
                   head :: rest
               |> updateGroup tail

       let participants = updateGroup (session.Participants |> Map.toList |> List.map(snd)) List.empty
                          |> List.rev

       { session with
            Groups = session.Groups |> List.filter(fun g -> g.Id <> groupId)
            Participants = participants
                           |> List.toSeq
                           |> Seq.map(fun p -> p.User.Id, p)
                           |> Map.ofSeq
                           }

    let producer (state: SessionObj) command =
        match command with
        | Start (title, user) -> Ok <| Started(title, user)
        | AddStory (user, id) ->
            Validation.validateOwnerAccess user state
            |> Result.map(fun _ -> StoryAdded id)

        | AddParticipant user ->
           match Validation.validateParticipantExist user.Id state.Participants with
           | Ok _ -> Error Errors.ParticipantAlreadyExist
           | Error _ -> Ok <| ParticipantAdded { User = user; GroupId = state.DefaultGroupId }

        | RemoveParticipant id ->
            Validation.validateParticipantExist id state.Participants
            |> Result.map(ParticipantRemoved)

        | SetActiveStory (user, id) ->
             Validation.validateOwnerAccess user state
             |> Result.bind(fun _ ->
                                    if state.Stories |> List.contains id then
                                        Ok <| ActiveStorySet id
                                    else
                                        Error <| Errors.StoryNotExist
                    )
        | AddGroup (user, group) ->
            Validation.validateOwnerAccess user state
            |> Result.bind(fun _ -> Validation.validateGroupNotExists group.Name state.Groups)
            |> Result.map(fun _ -> GroupAdded group)

        | RemoveGroup (user, groupId) ->
            Validation.validateOwnerAccess user state
            |> Result.bind(fun _ -> Validation.validateGroupCanRemoved groupId state)
            |> Result.map(GroupRemoved)

        | MoveParticipantToGroup (user, participantId, groupId) ->
            Validation.validateOwnerAccess user state
            |> Result.bind(fun _ -> Validation.validateParticipantExist participantId state.Participants)
            |> Result.bind(fun user -> state.Groups
                                       |> List.tryFind (fun g -> g.Id = groupId)
                                       |> Option.map(fun g -> Ok (user, g))
                                       |> Option.defaultValue (Error <| Errors.GroupNotExist))
            |> Result.map(ParticipantMovedToGroup)

    let reducer (state: SessionObj) event =
        match event with
        | Started (title, user) ->
            { state with
                  Title = title
                  Owner = Some user }
        | StoryAdded story -> addStory story state
        | ParticipantAdded user -> addParticipant user state
        | ParticipantRemoved user -> removeParticipant user state
        | ActiveStorySet id -> { state with ActiveStory = Some id }
        | GroupAdded group -> {state with Groups =  group :: state.Groups}
        | GroupRemoved group -> removeGroup group.Id state
        | ParticipantMovedToGroup (user, group) -> {state with Participants = state.Participants.Add(user.Id, {GroupId = group.Id; User = user})}

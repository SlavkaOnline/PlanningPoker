namespace PlanningPoker.Domain

open System
open CommonTypes
open FSharp.UMX
open PlanningPoker.Domain.CommonTypes

[<Measure>]
type GroupId

type Group = {
    Id: Guid<GroupId>
    Name: string
}


[<CLIMutable>]
type SessionObj =
    { Title: string
      Owner: User option
      Participants:  Map<Guid<UserId>,User>
      Groups: Group list
      DefaultGroupId: Guid<GroupId>
      UserGroupMap: Map<Guid<UserId>, Guid<GroupId>>
      ActiveStory: Guid<StoryId> option
      Stories: Guid<StoryId> list }

    static member zero() =
        let defaultGroupId = Guid.NewGuid()
        { Title = ""
          Owner = None
          ActiveStory = None
          DefaultGroupId = %defaultGroupId
          Groups = [{Name = "Others"; Id = %defaultGroupId;}]
          UserGroupMap = Map.empty
          Participants = Map.empty
          Stories = [] }


[<RequireQualifiedAccess>]
module Session =

    [<RequireQualifiedAccess>]
    module DomainEvent =
         
         type Started = {
             Id: Guid<SessionId>
             Name: string
             UserId: Guid<UserId>
             }  
    
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

         let validateParticipantExist (userId: Guid<UserId>) (participants: Map<Guid<UserId>,User>) =
             participants.TryFind userId
             |> Option.map(Ok)
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
        | ParticipantAdded of participant: User * GroupId: Guid<GroupId>
        | ParticipantRemoved of participant: User
        | ActiveStorySet of id: Guid<StoryId>
        | GroupAdded of Group
        | GroupRemoved of Group
        | ParticipantMovedToGroup of user: User * group: Group
    
    let addParticipant (participant: User) groupId session =
        { session with
              Participants = session.Participants.Add(participant.Id, participant)
              UserGroupMap = session.UserGroupMap.Add(participant.Id, groupId) }

    let removeParticipant (participant: User) session =
        let participants = session.Participants.Remove participant.Id
        { session with
              Participants = participants }

    let addStory story session =
        { session with
              Stories = story :: session.Stories }

    let removeGroup groupId session =

       let rec updateGroup map rest =
           match map with
           | [] -> rest
           | head::tail ->
               if snd head = groupId then
                   (fst head, session.DefaultGroupId) :: rest
               else
                   head :: rest
               |> updateGroup tail

       let userGroupList = session.UserGroupMap |> Map.toList;

       { session with
            Groups = session.Groups |> List.filter(fun g -> g.Id <> groupId)
            UserGroupMap =  updateGroup userGroupList List.empty
                            |> List.rev
                            |> Map.ofList
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
           | Error _ -> Ok <| ParticipantAdded (user, state.UserGroupMap.TryFind user.Id |> Option.defaultValue state.DefaultGroupId)

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
        | ParticipantAdded (user, groupId) -> addParticipant user groupId state
        | ParticipantRemoved user -> removeParticipant user state
        | ActiveStorySet id -> { state with ActiveStory = Some id }
        | GroupAdded group -> {state with Groups =  group :: state.Groups}
        | GroupRemoved group -> removeGroup group.Id state
        | ParticipantMovedToGroup (user, group) ->  {state with UserGroupMap = state.UserGroupMap.Add(user.Id, group.Id)}

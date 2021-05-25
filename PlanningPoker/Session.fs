namespace PlanningPoker.Domain

open CommonTypes
open FSharp.UMX


[<CLIMutable>]
type SessionObj =
    { Title: string
      Owner: User option
      Participants: User list
      Stories: Guid<ObjectId> list }
    member this.hasAccessToChange user =
        match this.Owner with
        | Some u -> u = user
        | None -> false

    member this.hasParticipant participant =
        List.contains participant this.Participants

    static member zero =
        { Title = ""
          Owner = None
          Participants = []
          Stories = [] }


[<RequireQualifiedAccess>]
module Session =

    type Command =
        | Start of title: string * user: User
        | AddStory of user: User * storyId: Guid<ObjectId>
        | AddParticipant of participant: User
        | RemoveParticipant of id: Guid<UserId>

    type Event =
        | Started of title: string * user: User
        | StoryAdded of story: Guid<ObjectId>
        | ParticipantAdded of participant: User
        | ParticipantRemoved of participant: User

    let addParticipant participant session =
        { session with
              Participants = participant :: session.Participants }

    let removeParticipant (participant: User) session =
        { session with
              Participants =
                  session.Participants
                  |> List.filter (fun p -> p.Id <> participant.Id) }

    let addStory story session =
        { session with
              Stories = story :: session.Stories }

    let producer (state: SessionObj) command =
        match command with
        | Start (title, user) -> Ok <| Started(title, user)
        | AddStory (user, id) ->
            if state.hasAccessToChange user then
                Ok <| StoryAdded id
            else
                Error <| Errors.UnauthorizedAccess

        | AddParticipant user ->
            if not (state.hasParticipant user) then
                Ok <| ParticipantAdded user
            else
                Error <| Errors.ParticipantAlreadyExist

        | RemoveParticipant id ->
            match List.tryFind (fun (p: User) -> p.Id = id) state.Participants with
            | Some p -> Ok <| ParticipantRemoved p
            | None -> Error <| Errors.ParticipantNotExist

    let reducer (state: SessionObj) event =
        match event with
        | Started (title, user) -> { state with Title = title; Owner = Some user }
        | StoryAdded story -> addStory story state
        | ParticipantAdded user -> addParticipant user state
        | ParticipantRemoved user -> removeParticipant user state

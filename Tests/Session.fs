module Tests.Session

open System
open PlanningPoker.Domain
open PlanningPoker.Domain.CommonTypes
open Tests.Helper
open Xunit
open FSharp.UMX
open Swensen.Unquote

let getUser () =
    { Id = % Guid.NewGuid()
      Name = ""
      Picture = Some "" }

[<Fact>]
let ``The session is created with default group`` () =
    let session = SessionObj.zero ()

    test <@ session.Groups.Head.Id = session.DefaultGroupId @>

[<Fact>]
let ``The Participant is added to the new group``() =
    let session = SessionObj.zero ()
    let user = getUser ()
    let group = { Id = %Guid.NewGuid(); Name = "Group" }
    let handler = Aggregate.createHandler Session.producer Session.reducer

    let result =
        session
        |> handler (Session.Start("Session", user))
        |> Result.bind(Session.AddGroup(user, group) |> handler)
        |> Result.bind(Session.AddParticipant user |> handler)
        |> Result.bind(Session.MoveParticipantToGroup(user, user.Id, group.Id) |> handler)
        |> Result.map(fun s ->  s.UserGroupMap)

    test <@ match result with
            | Ok userGroupMap -> userGroupMap.[user.Id] = group.Id
            | _ -> false
             @>

[<Fact>]
let ``The participant from removed group moving to default group``() =
    let session = SessionObj.zero()
    let user = getUser()
    let group = { Id = %Guid.NewGuid(); Name = "Group" }

    let participants = [|getUser(); getUser(); getUser()|]

    let handler = Aggregate.createHandler Session.producer Session.reducer

    let result =
        session
        |> handler (Session.Start("Session", user))
        |> Result.bind(Session.AddGroup(user, group) |> handler )
        |> Result.bind(Session.AddParticipant participants.[0] |> handler )
        |> Result.bind(Session.MoveParticipantToGroup(user, participants.[0].Id, group.Id) |> handler )
        |> Result.bind(Session.AddParticipant participants.[1] |> handler )
        |> Result.bind(Session.MoveParticipantToGroup(user, participants.[1].Id, group.Id) |> handler )
        |> Result.bind(Session.AddParticipant participants.[2] |> handler )
        |> Result.bind(Session.MoveParticipantToGroup(user, participants.[2].Id, group.Id) |> handler )
        |> Result.bind(Session.RemoveGroup(user, group.Id) |> handler )
        |> Result.map(fun s -> s.UserGroupMap)

    test <@ match result with
            | Ok userGroupMap -> userGroupMap |> Map.toSeq |> Seq.map( fun g -> snd g = session.DefaultGroupId) |> Seq.reduce (&&)
            | _ -> false
             @>

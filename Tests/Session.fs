module Tests.Session

open System
open PlanningPoker.Domain
open PlanningPoker.Domain.CommonTypes
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
let ``The new participant is added to default group`` () =
    let session = SessionObj.zero ()
    let user = getUser ()

    let result =
        Session.producer session
        <| Session.AddParticipant user

    test
        <@ match result with
           | Ok event ->
               match event with
               | Session.ParticipantAdded p -> p.User = user
               | _ -> false
           | _ -> false @>

namespace IntegrationTests

open System.Collections.Generic
open FSharp.UMX
open GrainInterfaces
open PlanningPoker.Domain
open Xunit
open System
open Swensen.Unquote
open CommonTypes

[<Collection("silo")>]
type BehaviorTests(fixture: ClusterFixture) =

    let cluster = fixture.Cluster

    let getUser() = {Id = %Guid.NewGuid(); Name = ""; Picture = Some ""}

    [<Fact>]
    let ``Created session has version = 1`` () =
        task {
            let id = Guid.NewGuid()
            let grain = cluster.GrainFactory.GetGrain<ISessionGrain> id
            let! session = grain.Start("Session", getUser());
            test <@ session.Version = 1 @>
        }
    [<Fact>]
    let ``Created story has version = 1`` () =
        task {
            let id = Guid.NewGuid()
            let grain = cluster.GrainFactory.GetGrain<IStoryGrain> id
            let! story = grain.Configure(getUser(), "story", [|"Card 1"|]);
            test <@ story.Version = 1 @>
        }
    [<Fact>]
    let ``Story check duration`` () =
        task {
            let id = Guid.NewGuid()
            let user = getUser()
            let sessionGrain = cluster.GrainFactory.GetGrain<ISessionGrain> id
            let! _ = sessionGrain.Start("Session", user)

            let! session = sessionGrain.AddStory(user, "story1", [|"Card 1"|])
            let storyId = session.Stories.[0]

            let storyGrain = cluster.GrainFactory.GetGrain<IStoryGrain> storyId

            let dt1 = DateTime(2021, 9, 14, 0, 0, 0, 0);

            let! _ =  sessionGrain.SetActiveStory(user, storyId, dt1)

            let! session_ = sessionGrain.AddStory(user, "story2", [|"Card 1"|])
            let anotherStoryId = session_.Stories.[0]

            let dt2 = dt1.AddMinutes 5.0

            let! _ =  sessionGrain.SetActiveStory(user, anotherStoryId, dt2)

            let dt3 = dt1.AddMinutes 10.0

            let! _ =  sessionGrain.SetActiveStory(user, storyId, dt3)

            let! _ = storyGrain.Vote(user, "Card 1", dt3)

            let dt4 = dt1.AddMinutes 15.0

            let groups = seq {session.Groups.[0].Id, (session.Participants |> Array.map(fun p -> p.Id)) } |> dict |> Dictionary

            let! story = storyGrain.Close(user, dt4, groups)

            let expected = TimeSpan.FromMinutes(10.).ToString(@"hh\:mm\:ss");
            test <@ story.Duration = expected @>
        }

    [<Fact>]
    let ``Vote check duration`` () =
        task {
            let id = Guid.NewGuid()
            let user = getUser()
            let sessionGrain = cluster.GrainFactory.GetGrain<ISessionGrain> id
            let! _ = sessionGrain.Start("Session", user)

            let! session = sessionGrain.AddStory(user, "story1", [|"Card 1"|])
            let storyId = session.Stories.[0]

            let storyGrain = cluster.GrainFactory.GetGrain<IStoryGrain> storyId

            let dt1 = DateTime(2021, 9, 14, 0, 0, 0, 0);

            let! _ =  sessionGrain.SetActiveStory(user, storyId, dt1)

            let! session_ = sessionGrain.AddStory(user, "story2", [|"Card 1"|])
            let anotherStoryId = session_.Stories.[0]

            let dt2 = dt1.AddMinutes 5.0

            let! _ =  sessionGrain.SetActiveStory(user, anotherStoryId, dt2)

            let dt3 = dt1.AddMinutes 10.0

            let! _ =  sessionGrain.SetActiveStory(user, storyId, dt3)

            let! _ = storyGrain.Vote(user, "Card 1", dt3)

            let dt4 = dt1.AddMinutes 10.0

            let groups = seq {(session.Groups.[0].Id, (session.Participants |> Array.map(fun p -> p.Id) )) } |> dict |> Dictionary

            let! story = storyGrain.Close(user, dt4, groups)
            let expected = TimeSpan.FromMinutes(5.).ToString(@"hh\:mm\:ss")
            let duration = story.Statistics.[0].Result.["Card 1"].Voters.[0].Duration
            test <@ duration = expected @>
        }
    
    [<Fact>]
    let ``Paused story check duration`` () =
        task {
            let id = Guid.NewGuid()
            let user = getUser()
            let sessionGrain = cluster.GrainFactory.GetGrain<ISessionGrain> id
            let! _ = sessionGrain.Start("Session", user)

            let! session = sessionGrain.AddStory(user, "story1", [|"Card 1"|])
            let storyId = session.Stories.[0]

            let storyGrain = cluster.GrainFactory.GetGrain<IStoryGrain> storyId

            let dt1 = DateTime(2021, 9, 14, 0, 0, 0, 0);

            let! _ =  sessionGrain.SetActiveStory(user, storyId, dt1)

            let! session_ = sessionGrain.AddStory(user, "story2", [|"Card 1"|])
            let anotherStoryId = session_.Stories.[0]
            let anotherStoryGrain = cluster.GrainFactory.GetGrain<IStoryGrain> anotherStoryId
            
            let dt2 = dt1.AddMinutes 5.0

            let! _ =  sessionGrain.SetActiveStory(user, anotherStoryId, dt1)
            let! _ = anotherStoryGrain.Vote(user, "Card 1", dt2)

            let dt3 = dt1.AddMinutes 10.0

            let! _ =  sessionGrain.SetActiveStory(user, storyId, dt3)

            let groups = seq {session.Groups.[0].Id, (session.Participants |> Array.map(fun p -> p.Id)) } |> dict |> Dictionary

            let! story = anotherStoryGrain.Close(user, dt3, groups)

            let expected = TimeSpan.FromMinutes(10.).ToString(@"hh\:mm\:ss");
            test <@ story.Duration = expected @>
        }
        
    [<Fact>]
    let ``Not started story check duration`` () =
        task {
            let id = Guid.NewGuid()
            let user = getUser()
            let sessionGrain = cluster.GrainFactory.GetGrain<ISessionGrain> id
            let! _ = sessionGrain.Start("Session", user)

            let! session = sessionGrain.AddStory(user, "story1", [|"Card 1"|])
            let storyId = session.Stories.[0]
            
            let dt1 = DateTime(2021, 9, 14, 0, 0, 0, 0);

            let! _ =  sessionGrain.SetActiveStory(user, storyId, dt1)

            let! session_ = sessionGrain.AddStory(user, "story2", [|"Card 1"|])
            let anotherStoryId = session_.Stories.[0]
            let anotherStoryGrain = cluster.GrainFactory.GetGrain<IStoryGrain> anotherStoryId
            
            let dt2 = dt1.AddMinutes 5.0

            let! _ = anotherStoryGrain.Vote(user, "Card 1", dt2)

            let dt3 = dt1.AddMinutes 10.0

            let groups = seq {session.Groups.[0].Id, (session.Participants |> Array.map(fun p -> p.Id)) } |> dict |> Dictionary

            let! story = anotherStoryGrain.Close(user, dt3, groups)

            test <@ story.Duration = TimeSpan.Zero.ToString(@"hh\:mm\:ss") @>
        }
        
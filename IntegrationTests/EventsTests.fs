namespace IntegrationTests

open System.Collections.Generic
open FSharp.Control
open IntegrationTests.FakeServer
open Xunit
open Api
open Gateway.Requests
open Gateway.Views
open Microsoft.AspNetCore.SignalR.Client
open Swensen.Unquote
open System.IdentityModel.Tokens.Jwt
open System

[<Collection("Real Server Collection")>]
type EventsTests(fixture: CustomWebApplicationFactory<Program>) =

    let server = fixture.Server
    let apiClient = fixture.CreateClient()
    let cardsId = "66920B8F-3962-46FE-A2C1-434134B7F0FD"
    let pause = TimeSpan.FromSeconds 10.0

    let validateGrowingEventOrdering arr =
        arr
        |> Array.pairwise
        |> Array.fold (fun state (a, b) -> state && a < b) true

    let compareArraysByExistingElements (arr1: string array) (arr2: string array) =
        let set1 = arr1 |> Set.ofArray
        let set2 = arr2 |> Set.ofArray
        set1 = set2


    [<Fact>]
    let ``The participant was added when SignalR connection has been the establishment`` () =
        task {

            let! user = Helper.login apiClient "test"

            let! connection = Helper.createEventsConnection server user.Token

            let! session = Helper.createSession apiClient user.Token "Session"

            let! subscription = connection.StreamAsChannelAsync<_>("Session", session.Id, session.Version)

            let! updatedSession = Helper.getSession apiClient user.Token session.Id

            let! events =
                async {
                    let! arr =
                        subscription.ReadAllAsync()
                        |> AsyncSeq.ofAsyncEnum
                        |> AsyncSeq.bufferByCountAndTime 1 (int pause.TotalMilliseconds)
                        |> AsyncSeq.take (1)
                        |> AsyncSeq.toArrayAsync

                    return arr.[0]
                }

            do! connection.StopAsync()

            test <@ updatedSession.Participants.Length = 1 @>
        }


    [<Fact>]
    let ``The participant was removed when connection has been the closed`` () =
        task {
            let! user = Helper.login apiClient "test"
            let! user2 = Helper.login apiClient "test"
            let! connection = Helper.createEventsConnection server user.Token
            let! connection2 = Helper.createEventsConnection server user2.Token

            let! session = Helper.createSession apiClient user.Token "Session"

            let! subscription = connection.StreamAsChannelAsync<_>("Session", session.Id, session.Version)

            let! subscription2 = connection2.StreamAsChannelAsync<_>("Session", session.Id, session.Version)

            let! _ =
                async {
                    let! arr =
                        subscription.ReadAllAsync()
                        |> AsyncSeq.ofAsyncEnum
                        |> AsyncSeq.bufferByCountAndTime 2 (int pause.TotalMilliseconds)
                        |> AsyncSeq.take (1)
                        |> AsyncSeq.toArrayAsync

                    return arr.[0]
                }

            let! updatedSession = Helper.getSession apiClient user.Token session.Id

            test <@ updatedSession.Participants.Length = 2 @>

            do! connection2.StopAsync()

            let! _ =
                async {
                    let! arr =
                        subscription.ReadAllAsync()
                        |> AsyncSeq.ofAsyncEnum
                        |> AsyncSeq.bufferByCountAndTime 1 (int pause.TotalMilliseconds)
                        |> AsyncSeq.take (1)
                        |> AsyncSeq.toArrayAsync

                    return arr.[0]
                }

            let! finallySession = Helper.getSession apiClient user.Token session.Id
            do! connection.StopAsync()

            test <@ finallySession.Participants.Length = 1 @>
        }

    [<Fact>]
    let ``Session events in stream equals completed actions`` () =
        task {

            let! user = Helper.login apiClient "test"

            let! connection = Helper.createEventsConnection server user.Token

            //action 1 Started
            let! session = Helper.createSession apiClient user.Token "Session"

            //action 3 "ParticipantAdded"
            let! subscription = connection.StreamAsChannelAsync<EventsDeliveryHub.Event>("Session", session.Id, 0)

            //action 3 StoryAdded
            let! ses =
                Helper.addStoryToSession
                    apiClient
                    user.Token
                    session
                    { CreateStory.Title = "Story 1"
                      CardsId = cardsId
                      CustomCards = [| "1"; "2" |] }

            let storyId = ses.Stories.[0]

            //action 4 ActiveStorySet
            do! Helper.setActiveStory apiClient user.Token session.Id storyId

            let! events =
                async {
                    let! arr =
                        subscription.ReadAllAsync()
                        |> AsyncSeq.ofAsyncEnum
                        |> AsyncSeq.bufferByCountAndTime 4 (int pause.TotalMilliseconds)
                        |> AsyncSeq.take (1)
                        |> AsyncSeq.toArrayAsync

                    return arr.[0] |> Array.map (fun e -> (e.Order, e.Type))
                }

            let! s = Helper.getSession apiClient user.Token session.Id

            test <@ events.Length > 0 @>
            test <@ validateGrowingEventOrdering (events |> Array.map (fst)) @>

            test
                <@ compareArraysByExistingElements
                    (events |> Array.map (snd))
                    [| "Started"
                       "StoryAdded"
                       "ParticipantAdded"
                       "ActiveStorySet" |] @>

            test <@ s.Version = 4 @>
        }



    [<Fact>]
    let ``Session events in stream equals completed actions from non zero event`` () =
        task {

            let! user = Helper.login apiClient "test"

            let! connection = Helper.createEventsConnection server user.Token

            //action 1 Started
            let! session = Helper.createSession apiClient user.Token "Session"

            //action 2 "ParticipantAdded"
            let! subscription =
                connection.StreamAsChannelAsync<EventsDeliveryHub.Event>("Session", session.Id, session.Version)

            //action 3 StoryAdded
            let! ses =
                Helper.addStoryToSession
                    apiClient
                    user.Token
                    session
                    { CreateStory.Title = "Story 1"
                      CardsId = cardsId
                      CustomCards = [| "1"; "2" |] }

            let storyId = ses.Stories.[0]

            //action 4 ActiveStorySet
            let! s = Helper.setActiveStory apiClient user.Token session.Id storyId

            let! events =
                async {
                    let! arr =
                        subscription.ReadAllAsync()
                        |> AsyncSeq.ofAsyncEnum
                        |> AsyncSeq.bufferByCountAndTime 3 (int pause.TotalMilliseconds)
                        |> AsyncSeq.take (1)
                        |> AsyncSeq.toArrayAsync

                    return arr.[0] |> Array.map (fun e -> (e.Order, e.Type))
                }

            let! s = Helper.getSession apiClient user.Token session.Id

            test <@ events.Length > 0 @>
            test <@ validateGrowingEventOrdering (events |> Array.map (fst)) @>

            test
                <@ compareArraysByExistingElements
                    (events |> Array.map (snd))
                    [| "StoryAdded"
                       "ParticipantAdded"
                       "ActiveStorySet" |] @>

            test <@ s.Version = 4 @>
        }


    [<Fact>]
    let ``Story events in stream equals completed actions`` () =
        task {

            let! user = Helper.login apiClient "test"

            let! connection = Helper.createEventsConnection server user.Token
            let! session = Helper.createSession apiClient user.Token "Session"

            //action1 StoryConfigured
            let! s =
                Helper.addStoryToSession
                    apiClient
                    user.Token
                    session
                    { CreateStory.Title = "Story 1"
                      CardsId = cardsId
                      CustomCards = [||] }

            let! ses =
                Helper.addStoryToSession
                    apiClient
                    user.Token
                    s
                    { CreateStory.Title = "Story 2"
                      CardsId = cardsId
                      CustomCards = [||] }

            let storyId = ses.Stories.[1]
            let anotherStoryId = ses.Stories.[0]

            let! subscription = connection.StreamAsChannelAsync<EventsDeliveryHub.Event>("Story", storyId, 0)


            //action2  ActiveSet
            do! Helper.setActiveStory apiClient user.Token session.Id storyId

            //action3 Voted
            let! _ = Helper.vote apiClient user.Token storyId "XXS"

            let groups =
                seq { (session.Groups.[0].Id, (session.Participants |> Array.map (fun p -> p.Id))) }
                |> dict
                |> Dictionary

            //action4 StoryClosed
            let! _ = Helper.closeStory apiClient user.Token storyId { Groups = groups }

            //action5  Paused
            do! Helper.setActiveStory apiClient user.Token session.Id anotherStoryId
            let! _ = Helper.vote apiClient user.Token anotherStoryId "XXS"
            let! _ = Helper.closeStory apiClient user.Token anotherStoryId { Groups = groups }

            //action6  ActiveSet
            do! Helper.setActiveStory apiClient user.Token session.Id storyId

            //action7 Cleared
            let! _ = Helper.clearStory apiClient user.Token storyId

            //action8  Paused
            do! Helper.setActiveStory apiClient user.Token session.Id anotherStoryId

            let! s = Helper.getStory apiClient user.Token storyId

            let! events =
                async {
                    let! arr =
                        subscription.ReadAllAsync()
                        |> AsyncSeq.ofAsyncEnum
                        |> AsyncSeq.bufferByCountAndTime 8 (int pause.TotalMilliseconds)
                        |> AsyncSeq.take (1)
                        |> AsyncSeq.toArrayAsync

                    return arr.[0] |> Array.map (fun e -> (e.Order, e.Type))
                }

            do! connection.StopAsync()

            test
                <@ events = [| 1, "StoryConfigured"
                               2, "ActiveSet"
                               3, "Voted"
                               4, "StoryClosed"
                               5, "Paused"
                               6, "ActiveSet"
                               7, "Cleared"
                               8, "Paused" |] @>

            test <@ s.Version = 8 @>
        }

    [<Fact>]
    let ``The workflow add group, move Participant, remove group works fine`` () =
        task {

            let! user = Helper.login apiClient "test"
            let tokenHandler = JwtSecurityTokenHandler()
            let token = tokenHandler.ReadJwtToken(user.Token)

            let userId =
                token.Claims
                |> Seq.filter (fun c -> c.Type = "nameid")
                |> Seq.tryHead
                |> Option.map (fun c -> c.Value |> Guid.Parse)
                |> Option.defaultValue Guid.Empty

            let! connection = Helper.createEventsConnection server user.Token
            let! session = Helper.createSession apiClient user.Token "Session"

            let! subscription = connection.StreamAsChannelAsync<EventsDeliveryHub.Event>("Session", session.Id, 0)

            let! sessionWithGroup = Helper.addGroup apiClient user.Token session.Id "group"

            let group =
                sessionWithGroup.Groups
                |> Array.find (fun g -> g.Id <> session.DefaultGroupId)

            let! _ = Helper.moveParticipantToGroup apiClient user.Token session.Id userId group.Id
            let! s = Helper.removeGroup apiClient user.Token session.Id group.Id

            let! events =
                async {
                    let! arr =
                        subscription.ReadAllAsync()
                        |> AsyncSeq.ofAsyncEnum
                        |> AsyncSeq.bufferByCountAndTime 5 (int pause.TotalMilliseconds)
                        |> AsyncSeq.take (1)
                        |> AsyncSeq.toArrayAsync

                    return arr.[0] |> Array.map (fun e -> (e.Order, e.Type))
                }

            do! connection.StopAsync()

            test <@ events.Length > 0 @>
            test <@ validateGrowingEventOrdering (events |> Array.map (fst)) @>

            test
                <@ compareArraysByExistingElements
                    (events |> Array.map (snd))
                    [| "Started"
                       "ParticipantAdded"
                       "GroupAdded"
                       "ParticipantMovedToGroup"
                       "GroupRemoved" |] @>

            test <@ s.Version = 5 @>
        }
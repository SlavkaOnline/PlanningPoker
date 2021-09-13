namespace IntegrationTests

open System
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks
open FSharp.Control
open IntegrationTests.FakeServer
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http.Connections
open Xunit

open WebApi
open System.Net.Http
open Gateway.Requests
open Gateway.Views
open Microsoft.AspNetCore.SignalR.Client
open EventsDelivery

open Swensen.Unquote

//will work on NET 6 https://github.com/dotnet/aspnetcore/issues/11888
//type Test(factory: FakeServerFixture) =
//
//    [<Fact>]
//    let ``The participant was added when SignalR connection has been the establishment`` () = async {
//
//           let apiClient = factory.CreateClient()
//           let! user = Helper.login apiClient "test"
//
//           let connection = HubConnectionBuilder()
//                                .WithUrl($"%s{apiClient.BaseAddress.ToString()}events", fun options ->
//                                    options.AccessTokenProvider <- (fun _ -> Task.FromResult user.Token)
//                                    options.WebSocketConfiguration
//                                    options.HttpMessageHandlerFactory <- (fun _ -> factory.Server.CreateHandler())
//                                    )
//                                .Build()
//
//
//           let! session = Helper.requestPost<_, SessionView> apiClient {CreateSession.Title = "Session"}  user.Token "Sessions"
//           do! connection.StartAsync() |> Async.AwaitTask
//           let subscription = connection.StreamAsChannelAsync<_>("Session", session.Id, 0)
//
//           let! updatedSession = Helper.requestGet<SessionView> apiClient user.Token $"Sessions/%s{session.Id.ToString()}"
//
//           test <@ updatedSession.Participants.Length = 1
//                    @>
//
//       }
//
//    interface IClassFixture<FakeServerFixture>

[<Collection("Real Server Collection")>]
type EventsTests(server: RealServerFixture) =

    let url = "http://localhost:5050"
    do server.Start(url)
    let cardsId = "66920B8F-3962-46FE-A2C1-434134B7F0FD"

    [<Fact>]
    let ``The participant was added when SignalR connection has been the establishment`` () =
        async {
            use apiClient = new HttpClient()
            do apiClient.BaseAddress <- Uri(url)

            let! user = Helper.login apiClient "test"

            let connection =
                HubConnectionBuilder()
                    .WithUrl(
                        $"%s{apiClient.BaseAddress.ToString()}events",
                        fun options ->
                            options.AccessTokenProvider <- (fun _ -> Task.FromResult user.Token)
                            options.Transports <- HttpTransportType.WebSockets
                            options.SkipNegotiation <- true
                    )
                    .Build()


            let! session = Helper.createSession apiClient user.Token "Session"

            do! connection.StartAsync() |> Async.AwaitTask

            let! subscription =
                connection.StreamAsChannelAsync<_>("Session", session.Id, 0)
                |> Async.AwaitTask

            do! Async.Sleep(1000);

            let! updatedSession = Helper.getSession apiClient user.Token session.Id

            test <@ updatedSession.Participants.Length = 1 @>
        }


    [<Fact>]
    let ``The participant was removed when SignalR connection has been the closed`` () =
        async {
            use apiClient = new HttpClient()
            do apiClient.BaseAddress <- Uri(url)

            let! user = Helper.login apiClient "test"

            let connection =
                HubConnectionBuilder()
                    .WithUrl(
                        $"%s{apiClient.BaseAddress.ToString()}events",
                        fun options ->
                            options.AccessTokenProvider <- (fun _ -> Task.FromResult user.Token)
                            options.Transports <- HttpTransportType.WebSockets
                            options.SkipNegotiation <- true
                    )
                    .Build()


            let! session = Helper.createSession apiClient user.Token "Session"

            do! connection.StartAsync() |> Async.AwaitTask

            let! subscription =
                connection.StreamAsChannelAsync<_>("Session", session.Id, 0)
                |> Async.AwaitTask

            do! Async.Sleep(1000);

            let! updatedSession = Helper.getSession apiClient user.Token session.Id

            test <@ updatedSession.Participants.Length = 1 @>

            do! connection.StopAsync() |> Async.AwaitTask

            do! Async.Sleep(1000)

            let! finallySession = Helper.getSession apiClient user.Token session.Id

            test <@ finallySession.Participants.Length = 0 @>
        }

    [<Fact>]
    let ``Session events in stream equals completed actions`` () =
        async {

            use apiClient = new HttpClient()
            do apiClient.BaseAddress <- Uri(url)

            let! user = Helper.login apiClient "test"

            let connection =
                HubConnectionBuilder()
                    .WithUrl(
                        $"%s{apiClient.BaseAddress.ToString()}events",
                        fun options ->
                            options.AccessTokenProvider <- (fun _ -> Task.FromResult user.Token)
                            options.Transports <- HttpTransportType.WebSockets
                            options.SkipNegotiation <- true
                    )
                    .Build()

            //action 1 Started
            let! session = Helper.createSession apiClient user.Token "Session"

            do! connection.StartAsync() |> Async.AwaitTask

           //action 2 "ParticipantAdded"
            let! subscription =
                connection.StreamAsChannelAsync<EventsDeliveryHub.Event>("Session", session.Id, 0)
                |> Async.AwaitTask

            let eventsBuffer = Channel.CreateUnbounded<EventsDeliveryHub.Event>()
            let subs =  subscription.ReadAllAsync()
                       |> AsyncSeq.ofAsyncEnum
                       |> AsyncSeq.iterAsync(fun e -> eventsBuffer.Writer.WriteAsync(e).AsTask() |> Async.AwaitTask)
            let complete _ = eventsBuffer.Writer.Complete();
            Async.StartWithContinuations (subs, complete, complete, complete)

            //action 3 StoryAdded
            let! _, storyId = Helper.addStoryToSession apiClient user.Token session { CreateStory.Title = "Story 1"; CardsId = cardsId; CustomCards = [|"1"; "2"|] }

            //action 4 ActiveStorySet
            do! Helper.setActiveStory apiClient user.Token session.Id storyId

            do! Async.Sleep(1000)

            do! connection.StopAsync() |> Async.AwaitTask

            let events = eventsBuffer.Reader.ReadAllAsync()
                         |> AsyncSeq.ofAsyncEnum
                         |> AsyncSeq.toArraySynchronously
                         |> Array.map(fun e -> e.Type)

            test <@ events = [|"Started"; "ParticipantAdded"; "StoryAdded"; "ActiveStorySet"|] @>
        }


    [<Fact>]
    let ``Story events in stream equals completed actions`` () =
        async {

            use apiClient = new HttpClient()
            do apiClient.BaseAddress <- Uri(url)

            let! user = Helper.login apiClient "test"

            let connection =
                HubConnectionBuilder()
                    .WithUrl(
                        $"%s{apiClient.BaseAddress.ToString()}events",
                        fun options ->
                            options.AccessTokenProvider <- (fun _ -> Task.FromResult user.Token)
                            options.Transports <- HttpTransportType.WebSockets
                            options.SkipNegotiation <- true
                    )
                    .Build()

            let! session = Helper.createSession apiClient user.Token "Session"

            do! connection.StartAsync() |> Async.AwaitTask
            //action1 StoryConfigured
            let! s, storyId = Helper.addStoryToSession apiClient user.Token session { CreateStory.Title = "Story 1"; CardsId = cardsId; CustomCards = [||] }
            let! _, anotherStoryId = Helper.addStoryToSession apiClient user.Token s { CreateStory.Title = "Story 2"; CardsId = cardsId; CustomCards = [||] }

            let! subscription =
                connection.StreamAsChannelAsync<EventsDeliveryHub.Event>("Story", storyId, 0)
                |> Async.AwaitTask

            let eventsBuffer = Channel.CreateUnbounded<EventsDeliveryHub.Event>()
            let subs =  subscription.ReadAllAsync()
                       |> AsyncSeq.ofAsyncEnum
                       |> AsyncSeq.iterAsync(fun e -> eventsBuffer.Writer.WriteAsync(e).AsTask() |> Async.AwaitTask)
            let complete _ = eventsBuffer.Writer.Complete();
            Async.StartWithContinuations (subs, complete, complete, complete)

            //action2  ActiveSet
            do! Helper.setActiveStory apiClient user.Token session.Id storyId

            //action3 Voted
            do! Helper.vote apiClient user.Token storyId "XXS" |> Async.Ignore

            //action4 StoryClosed
            do! Helper.closeStory apiClient user.Token storyId |> Async.Ignore

            //action5  Paused
            do! Helper.setActiveStory apiClient user.Token session.Id anotherStoryId

            //action6  ActiveSet
            do! Helper.setActiveStory apiClient user.Token session.Id storyId

            //action7 Cleared
            do! Helper.clearStory apiClient user.Token storyId |> Async.Ignore

            //action8  Paused
            do! Helper.setActiveStory apiClient user.Token session.Id anotherStoryId

            do! Async.Sleep(1000)

            do! connection.StopAsync() |> Async.AwaitTask

            let events = eventsBuffer.Reader.ReadAllAsync()
                         |> AsyncSeq.ofAsyncEnum
                         |> AsyncSeq.toArraySynchronously
                         |> Array.map(fun e -> e.Type)

            test <@ events = [|"StoryConfigured"; "ActiveSet"; "Voted"; "StoryClosed"; "Paused"; "ActiveSet"; "Cleared"; "Paused" |] @>
        }

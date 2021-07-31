namespace IntegrationTests

open System
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


            let! session =
                Helper.requestPost<_, SessionView> apiClient { CreateSession.Title = "Session" } user.Token "Sessions"

            do! connection.StartAsync() |> Async.AwaitTask

            let! subscription =
                connection.StreamAsChannelAsync<_>("Session", session.Id, 0)
                |> Async.AwaitTask

            let! updatedSession =
                Helper.requestGet<SessionView> apiClient user.Token $"Sessions/%s{session.Id.ToString()}"

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


            let! session =
                Helper.requestPost<_, SessionView> apiClient { CreateSession.Title = "Session" } user.Token "Sessions"

            do! connection.StartAsync() |> Async.AwaitTask

            let! subscription =
                connection.StreamAsChannelAsync<_>("Session", session.Id, 0)
                |> Async.AwaitTask

            let! updatedSession =
                Helper.requestGet<SessionView> apiClient user.Token $"Sessions/%s{session.Id.ToString()}"

            test <@ updatedSession.Participants.Length = 1 @>

            do! connection.StopAsync() |> Async.AwaitTask

            do! Async.Sleep(1000)

            let! finallySession =
                Helper.requestGet<SessionView> apiClient user.Token $"Sessions/%s{session.Id.ToString()}"

            test <@ finallySession.Participants.Length = 0 @>
        }

    [<Fact>]
    let ``Events count in stream equals completed actions`` () =
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


            let! session =
                Helper.requestPost<_, SessionView> apiClient { CreateSession.Title = "Session" } user.Token "Sessions"

            do! connection.StartAsync() |> Async.AwaitTask

            let! subscription =
                connection.StreamAsChannelAsync<_>("Session", session.Id, 0)
                |> Async.AwaitTask

            do!
                Helper.requestGet<_> apiClient user.Token $"Sessions/%s{session.Id.ToString()}"
                |> Async.Ignore

            let! sessionWithStory =
                Helper.requestPost<_, SessionView>
                    apiClient
                    { CreateStory.Title = "Story 1"
                      CardsId = cardsId
                      CustomCards = [|"1"; "2"|] }
                    user.Token
                    $"Sessions/%s{session.Id.ToString()}/stories"

            let storyId = sessionWithStory.Stories.[0]

            do!
                Helper.requestPost<_, _>
                    apiClient
                    { SetActiveStory.Id = storyId.ToString() }
                    user.Token
                    $"Sessions/%s{session.Id.ToString()}/activestory"

            do! Async.Sleep(1000)

            test <@ subscription.Count = 5 @>
        }

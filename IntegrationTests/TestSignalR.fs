namespace IntegrationTests

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http.Connections
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.AspNetCore.SignalR.Client
open Xunit
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


type Tests() =

    [<Fact>]
    let ``Test SignalR``() = task {
        let factory =  new WebApplicationFactory<WebApi.Program>()
        let client = factory.CreateClient()
        let! user = Helper.login client "tests"

        let connection = HubConnectionBuilder()
                             .WithUrl($"%s{client.BaseAddress.ToString()}events", fun options ->
                                    options.AccessTokenProvider <- (fun _ -> Task.FromResult user.Token)
                                    options.SkipNegotiation <- true
                                    options.Transports <- HttpTransportType.WebSockets
                                    options.WebSocketFactory <- (fun ctx cancellationToken ->
                                            let t = task {
                                                let webSocketClient = factory.Server.CreateWebSocketClient()
                                                let url = $"%s{ctx.Uri.ToString()}?access_token=%s{user.Token}"
                                                return! webSocketClient.ConnectAsync(Uri(url), cancellationToken)
                                            }
                                            ValueTask<Net.WebSockets.WebSocket>(t)
                                        )
                                    )
                             .Build()
        let! session = Helper.createSession client user.Token "Session"
        do! connection.StartAsync()
        let! subscription =
                connection.StreamAsChannelAsync<_>("Session", session.Id, 0)
                |> Async.AwaitTask

        do! Task.Delay(1000);

        let! updatedSession = Helper.getSession client user.Token session.Id

        test <@ updatedSession.Participants.Length = 1 @>
        }


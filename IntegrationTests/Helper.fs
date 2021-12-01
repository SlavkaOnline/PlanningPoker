namespace IntegrationTests

open System
open System.Collections.Generic
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Threading.Tasks
open Gateway.Requests
open Gateway.Views
open Microsoft.AspNetCore.Http.Connections
open Microsoft.AspNetCore.SignalR.Client
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.Hosting
open Newtonsoft.Json


[<RequireQualifiedAccess>]
module Helper =

    let login (client: HttpClient) (name: string) : Task<AuthUserModel> =
        task {
            let user = { Name = name }
            let request = new HttpRequestMessage()
            request.RequestUri <- Uri($"%s{client.BaseAddress.ToString()}api/login")
            request.Method <- HttpMethod.Post
            request.Content <- new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json")

            let! response = client.SendAsync(request)
            response.EnsureSuccessStatusCode() |> ignore

            let! content =
                response.Content.ReadAsStringAsync()

            return JsonConvert.DeserializeObject<AuthUserModel>(content)
        }

    let requestPost<'TBody, 'TResult>
        (client: HttpClient)
        (body: 'TBody)
        (token: string)
        (path: string)
        : Task<'TResult> =
        task {
            let request = new HttpRequestMessage()
            request.RequestUri <- Uri($"%s{client.BaseAddress.ToString()}api/%s{path}")
            request.Method <- HttpMethod.Post
            request.Content <- new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)

            let! response = client.SendAsync(request)
            response.EnsureSuccessStatusCode() |> ignore
            let! content =
                response.Content.ReadAsStringAsync()

            return JsonConvert.DeserializeObject<'TResult>(content)
        }

    let requestDelete<'TResult>
        (client: HttpClient)
        (token: string)
        (path: string)
        : Task<'TResult> =
        task {
            let request = new HttpRequestMessage()
            request.RequestUri <- Uri($"%s{client.BaseAddress.ToString()}api/%s{path}")
            request.Method <- HttpMethod.Delete
            request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)

            let! response = client.SendAsync(request)
            response.EnsureSuccessStatusCode() |> ignore
            let! content =
                response.Content.ReadAsStringAsync()

            return JsonConvert.DeserializeObject<'TResult>(content)
        }

    let requestPostWithoutBody<'TResult>
        (client: HttpClient)
        (token: string)
        (path: string)
        : Task<'TResult> =
        task {
            let request = new HttpRequestMessage()
            request.RequestUri <- Uri($"%s{client.BaseAddress.ToString()}api/%s{path}")
            request.Method <- HttpMethod.Post
            request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)

            let! response = client.SendAsync(request)
            response.EnsureSuccessStatusCode() |> ignore
            let! content = response.Content.ReadAsStringAsync()

            return JsonConvert.DeserializeObject<'TResult>(content)
        }

    let requestGet<'TResult> (client: HttpClient) (token: string) (path: string) : Task<'TResult> =
        task {
            let request = new HttpRequestMessage()
            request.RequestUri <- Uri($"%s{client.BaseAddress.ToString()}api/%s{path}")
            request.Method <- HttpMethod.Get
            request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)

            let! response = client.SendAsync(request)
            response.EnsureSuccessStatusCode() |> ignore

            let! content = response.Content.ReadAsStringAsync()

            return JsonConvert.DeserializeObject<'TResult>(content)
        }

    
    let private createWebSocketConnection (testServer: TestServer) (token:string) (path: string) =
        task {
            let connection = HubConnectionBuilder()
                                 .WithUrl($"%s{testServer.BaseAddress.ToString()}%s{path}", fun options ->
                                        options.AccessTokenProvider <- (fun _ -> Task.FromResult token)
                                        options.SkipNegotiation <- true
                                        options.Transports <- HttpTransportType.WebSockets
                                        options.WebSocketFactory <- (fun ctx cancellationToken ->
                                                let t = task {
                                                    let webSocketClient = testServer.CreateWebSocketClient()
                                                    let url = $"%s{ctx.Uri.ToString()}?access_token=%s{token}"
                                                    return! webSocketClient.ConnectAsync(Uri(url), cancellationToken)
                                                }
                                                ValueTask<Net.WebSockets.WebSocket>(t)
                                            )
                                        )
                                 .Build()
            do! connection.StartAsync()
            return connection
        }        
    
    let createEventsConnection (testServer: TestServer) (token: string) =
        createWebSocketConnection testServer token "events"
        
    let createSession (client: HttpClient) (token: string) (title: string) =
        requestPost<_, SessionView> client { CreateSession.Title = title } token "sessions"

    let getStory (client: HttpClient) (token: string) (id:Guid) =
        requestGet<StoryView> client token $"stories/%s{id.ToString()}"

    let addStoryToSession (client: HttpClient) (token: string) (session: SessionView) (arg: CreateStory) =
        requestPost<_, SessionView>
                                            client
                                            arg
                                            token
                                            $"Sessions/%s{session.Id.ToString()}/stories"



    let setActiveStory (client: HttpClient) (token: string) (sessionId: Guid) (id: Guid) =
        requestPostWithoutBody<_>
                    client
                    token
                    $"sessions/%s{sessionId.ToString()}/activestory/%s{id.ToString()}"

    let getSession  (client: HttpClient) (token: string) (id: Guid) =
        requestGet<SessionView> client token $"sessions/%s{id.ToString()}"

    let vote  (client: HttpClient) (token: string) (id: Guid) (card: string) =
        requestPost<_, StoryView> client {Card = card} token $"stories/%s{id.ToString()}/vote"

    let closeStory  (client: HttpClient) (token: string) (id: Guid) (arg: CloseStory) =
        requestPost<_,StoryView> client arg token $"stories/%s{id.ToString()}/closed"

    let clearStory  (client: HttpClient) (token: string) (id: Guid)  =
        requestPostWithoutBody<StoryView> client token $"stories/%s{id.ToString()}/cleared"

    let addGroup (client: HttpClient) (token: string) (id: Guid) (name: string) =
        requestPost<_, SessionView> client { CreateGroup.Name = name } token $"sessions/%s{id.ToString()}/groups"

    let removeGroup (client: HttpClient) (token: string) (id: Guid) (groupId: Guid) =
        requestDelete<SessionView> client token $"sessions/%s{id.ToString()}/groups/%s{groupId.ToString()}"

    let moveParticipantToGroup (client: HttpClient) (token: string) (id: Guid) (userId: Guid) (groupId: Guid) =
        requestPost<_,SessionView> client {MoveParticipantToGroup.ParticipantId = userId} token $"sessions/%s{id.ToString()}/groups/%s{groupId.ToString()}/participants"
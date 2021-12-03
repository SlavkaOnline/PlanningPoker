namespace IntegrationTests

open System
open System.Collections.Concurrent
open System.IdentityModel.Tokens.Jwt
open System.Threading
open FSharp.Control
open Gateway.Views
open Swensen.Unquote
open System.Threading.Tasks
open Api
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.AspNetCore.SignalR.Client
open Xunit

[<Collection("Real Server Collection")>]
type ChatTests(fixture: WebApplicationFactory<Program>) =

    let server = fixture.Server
    let apiClient = fixture.CreateClient()

    [<Fact>]
    let ``Send simple message works fine`` () =
        task {

            let userName1 = "User1"
            let userName2 = "User2"
            let group = Guid.NewGuid();
            let message = "simple message"
            
            let! user1 = Helper.login apiClient userName1
            let! user2 = Helper.login apiClient userName1

            let tokenHandler = JwtSecurityTokenHandler()
            let token = tokenHandler.ReadJwtToken(user1.Token)
            let user1Id =  token.Claims |> Seq.filter(fun c -> c.Type = "nameid") |> Seq.tryHead |> Option.map(fun c -> c.Value |> Guid.Parse ) |> Option.defaultValue Guid.Empty
            
            let! userConnection1 = Helper.createEventsConnection server user1.Token
            let! userConnection2 = Helper.createEventsConnection server user2.Token
            
            let tcs = TaskCompletionSource<ChatMessage>()
            let cts = new CancellationTokenSource()
            
            let! stream = userConnection2.StreamAsChannelAsync<ChatMessage>("Chat", group)
            do stream.ReadAllAsync(cts.Token)
                |> AsyncSeq.ofAsyncEnum
                |> AsyncSeq.iterAsync(fun m -> async { return tcs.SetResult m })
                |> Async.StartAsTask
                |> ignore

            do! userConnection1.SendAsync("SendMessage", group, message)

            let! task =
                Task.WhenAny(
                    tcs.Task.ContinueWith(fun (t: Task<ChatMessage>) -> Some t.Result),
                    Task
                        .Delay(TimeSpan.FromSeconds 1.)
                        .ContinueWith(fun _ -> None)
                )
                
            let! result = task

            do cts.Cancel()
            do! userConnection1.StopAsync()
            do! userConnection2.StopAsync()
            
            test
                <@ match result with
                   | Some m -> m.Text = message && Guid.Parse m.Group = group && m.User.Id = user1Id
                   | None -> false @>
        }
        
    [<Fact>]
    let ``Every users has one message`` () =
        task {

            let userName1 = "User1"
            let userName2 = "User2"
            let group = Guid.NewGuid()
            let message = "simple message"
            
            let! user1 = Helper.login apiClient userName1
            let! user2 = Helper.login apiClient userName1

            let! userConnection1 = Helper.createEventsConnection server user1.Token
            let! userConnection2 = Helper.createEventsConnection server user2.Token
            
            let messages = ConcurrentBag();
            let cts = new CancellationTokenSource()
            
            let! stream = userConnection2.StreamAsChannelAsync<ChatMessage>("Chat", group)
            do stream.ReadAllAsync(cts.Token)
                |> AsyncSeq.ofAsyncEnum
                |> AsyncSeq.iterAsync(fun m -> async { return messages.Add m })
                |> Async.StartAsTask
                |> ignore

            do! userConnection1.SendAsync("SendMessage", group, message)

            do! Task.Delay(TimeSpan.FromSeconds 1.)
            do cts.Cancel()
            do! userConnection1.StopAsync()
            do! userConnection2.StopAsync()
            
            test <@ messages.Count = 1 @>
          
        }
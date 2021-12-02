namespace IntegrationTests

open System
open System.Collections.Concurrent
open System.Threading
open Swensen.Unquote
open System.Threading.Tasks
open Api
open Microsoft.AspNetCore.Mvc.Testing
open PlanningPoker.Domain.CommonTypes
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
            let group = "group"
            let message = "simple message"
            
            let! user1 = Helper.login apiClient userName1
            let! user2 = Helper.login apiClient userName1

            let! userConnection1 = Helper.createEventsConnection server user1.Token
            let! userConnection2 = Helper.createEventsConnection server user2.Token

            do! Task.WhenAll(userConnection1.SendAsync("Join", group), userConnection2.SendAsync("Join", group))
            
            let tcs = TaskCompletionSource<string>()

            use _ =
                userConnection2.On<string, string, string>(
                    "chatMessage",
                    (fun _ _ message ->
                        tcs.SetResult message
                        Task.CompletedTask)
                )

            do! userConnection1.SendAsync("SendMessage", group, message)

            let! task =
                Task.WhenAny(
                    tcs.Task.ContinueWith(fun (t: Task<string>) -> Some t.Result),
                    Task
                        .Delay(TimeSpan.FromSeconds 1.)
                        .ContinueWith(fun _ -> None)
                )

            let! result = task

            test
                <@ match result with
                   | Some m -> m = message
                   | None -> false @>
        }
        
    [<Fact>]
    let ``Every users has one message`` () =
        task {

            let userName1 = "User1"
            let userName2 = "User2"
            let group = "group"
            let message = "simple message"
            
            let! user1 = Helper.login apiClient userName1
            let! user2 = Helper.login apiClient userName1

            let! userConnection1 = Helper.createEventsConnection server user1.Token
            let! userConnection2 = Helper.createEventsConnection server user2.Token

            do! Task.WhenAll(userConnection1.SendAsync("Join", group), userConnection2.SendAsync("Join", group))
            
            let messages = ConcurrentBag();

            use _ =
                userConnection2.On<string, string, string>(
                    "chatMessage",
                    (fun _ _ message ->
                        messages.Add(message)
                        Task.CompletedTask)
                )
            
            do! userConnection1.SendAsync("SendMessage", group, message)
            do! Task.Delay(TimeSpan.FromSeconds 1.)
            
            test <@ messages.Count = 1 @>
          
        }
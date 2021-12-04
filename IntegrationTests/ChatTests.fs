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
    let pause = TimeSpan.FromSeconds 1

    [<Fact>]
    let ``Send simple message works fine`` () =
        task {

            let userName1 = "User1"
            let userName2 = "User2"
            let group = Guid.NewGuid()
            let message = "simple message"

            let! user1 = Helper.login apiClient userName1
            let! user2 = Helper.login apiClient userName1

            let tokenHandler = JwtSecurityTokenHandler()
            let token = tokenHandler.ReadJwtToken(user1.Token)

            let user1Id =
                token.Claims
                |> Seq.filter (fun c -> c.Type = "nameid")
                |> Seq.tryHead
                |> Option.map (fun c -> c.Value |> Guid.Parse)
                |> Option.defaultValue Guid.Empty

            let! userConnection1 = Helper.createEventsConnection server user1.Token
            let! userConnection2 = Helper.createEventsConnection server user2.Token

            let! stream = userConnection2.StreamAsChannelAsync<ChatMessage>("Chat", group)

            do! Task.Delay(pause)
            do! userConnection1.SendAsync("SendMessage", group, message)

            let! events =
                async {
                    let! arr =
                        stream.ReadAllAsync()
                        |> AsyncSeq.ofAsyncEnum
                        |> AsyncSeq.bufferByCountAndTime 1 (int pause.TotalMilliseconds)
                        |> AsyncSeq.take 1
                        |> AsyncSeq.toArrayAsync

                    return arr.[0]
                }


            let result = events |> Array.tryHead

            do! userConnection1.StopAsync()
            do! userConnection2.StopAsync()

            test
                <@ match result with
                   | Some m ->
                       m.Text = message
                       && Guid.Parse m.Group = group
                       && m.User.Id = user1Id
                   | None -> false @>
        }

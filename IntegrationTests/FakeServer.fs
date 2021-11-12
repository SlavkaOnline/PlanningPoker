namespace IntegrationTests

open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting
open WebApi
open Xunit

module FakeServer =
    open Microsoft.AspNetCore.Mvc.Testing

    type RealServerFixture() =
        let host = WebApi.Program.CreateHostBuilder([||])

        let mutable serverBuild : IHost option = None

        member _.Start(url: string) =
            match serverBuild with
            | None ->
                let h =
                    host
                        .ConfigureWebHostDefaults(fun builder -> builder.UseUrls(url) |> ignore)
                        .Build()

                serverBuild <- Some h
                async { do h.Run() } |> Async.Start
            | Some _ -> ()

        interface IAsyncLifetime with
            member _.DisposeAsync() =
                match serverBuild with
                | Some s -> s.StopAsync()
                | None -> Task.CompletedTask

            member _.InitializeAsync() = Task.CompletedTask


    [<CollectionDefinition("Real Server Collection")>]
    type RealServerCollectionFixture() =
        interface ICollectionFixture<WebApplicationFactory<Program>>

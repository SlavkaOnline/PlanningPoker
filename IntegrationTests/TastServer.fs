namespace IntegrationTests

open System
open System.Threading.Channels
open System.Threading.Tasks
open Api
open Api.DomainEventsHandler
open Databases
open Databases.Models
open FSharp.Control
open LinqToDB
open LinqToDB.Data
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Identity
open Microsoft.Extensions.DependencyInjection
open LinqToDB.EntityFrameworkCore
open Microsoft.FSharp.Core
open Orleans
open PlanningPoker.Domain
open Xunit
open Microsoft.EntityFrameworkCore
open System.Collections.Generic
open Orleans.Streams

module TestServer =
    open Microsoft.AspNetCore.Mvc.Testing


    [<RequireQualifiedAccess>]
    module DBHelper =
        [<Literal>]
        let clearTablesSql =
            """
            CREATE OR REPLACE FUNCTION truncate_tables() RETURNS void AS $$
            DECLARE
                statements CURSOR FOR
                    SELECT tablename FROM pg_tables
                    WHERE schemaname = 'public' and tablename not in ('__EFMigrationsHistory', 'orleansquery', 'orleansstorage');
            BEGIN
                FOR stmt IN statements LOOP
                    EXECUTE 'TRUNCATE TABLE ' || quote_ident(stmt.tablename) || ' CASCADE;';
                END LOOP;
            END;
            $$ LANGUAGE plpgsql;
            SELECT truncate_tables()
    """

    type Seeder(db: DataConnection, userManager: UserManager<Account>) =

        member _.Seed<'TEntity when 'TEntity: not struct>(entity: 'TEntity) : Task<'TEntity> =
            db
                .GetTable<'TEntity>()
                .InsertWithOutputAsync(entity)

        member _.Seed<'TEntity when 'TEntity: not struct>(entity: 'TEntity array) : Task<BulkCopyRowsCopied> =
            db.BulkCopyAsync(entity)

        member this.Account(id: Guid, userName: string, email: string, name: string) =
            userManager.CreateAsync(Account(id, userName, email, name))

        interface IAsyncDisposable with
            member this.DisposeAsync() = db.DisposeAsync()

    type EventWaiter<'TEvent>(stream: ChannelReader<'TEvent>) =
        
        member _.WaitMessages() =
            async {
                let! arr =
                    stream.ReadAllAsync()
                    |> AsyncSeq.ofAsyncEnum
                    |> AsyncSeq.bufferByCountAndTime 1 2000 
                    |> AsyncSeq.take 1
                    |> AsyncSeq.toArrayAsync
                return arr.[0] 
            }
            |> Async.StartAsTask
            
            

    type CustomWebApplicationFactory<'TStartup when 'TStartup: not struct>() =
        inherit WebApplicationFactory<'TStartup>()

        member this.Seeder =
            Seeder(this.Db.CreateLinq2DbConnectionDetached(), this.UserManager)

        member private this.Scope =
            this
                .Services
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope()

        member private this.UserManager = this.Scope.ServiceProvider.GetRequiredService<UserManager<Account>>()
        
        member this.Db: DataBaseContext =
            this.Scope.ServiceProvider.GetRequiredService<DataBaseContext>()

        member this.GetService<'TService>() =
            this.Scope.ServiceProvider.GetRequiredService<'TService>()


        member this.CreateEventHandler<'TEvent>(stream: CommonTypes.Streams.Stream) =
            task {
            let client = this.Scope.ServiceProvider.GetRequiredService<IClusterClient>()
            let channel = Channel.CreateUnbounded<'TEvent>()
            let! eventStream = client
                                  .GetStreamProvider("SMS")
                                  .GetStream<'TEvent>(stream.Id, stream.Namespace)
                                  .SubscribeAsync(fun event t -> channel.Writer.WriteAsync(event).AsTask())
            return EventWaiter(channel.Reader)
            }
        
        
        member this.InitAsync() : Task =
            task {
                do! this.Db.Database.MigrateAsync()
                do this.Db.CreateOrleansTables()
            }

        member this.ResetAsync() : Task =
            this.Db.Database.ExecuteSqlRawAsync(DBHelper.clearTablesSql)

        override this.ConfigureWebHost(builder: IWebHostBuilder) =
            builder.ConfigureServices (fun (services: IServiceCollection) ->
                let sp = services.BuildServiceProvider()
                use scope = sp.CreateScope()
                let scopedServices = scope.ServiceProvider

                let context =
                    scopedServices.GetRequiredService<DataBaseContext>()

                context.Database.EnsureDeleted() |> ignore)
            |> ignore
        override this.DisposeAsync() =
            let b = base.DisposeAsync()

            task {
                let! _ = (this.Seeder :> IAsyncDisposable).DisposeAsync()
                let! _ = this.Db.DisposeAsync()
                this.Scope.Dispose()
                return! b
            }
            |> ValueTask


    [<CollectionDefinition("Real Server Collection")>]
    type RealServerCollectionFixture() =
        interface ICollectionFixture<CustomWebApplicationFactory<Program>>


    [<Collection("Real Server Collection")>]
    type TestServerBase(fixture: CustomWebApplicationFactory<Program>) =
        interface IAsyncLifetime with
            member this.DisposeAsync() = Task.CompletedTask

            member this.InitializeAsync() = fixture.ResetAsync()
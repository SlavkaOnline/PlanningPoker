namespace IntegrationTests

open System
open System.Threading.Tasks
open Api
open Databases
open Databases.Models
open LinqToDB
open LinqToDB.Data
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open LinqToDB.EntityFrameworkCore
open Xunit

module FakeServer =
    open Microsoft.AspNetCore.Mvc.Testing

    type Seeder(db: DataConnection) =

        member _.Seed<'TEntity when 'TEntity: not struct>(entity: 'TEntity) : Task<'TEntity> =
            db
                .GetTable<'TEntity>()
                .InsertWithOutputAsync(entity)

        member _.Seed<'TEntity when 'TEntity: not struct>(entity: 'TEntity array) : Task<BulkCopyRowsCopied> =
            db.BulkCopyAsync(entity)

        member this.Account(id: Guid, name: string, email: string) = this.Seed(Account(id, name, email))

        interface IAsyncDisposable with
            member this.DisposeAsync() = db.DisposeAsync()


    type CustomWebApplicationFactory<'TStartup when 'TStartup: not struct>() as this =
        inherit WebApplicationFactory<'TStartup>()

        member this.Seeder =
            Seeder(this.Db.CreateLinq2DbConnectionDetached())

        member private this.Scope =
            this
                .Services
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope()

        member this.Db: DataBaseContext =
            this.Scope.ServiceProvider.GetRequiredService<DataBaseContext>()

        member this.GetService<'TService>() =
            this.Scope.ServiceProvider.GetRequiredService<'TService>()

        
        override this.ConfigureWebHost(builder: IWebHostBuilder) =                
                builder.ConfigureServices(fun (services: IServiceCollection) ->
                    let sp = services.BuildServiceProvider()
                    use scope = sp.CreateScope()
                    let scopedServices = scope.ServiceProvider
                    let context = scopedServices.GetRequiredService<DataBaseContext>()
                    context.Database.EnsureDeleted() |> ignore
                    ) |> ignore        

        override this.DisposeAsync() =
            let b = base.DisposeAsync()

            task {
                let! _ = (this.Seeder :> IAsyncDisposable).DisposeAsync()
                let! _ = this.Db.Database.EnsureDeletedAsync()
                let! _ = this.Db.DisposeAsync()
                this.Scope.Dispose()
                return! b
            }
            |> ValueTask



    [<CollectionDefinition("Real Server Collection")>]
    type RealServerCollectionFixture() =
        interface ICollectionFixture<CustomWebApplicationFactory<Program>>

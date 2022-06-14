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
open Microsoft.FSharp.Core
open Xunit
open Microsoft.EntityFrameworkCore

module FakeServer =
    open Microsoft.AspNetCore.Mvc.Testing

    
    [<RequireQualifiedAccess>]
    module DBHelper =
        [<Literal>]
        let clearTablesSql = """
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

        member this.Seeder = Seeder(this.Db.CreateLinq2DbConnectionDetached())

        member private this.Scope =
            this
                .Services
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope()

        member this.Db: DataBaseContext =
            this.Scope.ServiceProvider.GetRequiredService<DataBaseContext>()

        member this.GetService<'TService>() =
            this.Scope.ServiceProvider.GetRequiredService<'TService>()


        member this.InitAsync(): Task =
            task {
                do! this.Db.Database.MigrateAsync()
                do this.Db.CreateOrleansTables()
            }
        
        member this.ResetAsync(): Task =
            this.Db.Database.ExecuteSqlRawAsync(DBHelper.clearTablesSql)
       

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
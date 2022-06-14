namespace IntegrationTests

open System
open Api
open Api.Application
open Api.Commands
open Gateway.Views
open IntegrationTests.FakeServer
open Xunit
open Swensen.Unquote
open Microsoft.EntityFrameworkCore


[<Collection("Real Server Collection")>]
type ApiTests(fixture: CustomWebApplicationFactory<Program>) =

    let seeder = fixture.Seeder

    [<Fact>]
    let ``Create new account`` () =
        task {
            let cmd =
                fixture.GetService<ICommand<GetOrCreateNewAccountCommandArgs, AuthUser>>()

            let arg =
                { Id = Guid.NewGuid()
                  Email = "test@gmail.com"
                  Name = "test"
                  Picture = "pic" }

            let! _ = cmd.Execute arg

            let! account =
                fixture
                    .Db
                    .Users
                    .AsNoTracking()
                    .SingleAsync(fun x -> x.Email = arg.Email)

            test <@ arg.Id = account.Id @>
            test <@ arg.Email = account.Email @>
            test <@ arg.Name = account.UserName @>

        }

    [<Fact>]
    let ``Get existed account`` () =
        task {

            let id = Guid.NewGuid()
            let email = "test1@gmail.com"
            let name = "test"
            let pic = "pic"
        
            let! _ = seeder.Account(id, name, email)

            let cmd =
                fixture.GetService<ICommand<GetOrCreateNewAccountCommandArgs, AuthUser>>()

            let arg =
                { Id = id
                  Email = email
                  Name = name
                  Picture = pic }

            let! userResult = cmd.Execute arg

            test
                <@ match userResult with
                   | Error _ -> false
                   | Ok _ -> true @>
        }

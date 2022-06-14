namespace IntegrationTests

open System
open System.Threading.Tasks
open Api
open Api.Application
open Api.Commands
open Gateway.Views
open IntegrationTests.TestServer
open Xunit
open Swensen.Unquote
open Microsoft.EntityFrameworkCore

type AccountTests(fixture: CustomWebApplicationFactory<Program>) =
    inherit TestServerBase(fixture)
    
    let seeder = fixture.Seeder

    [<Fact>]
    let ``Create new account`` () =
        task {
            let cmd =
                fixture.GetService<ICommand<GetOrCreateNewAccountCommandArgs, AuthUser>>()

            let arg =
                { Id = Guid.NewGuid()
                  Email = "test@gmail.com"
                  UserName = "test_"
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
            test <@ arg.UserName = account.UserName @>
            test <@ arg.Name = account.Name @>
        }

    [<Fact>]
    let ``Get existed account`` () =
        task {

            let id = Guid.NewGuid()
            let email = "test@gmail.com"
            let userName = "test"
            let name = "test"
            let pic = "pic"
        
            let! _ = seeder.Account(id, userName, email, name)

            let cmd =
                fixture.GetService<ICommand<GetOrCreateNewAccountCommandArgs, AuthUser>>()

            let arg =
                { Id = id
                  Email = email
                  UserName = "test_"
                  Name = name
                  Picture = pic }

            let! userResult = cmd.Execute arg

            test
                <@ match userResult with
                   | Error _ -> false
                   | Ok _ -> true @>
        }
        
    [<Fact>]
    let ``Update account name`` () =
        task {

            let id = Guid.NewGuid()
            let email = "test@gmail.com"
            let name = "test"
            let userName = "test_"
            let pic = "pic"
        
            let! _ = seeder.Account(id, userName, email, name)

            let cmd =
                fixture.GetService<ICommand<GetOrCreateNewAccountCommandArgs, AuthUser>>()

            let arg =
                { Id = id
                  Email = email
                  UserName = "test__"
                  Name = "__test"
                  Picture = pic }

            let! _ = cmd.Execute arg

            let! account =
                fixture
                    .Db
                    .Users
                    .AsNoTracking()
                    .SingleAsync(fun x -> x.Email = arg.Email)
                    
            test <@ arg.UserName = account.UserName @>        
            test <@ arg.Name = account.Name @>        
        }
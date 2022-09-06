namespace IntegrationTests

open System
open Api
open Gateway
open IntegrationTests.TestServer
open Xunit
open Views
open Requests
open Swensen.Unquote

type AccountSessionsTests(fixture: CustomWebApplicationFactory<Program>) as this =
  inherit TestServerBase(fixture)
  
  [<Fact>]
  let ``Get sessions page by date``() =
      task {
        let! user = Helper.login this.Client "Test" 
        let now = DateTime.Now;
        let request = {
          Filter = Some { AccountSessionsFilter.AccountId = Helper.getUserId user} 
          Token = None
          Take = 10
        }
        
        let! _ = this.Seeder.AccountSessions(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Test1", now)
        let! _ = this.Seeder.AccountSessions(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Test2", now)
        let! result = Helper.getAccountSessions<_, DateTime, AccountSessions>
                        this.Client
                        user.Token
                        (Helper.getUserId user)
                        request
        let expected = Array.map (fun (x:AccountSessions) -> x.Name) result.View
        test <@
              expected = [| "Test2"; "Test1"|]
               @>
  }
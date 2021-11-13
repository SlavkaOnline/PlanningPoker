namespace IntegrationTests

open Api
open Xunit

module FakeServer =
    open Microsoft.AspNetCore.Mvc.Testing

    [<CollectionDefinition("Real Server Collection")>]
    type RealServerCollectionFixture() =
        interface ICollectionFixture<WebApplicationFactory<Program>>

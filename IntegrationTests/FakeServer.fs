namespace IntegrationTests

open Api
open Databases
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Xunit

module FakeServer =
    open Microsoft.AspNetCore.Mvc.Testing

    
    type CustomWebApplicationFactory<'TStartup when 'TStartup : not struct> () = 
        inherit WebApplicationFactory<'TStartup>() 
            
            override this.ConfigureWebHost(builder: IWebHostBuilder) =
                
                builder.ConfigureServices(fun (services: IServiceCollection) ->
                    let sp = services.BuildServiceProvider()
                    use scope = sp.CreateScope()
                    let scopedServices = scope.ServiceProvider
                    let context = scopedServices.GetRequiredService<DataBaseContext>()
                    context.Database.EnsureDeleted() |> ignore
                    ) |> ignore
                

            
    
    [<CollectionDefinition("Real Server Collection")>]
    type RealServerCollectionFixture() =
        interface ICollectionFixture<CustomWebApplicationFactory<Program>>

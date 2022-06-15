namespace IntegrationTests

open System
open GrainInterfaces
open Microsoft.Extensions.Configuration
open Orleans.Configuration
open Orleans.Hosting
open Orleans.TestingHost
open Orleans
open Xunit



type AddApplicationParts () =

        interface ISiloConfigurator with
          member _.Configure (hostBuilder: ISiloBuilder) = 
             hostBuilder
               .AddMemoryGrainStorage("Database")
               .AddMemoryGrainStorage("PubSubStore")
               .AddLogStorageBasedLogConsistencyProvider()
               .ConfigureApplicationParts(fun parts ->
                                                 parts.AddApplicationPart(typeof<Grains.SessionGrain>.Assembly)
                                                  .AddApplicationPart(typeof<ISessionGrain>.Assembly)
                                                  .WithCodeGeneration() |> ignore )

                 .AddSimpleMessageStreamProvider("SMS", fun (configurator: SimpleMessageStreamProviderOptions) -> configurator.FireAndForgetDelivery <- true)
                 .UseLocalhostClustering()
             |> ignore



type OrleansClusterFixture() =
    let builder = TestClusterBuilder()
    do builder.Options.ServiceId <- Guid.NewGuid().ToString()
    do builder.Options.InitialSilosCount <- (int16 1)
    do builder.AddSiloBuilderConfigurator<AddApplicationParts>() |> ignore
    let cluster = builder.Build()
    do cluster.Deploy()

    member this.Cluster with get () = cluster

    interface IDisposable with
      member this.Dispose() =
        cluster.StopAllSilos()


[<CollectionDefinition("Orleans")>]
type ClusterCollection() =

  interface ICollectionFixture<OrleansClusterFixture>
 
[<Collection("Orleans")>]  
type OrleansTestServer(fixture: OrleansClusterFixture) =
  class end
namespace IntegrationTests

open System
open GrainInterfaces
open Microsoft.Extensions.Configuration
open Orleans.Configuration
open Orleans.Hosting
open Orleans.TestingHost
open Orleans
open Orleans.TestingHost
open Orleans
open Microsoft.Extensions.DependencyInjection
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



type ClusterFixture() =
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


[<CollectionDefinition("silo")>]
type ClusterCollection() =

  interface ICollectionFixture<ClusterFixture>
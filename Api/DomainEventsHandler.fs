namespace Api

open System
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Orleans
open Orleans.Streams
open PlanningPoker.Domain
open System.Runtime.CompilerServices

module DomainEventsHandler =

  type IDomainEventHandler<'TEvent> =
    abstract member Handle: 'TEvent -> Task
  
  type Handler =
    { Stream: CommonTypes.Streams.Stream
      Handler: System.Type }

  type EventHandlerBuilder<'TEvent>() =
    let handlers: ResizeArray<Handler> = ResizeArray()

    member this.AddHandler<'THandler when 'THandler :> IDomainEventHandler<'TEvent>>(stream: CommonTypes.Streams.Stream) =
      handlers.Add { Stream = stream; Handler = typeof<'THandler>  }
      this

    member _.Handlers = handlers.AsReadOnly()


  type EventHandler<'TEvent>(
    client: IClusterClient,
    sp: IServiceProvider,
    handlersBuilder: EventHandlerBuilder<'TEvent>) =

    interface IHostedService with
      member this.StartAsync(cancellationToken) =
        Task.WhenAll(
          handlersBuilder.Handlers
          |> Seq.map (fun h ->
            client
              .GetStreamProvider("SMS")
              .GetStream<'TEvent>(h.Stream.Id, h.Stream.Namespace)
              .SubscribeAsync(fun event _ ->
                let handler = sp.GetService(h.Handler) :?> IDomainEventHandler<'TEvent>
                handler.Handle event))
        )

      member this.StopAsync(cancellationToken) = Task.CompletedTask
  
  [<Extension>]
  type EventHandlerExtensions() =
      
    [<Extension>]
    static member AddEventHandlers<'TEvent> (services: IServiceCollection, builder: EventHandlerBuilder<'TEvent> -> EventHandlerBuilder<'TEvent>): IServiceCollection =
        services
          .AddSingleton(builder(EventHandlerBuilder<'TEvent>()))
          .AddHostedService<EventHandler<'TEvent>>()
        
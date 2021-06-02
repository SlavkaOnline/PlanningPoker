namespace EventsDelivery

open System
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Channels
open FSharp.Control
open FSharp.UMX
open Gateway.Views
open GrainInterfaces
open Microsoft.AspNetCore.SignalR
open Newtonsoft.Json
open Orleans
open Orleans.Streams
open PlanningPoker.Domain
open System.Security.Claims
open PlanningPoker.Domain.CommonTypes

module EventsDeliveryHub =

    type Event =
        { EntityId: Guid
          Order: int32
          Type: string
          Payload: string }

    let private createEvent entityId order _type payload =
        { EntityId = entityId
          Order = order
          Type = _type
          Payload = payload }

    let convertSessionEvent (entityId: Guid) (domainEvent: EventView<Session.Event>) : Event =
        let create = createEvent entityId domainEvent.Order

        match domainEvent.Payload with
        | Session.Event.ActiveStorySet id ->
            create "ActiveStorySet"
            <| JsonConvert.SerializeObject({| id = %id |})
        | Session.Event.StoryAdded id ->
            create "StoryAdded"
            <| JsonConvert.SerializeObject({| id = %id |})
        | Session.Event.ParticipantAdded participant ->
            create "ParticipantAdded"
            <| JsonConvert.SerializeObject(
                {| id = %participant.Id
                   name = participant.Name |}
            )
        | Session.Event.ParticipantRemoved participant ->
            create "ParticipantRemoved"
            <| JsonConvert.SerializeObject(
                {| id = %participant.Id
                   name = participant.Name |}
            )
        | Session.Event.Started _ -> create "Started" ""


    let convertStoryEvent (entityId: Guid) (domainEvent: EventView<Story.Event>) : Event =
        let create = createEvent entityId domainEvent.Order

        match domainEvent.Payload with
        | Story.Event.Voted (user, _) -> create "Voted" <| JsonConvert.SerializeObject({| id = %user.Id; name = user.Name |})
        | Story.Event.VoteRemoved user -> create "VoteRemoved" <| JsonConvert.SerializeObject({| id = %user.Id; name = user.Name |})
        | Story.Event.StoryClosed _ -> create "StoryClosed" ""
        | Story.Event.StoryStarted _ -> create "StoryStarted" ""


    type DomainEventHub(client: IClusterClient) =
        inherit Hub()


        member private this.CreateSubscriptionsToEvent<'TEvent>
            (
                id: string,
                version: int32,
                eventConverter: Guid -> EventView<'TEvent> -> Event,
                [<EnumeratorCancellation>] cancellationToken: CancellationToken
            ) : System.Collections.Generic.IAsyncEnumerable<Event> =
            let guid = Guid.Parse(id)

            let grain =
                client.GetGrain<IDomainGrain<'TEvent>>(guid)

            let bufferChannel =
                Channel.CreateUnbounded<EventView<'TEvent>>()

            let eventsChannel =
                Channel.CreateUnbounded<EventView<'TEvent>>()

            let task =
                async {
                    let! sub =
                        client
                            .GetStreamProvider("SMS")
                            .GetStream<EventView<'TEvent>>(guid, "DomainEvents")
                            .SubscribeAsync(fun event token -> bufferChannel.Writer.WriteAsync(event).AsTask())
                        |> Async.AwaitTask

                    cancellationToken.Register
                        (fun _ ->
                            sub.UnsubscribeAsync()
                            |> Async.AwaitTask
                            |> Async.RunSynchronously)
                    |> ignore

                    let! events = grain.GetEventsAfter(version) |> Async.AwaitTask

                    let lastVersion =
                        if events.Count > 0 then
                            events.Item(events.Count - 1).Order
                        else
                            0

                    for e in events do
                        do!
                            eventsChannel.Writer.WriteAsync(e).AsTask()
                            |> Async.AwaitTask

                    do!
                        bufferChannel.Reader.ReadAllAsync()
                        |> AsyncSeq.ofAsyncEnum
                        |> AsyncSeq.filter (fun e -> e.Order > lastVersion)
                        |> AsyncSeq.iterAsync
                            (fun e ->
                                eventsChannel.Writer.WriteAsync(e).AsTask()
                                |> Async.AwaitTask)
                }

            Async.StartAsTask(task, ?taskCreationOptions = None, ?cancellationToken = Some cancellationToken)
            |> ignore

            eventsChannel.Reader.ReadAllAsync()
            |> AsyncSeq.ofAsyncEnum
            |> AsyncSeq.map (fun e -> eventConverter guid e)
            |> AsyncSeq.toAsyncEnum


        member this.Session
            (
                id: string,
                version: int32,
                [<EnumeratorCancellation>] cancellationToken: CancellationToken
            ) : System.Collections.Generic.IAsyncEnumerable<Event> =
            let session =
                client.GetGrain<ISessionGrain>(Guid.Parse(id))

            let userId =
                Seq.find (fun (c: Claim) -> c.Type = ClaimTypes.NameIdentifier) this.Context.User.Claims

            let userName =
                Seq.find (fun (c: Claim) -> c.Type = ClaimTypes.GivenName) this.Context.User.Claims

            let user =
                { User.Id = %(Guid.Parse(userId.Value))
                  Name = userName.Value }

            cancellationToken.Register(fun () -> session.RemoveParticipant(%user.Id) |> ignore)
            |> ignore

            async {
                do!
                    session.AddParticipant(user)
                    |> Async.AwaitTask
                    |> Async.Ignore

                return this.CreateSubscriptionsToEvent(id, version, convertSessionEvent, cancellationToken)
            }
            |> Async.RunSynchronously

        member this.Story
            (
                id: string,
                version: int32,
                [<EnumeratorCancellation>] cancellationToken: CancellationToken
            ) : System.Collections.Generic.IAsyncEnumerable<Event> =
            this.CreateSubscriptionsToEvent(id, version, convertStoryEvent, cancellationToken)

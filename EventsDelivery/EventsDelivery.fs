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
open Newtonsoft.Json.Serialization
open Orleans
open Orleans.Streams
open PlanningPoker.Domain
open System.Security.Claims
open PlanningPoker.Domain.CommonTypes
open System.Threading.Tasks

module EventsDeliveryHub =

    let private jsonSerializerSettings = JsonSerializerSettings()
    jsonSerializerSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()

    let private toJson arg =
        JsonConvert.SerializeObject(arg, jsonSerializerSettings)

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
        | Session.Event.ActiveStorySet id -> create "ActiveStorySet" <| toJson {| id = %id |}
        | Session.Event.StoryAdded id -> create "StoryAdded" <| toJson {| id = %id |}

        | Session.Event.ParticipantAdded (participant, groupId) ->
            create "ParticipantAdded"
            <| toJson
                {| id = %participant.Id
                   name = participant.Name
                   picture = participant.Picture |> Option.defaultValue ""
                   groupId = groupId |}

        | Session.Event.ParticipantRemoved participant ->
            create "ParticipantRemoved"
            <| toJson
                {| id = %participant.Id
                   name = participant.Name |}
        | Session.Event.Started _ -> create "Started" ""

        | Session.Event.GroupAdded group ->
            create "GroupAdded"
            <| toJson {| id = group.Id; name = group.Name |}
        | Session.Event.GroupRemoved group ->
            create "GroupRemoved"
            <| toJson {| id = group.Id; name = group.Name |}

        | Session.Event.ParticipantMovedToGroup (user, group) ->
            create "ParticipantMovedToGroup"
            <| toJson {| group = group; user = user |}


    let convertStoryEvent (entityId: Guid) (domainEvent: EventView<Story.Event>) : Event =
        let create = createEvent entityId domainEvent.Order

        match domainEvent.Payload with
        | Story.Event.Voted (user, _) ->
            create "Voted"
            <| toJson {| id = %user.Id; name = user.Name |}
        | Story.Event.VoteRemoved user ->
            create "VoteRemoved"
            <| toJson {| id = %user.Id; name = user.Name |}
        | Story.Event.StoryClosed _ -> create "StoryClosed" ""
        | Story.Event.StoryConfigured _ -> create "StoryConfigured" ""
        | Story.Event.ActiveSet dt -> create "ActiveSet" <| toJson {| startedAt = dt |}
        | Story.Event.Cleared dt -> create "Cleared" <| toJson {| startedAt = dt |}
        | Story.Paused _ -> create "Paused" <| toJson {|  |}


    type DomainEventHub(client: IClusterClient) =
        inherit Hub()

        member private this.CreateSubscriptionsToEvent<'TEvent>
            (
                id: string,
                version: int32,
                eventConverter: Guid -> EventView<'TEvent> -> Event,
                [<EnumeratorCancellation>] cancellationToken: CancellationToken
            ) : Task<System.Collections.Generic.IAsyncEnumerable<Event>> =
            task {
                let guid = Guid.Parse(id)

                let grain =
                    client.GetGrain<IDomainGrain<'TEvent>>(guid)

                let bufferChannel =
                    Channel.CreateUnbounded<EventView<'TEvent>>()

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
                    events
                    |> Seq.tryLast
                    |> Option.map (fun e -> e.Order)
                    |> Option.defaultValue 0

                return
                    asyncSeq {
                        for e in events do
                            e

                        yield!
                            bufferChannel.Reader.ReadAllAsync(cancellationToken)
                            |> AsyncSeq.ofAsyncEnum
                            |> AsyncSeq.filter (fun e -> e.Order > lastVersion)
                    }
                    |> AsyncSeq.map (eventConverter guid)
                    |> AsyncSeq.toAsyncEnum
            }




        member this.Session
            (
                id: string,
                version: int32,
                [<EnumeratorCancellation>] cancellationToken: CancellationToken
            ) : Task<System.Collections.Generic.IAsyncEnumerable<Event>> =
            task {
                let session =
                    client.GetGrain<ISessionGrain>(Guid.Parse(id))

                let userId =
                    Seq.find (fun (c: Claim) -> c.Type = ClaimTypes.NameIdentifier) this.Context.User.Claims

                let userName =
                    Seq.find (fun (c: Claim) -> c.Type = ClaimTypes.GivenName) this.Context.User.Claims

                let picture =
                    Seq.tryFind (fun (c: Claim) -> c.Type = "picture") this.Context.User.Claims

                let user =
                    { User.Id = %(Guid.Parse(userId.Value))
                      Name = userName.Value
                      Picture = picture |> Option.map (fun c -> c.Value) }

                cancellationToken.Register(fun () -> session.RemoveParticipant(%user.Id) |> ignore)
                |> ignore

                let! _ = session.AddParticipant(user)

                return! this.CreateSubscriptionsToEvent(id, version, convertSessionEvent, cancellationToken)

            }

        member this.Story
            (
                id: string,
                version: int32,
                [<EnumeratorCancellation>] cancellationToken: CancellationToken
            ) : Task<System.Collections.Generic.IAsyncEnumerable<Event>> =
            this.CreateSubscriptionsToEvent(id, version, convertStoryEvent, cancellationToken)

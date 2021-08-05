module PlanningTelegramBot

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks
open FSharp.Control
open FSharp.UMX
open Gateway.Views
open GrainInterfaces
open Microsoft.Extensions.Logging
open Orleans
open Orleans.Streams
open PlanningPoker.Domain
open Telegram.Bot
open Telegram.Bot.Extensions.Polling
open Telegram.Bot.Types
open Orleans.Streams.PubSub
open Telegram.Bot.Types.Enums
open FSharp.UMX
open CommonTypes
open FSharp.Control.Tasks.V2

[<RequireQualifiedAccess>]
module internal BotHelper =

    let parseMessage (message: string) =
        let prepareCmd (cmd: string) = cmd.Replace("/", "").ToUpper()
        let parts = message.Split(" ", StringSplitOptions.RemoveEmptyEntries)
        if parts.Length = 1 then
            prepareCmd parts.[0], [||]
        else
            prepareCmd parts.[0], parts.[1..]

    let (|SessionGuid|_|) (session: string) =
            let result, _ = Guid.TryParse session
            if result then Some SessionGuid else None

[<RequireQualifiedAccess>]
module internal Command =

    [<Literal>]
    let connect = "CONNECT"


[<RequireQualifiedAccess>]
module internal UserAgent =

    type Agent = {
        TelegramChatId: int64
        User: User
        PollStoryMap: Dictionary<string, Guid>
        SessionSubscription: StreamSubscriptionHandle<EventView<Session.Event>> option
        StorySubscription: (StreamSubscriptionHandle<EventView<Story.Event>> * CancellationTokenSource) option
    }

    type Command =
        | ConnectToSession of Guid
        | ConnectToStory of Guid
        | ViewStory of Guid
        | Disconnect

    let create chatId user (botClient: ITelegramBotClient) (siloClient: IClusterClient) =
        let agent = {
            TelegramChatId = chatId
            User = user
            PollStoryMap = Dictionary<string, Guid>()
            SessionSubscription = None
            StorySubscription = None
        }

        let connectToStory state storyId = task {
                 let storyGrain = siloClient.GetGrain<IStoryGrain> storyId
                 let! story = storyGrain.GetState state.User

                 if story.IsClosed then
                     return state

                 else
                     let! msg = botClient.SendPollAsync((ChatId chatId), $"Current story %s{story.Title}", story.Cards, false, PollType.Regular, allowSendingWithoutReply = true)
                     state.PollStoryMap.Add(msg.Poll.Id, storyId)

                     let bufferChannel = Channel.CreateUnbounded<EventView<Story.Event>>()
                     let eventsChannel = Channel.CreateUnbounded<EventView<Story.Event>>()

                     let cts = new CancellationTokenSource()
                     let token = cts.Token


                     let! sub = siloClient
                                    .GetStreamProvider("SMS")
                                    .GetStream<EventView<Story.Event>>(storyId, "DomainEvents")
                                    .SubscribeAsync(fun event token -> bufferChannel.Writer.WriteAsync(event).AsTask())

                     let! events = storyGrain.GetEventsAfter(story.Version)
                     let lastVersion =
                        if events.Count > 0 then
                            events.Item(events.Count - 1).Order
                        else
                            0
                     for e in events do
                        do! eventsChannel.Writer.WriteAsync(e).AsTask()


                     Async.Start (bufferChannel.Reader.ReadAllAsync(token)
                                 |> AsyncSeq.ofAsyncEnum
                                 |> AsyncSeq.append (eventsChannel.Reader.ReadAllAsync(token)
                                                     |> AsyncSeq.ofAsyncEnum
                                                     |> AsyncSeq.filter(fun e -> e.Order > lastVersion))
                                 |> AsyncSeq.iterAsync(fun e -> async {
                                        match e.Payload with
                                        | Story.StoryClosed (result, _, _) ->
                                                do! botClient.DeleteMessageAsync((ChatId chatId), msg.MessageId) |> Async.AwaitTask
                                                let! _ = botClient.SendTextMessageAsync((ChatId chatId), $"The result of story %s{story.Title} is %s{%result}") |> Async.AwaitTask
                                                cts.Cancel()
                                                return ()
                                        | _ -> return ()
                                      }
                                     ), token)
                     return { state with StorySubscription = Some(sub, cts )}
            }

        let eventHandler state (event: Session.Event) (inbox: MailboxProcessor<Command>): Task = upcast task {
                              match event with
                              | Session.Event.ActiveStorySet e ->
                                     let! _ = inbox.Post <| ConnectToStory %e
                                     return! Task.CompletedTask
                              | _ -> return!  Task.CompletedTask
            }

        let connectToSession state sessionId (inbox: MailboxProcessor<Command>) = task {
                let! sub = siloClient
                               .GetStreamProvider("SMS")
                               .GetStream<EventView<Session.Event>>(sessionId, "DomainEvents")
                               .SubscribeAsync(fun event token -> eventHandler state event.Payload inbox)

                return {state with SessionSubscription = Some sub}
        }

        MailboxProcessor<Command>.Start(fun inbox ->

            let rec messageLoop state = async {
                match! inbox.Receive() with
                | ConnectToSession id ->
                        let sessionGrain = siloClient.GetGrain<ISessionGrain> id
                        let! s = sessionGrain.AddParticipant state.User |> Async.AwaitTask

                        if not (s.ActiveStory = Unchecked.defaultof<string>)

                        then
                            let! st = connectToStory state (Guid.Parse s.ActiveStory) |> Async.AwaitTask
                            return! connectToSession st id inbox |> Async.AwaitTask
                        else
                           return! connectToSession state id inbox |> Async.AwaitTask
                | _ -> return state
            }
            messageLoop agent
           )


type Bot(
    token: string,
    siloClient: IClusterClient,
    loggerFactory: ILoggerFactory
    ) =

    let mutable bot = null;

    let logger = loggerFactory.CreateLogger<Bot>()

    let connectToSession (session: string) (userName: string) (chatId: Int64) (botClient: ITelegramBotClient) = task {
        match Guid.TryParse(session) with
        | false, _ -> return Error "Unexpected session Id"
        | true, guid ->
                let participant = {Id = %Guid.NewGuid()
                                   Name = userName
                                   Picture = None}


                return Ok s
        }

    let updateHandler (botClient: ITelegramBotClient) (update: Update) (ctx: CancellationToken): Task =
           upcast task {
                    try
                        match update.Message with
                        | :? Message as message -> match BotHelper.parseMessage message.Text with
                                                   | Command.connect, [| session |] ->
                                                                                 match! connectToSession session message.From.Username message.Chat.Id botClient with
                                                                                 | Ok s ->
                                                                                       let! m = botClient.SendTextMessageAsync(ChatId message.Chat.Id, $"connected to %s{s.Title}")
                                                                                       logger.LogInformation("{@user} connected to session {@session}", message.From, session)
                                                                                       return! Task.CompletedTask
                                                                                 | Error err ->
                                                                                     let! m = botClient.SendTextMessageAsync(ChatId message.Chat.Id, err)
                                                                                     logger.LogError("Error while {@user} connecting to session {@session}", message.From, session)
                                                                                     return! Task.CompletedTask


                                                   | _ ->
                                                       let! m = botClient.SendTextMessageAsync(ChatId message.Chat.Id, "Unexpected command")
                                                       return! Task.CompletedTask

                        | _ -> ()

                        match update.PollAnswer with
                        | :? PollAnswer as pollAnswer -> logger.LogInformation("User send answer to {@Poll}", pollAnswer)
                        | _ -> ()

                        return! Task.CompletedTask

                    with
                    | :? Exception as ex -> logger.LogError(ex.Message, ex)
                 }

    member _.Start(cancellationToken: CancellationToken): Task = upcast task {
        bot <- TelegramBotClient(token)
        return! bot.ReceiveAsync({
                               new IUpdateHandler with
                                       member _.HandleUpdate(botClient, update, ctx) = updateHandler botClient update ctx
                                       member _.HandleError(botClient, update, ctx) = Task.CompletedTask
                                       member _.AllowedUpdates = [| UpdateType.Message; UpdateType.PollAnswer |]
                               },
                            cancellationToken
                           )
        }




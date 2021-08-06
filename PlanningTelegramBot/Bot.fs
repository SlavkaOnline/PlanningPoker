module PlanningTelegramBot

open System
open System.Collections.Concurrent
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
open Telegram.Bot.Types.Enums
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

    let createResultMessage (title: string) (result: string) (statistics: Dictionary<string, VoteResultView>) =
        let header = $"<b>{title}</b>\n Result - {result} \n<b>Statistics</b>"
        let votes =
           statistics
           |> Seq.map(fun kv -> sprintf "%s %s" $" <b>%s{kv.Key}</b> - <i>{kv.Value.Percent}</i>" (kv.Value.Voters |> Array.map (fun u -> u.Name) |> String.concat ", ")  )
           |> String.concat "\n"
           
        $"%s{header} %s{votes}"   
    
    let (|SessionGuid|_|) (session: string) =
            let result, _ = Guid.TryParse session
            if result then Some SessionGuid else None

[<RequireQualifiedAccess>]
module internal Command =

    [<Literal>]
    let connect = "CONNECT"


[<RequireQualifiedAccess>]
module internal UserAgent =

    type Poll = {
        MessageId: int
        StoryId: Guid
        PollOptions: PollOption array
    }
    
    type Agent = {
        TelegramChatId: int64
        User: User
        PollStoryMap: Dictionary<string, Poll>
        StoryResultMessageMap: Dictionary<Guid, int>
        SessionSubscription: StreamSubscriptionHandle<EventView<Session.Event>> option
        StorySubscription: (StreamSubscriptionHandle<EventView<Story.Event>> * CancellationTokenSource) option
    }

    type Command =
        | ConnectToSession of Guid
        | ConnectToStory of Guid
        | SendPollAnswer of pollId: string * vote: int
        | ViewStory of Guid
        | Disconnect

    let create chatId user (botClient: ITelegramBotClient) (siloClient: IClusterClient) (logger: ILogger<_>) =
        let agent = {
            TelegramChatId = chatId
            User = user
            PollStoryMap = Dictionary<string, Poll>()
            StoryResultMessageMap = Dictionary<Guid, int>()
            SessionSubscription = None
            StorySubscription = None
        }

        let connectToStory state storyId = task {
                     let storyGrain = siloClient.GetGrain<IStoryGrain> storyId
                     let! story = storyGrain.GetState state.User

                     state.StorySubscription |> Option.iter(fun (sub, cts) ->
                            task {
                                do! sub.UnsubscribeAsync()
                                cts.Cancel()
                            } |> ignore )
                        
                     let! msg = botClient.SendPollAsync((ChatId chatId), $"Story: %s{story.Title}", story.Cards, false, PollType.Regular, allowSendingWithoutReply = true)
                     state.PollStoryMap.Add(msg.Poll.Id, {MessageId = msg.MessageId; StoryId = storyId; PollOptions = msg.Poll.Options})

                     let bufferChannel = Channel.CreateUnbounded<EventView<Story.Event>>()
                     let eventsChannel = Channel.CreateUnbounded<EventView<Story.Event>>()

                     let cts = new CancellationTokenSource()
                     let token = cts.Token


                     let! sub = siloClient
                                    .GetStreamProvider("SMS")
                                    .GetStream<EventView<Story.Event>>(storyId, "DomainEvents")
                                    .SubscribeAsync(fun event _ -> bufferChannel.Writer.WriteAsync(event).AsTask())

                     let! events = storyGrain.GetEventsAfter(story.Version)
                     let lastVersion =
                        if events.Count > 0 then
                            events.Item(events.Count - 1).Order
                        else
                            0
                     for e in events do
                        do! eventsChannel.Writer.WriteAsync(e).AsTask()


                     eventsChannel.Reader.ReadAllAsync(token)
                     |> AsyncSeq.ofAsyncEnum
                     |> AsyncSeq.append (bufferChannel.Reader.ReadAllAsync(token)
                                         |> AsyncSeq.ofAsyncEnum
                                         |> AsyncSeq.filter(fun e -> e.Order > lastVersion))
                     |> AsyncSeq.iterAsync(fun e -> async {
                                            match e.Payload with
                                            | Story.StoryClosed (result, _, _) ->
                                                   try 
                                                        do! botClient.DeleteMessageAsync((ChatId chatId), msg.MessageId) |> Async.AwaitTask //TODO: change msgId
                                                        let! msgResult = botClient.SendTextMessageAsync((ChatId chatId), BotHelper.createResultMessage story.Title %result story.Statistics, ParseMode.Html) |> Async.AwaitTask
                                                        state.StoryResultMessageMap.Add(storyId, msgResult.MessageId)
                                                        return ()
                                                    with
                                                    | :? Exception as ex -> logger.LogError("Error while handle story event {@event}", e.Payload, ex)
                                                    
                                            | Story.Cleared _ ->
                                                    try 
                                                        let resultMessageId = state.StoryResultMessageMap.[storyId]
                                                        do! botClient.DeleteMessageAsync((ChatId chatId), resultMessageId) |> Async.AwaitTask
                                                        let! msg = botClient.SendPollAsync((ChatId chatId), $"Story: %s{story.Title}", story.Cards, false, PollType.Regular, allowSendingWithoutReply = true) |> Async.AwaitTask
                                                        state.PollStoryMap.Add(msg.Poll.Id, {MessageId = msg.MessageId; StoryId = storyId; PollOptions = msg.Poll.Options})
                                                        return ()
                                                    with
                                                    | :? Exception as ex -> logger.LogError("Error while handle story event {@event}", e.Payload, ex)
                                            | _ -> return ()
                                       
                                      })
                     |> Async.Start
                                     
                     return { state with StorySubscription = Some(sub, cts )}
            }

        let eventHandler (event: Session.Event) (inbox: MailboxProcessor<Command>): Task = upcast task {
                              match event with
                              | Session.Event.ActiveStorySet e ->
                                     inbox.Post <| ConnectToStory %e
                                     return! Task.CompletedTask
                              | _ -> return!  Task.CompletedTask
            }

        let subscribeToSession state sessionId (inbox: MailboxProcessor<Command>) = task {                                                                                           
                let! sub = siloClient
                               .GetStreamProvider("SMS")
                               .GetStream<EventView<Session.Event>>(sessionId, "DomainEvents")
                               .SubscribeAsync(fun event _ -> eventHandler event.Payload inbox)

                return {state with SessionSubscription = Some sub}
        }

        MailboxProcessor<Command>.Start(fun inbox ->

            let rec messageLoop state = async {
                match! inbox.Receive() with
                | ConnectToSession id ->
                        
                        state.SessionSubscription
                        |> Option.iter (fun sub -> sub.UnsubscribeAsync() |> ignore)
                        let sessionGrain = siloClient.GetGrain<ISessionGrain> id
                        let! s = sessionGrain.AddParticipant state.User |> Async.AwaitTask
                        let! _ = botClient.SendTextMessageAsync((ChatId chatId), $"connected to %s{s.Title}") |> Async.AwaitTask
                        if not (s.ActiveStory = Unchecked.defaultof<string>)
                        then
                            let! st = connectToStory state (Guid.Parse s.ActiveStory) |> Async.AwaitTask
                            let! s = subscribeToSession st id inbox |> Async.AwaitTask
                            return! messageLoop s
                        else
                           let! s = subscribeToSession state id inbox |> Async.AwaitTask
                           return! messageLoop s
                           
                | ConnectToStory id -> let! s =  connectToStory state id |> Async.AwaitTask
                                       return! messageLoop s
                                       
                
                | SendPollAnswer (pollId, answer) ->
                                let poll = state.PollStoryMap.[pollId]
                                let vote = poll.PollOptions.[answer].Text
                                let storyGrain = siloClient.GetGrain<IStoryGrain> poll.StoryId
                                let! story = storyGrain.Vote(user, vote) |> Async.AwaitTask
                                return! messageLoop state
                | _ -> return! messageLoop state
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
    let userDict = ConcurrentDictionary<int64, MailboxProcessor<UserAgent.Command>>()

    let connectToSession (session: string) (userName: string) (chatId: Int64) (botClient: ITelegramBotClient) = task {
        match Guid.TryParse(session) with
        | false, _ ->
            logger.LogError("Error while {@user} connecting to session {@session}", userName, session)
            return! Task.CompletedTask
        | true, guid ->

                let mb = userDict.GetOrAdd(chatId, fun _ ->
                          let participant = {  Id = %Guid.NewGuid()
                                               Name = userName
                                               Picture = None}
                    
                          UserAgent.create chatId participant botClient siloClient logger
                    )
                
                mb.Post <| UserAgent.ConnectToSession guid
                return! Task.CompletedTask

        }

    let sendPollAnswer userId pollId vote =
        let mutable mb = Unchecked.defaultof<MailboxProcessor<UserAgent.Command>>;
        if userDict.TryGetValue(userId, &mb) then
            mb.Post <| UserAgent.SendPollAnswer(pollId, vote)
            ()
        else
            ()
    
    let updateHandler (botClient: ITelegramBotClient) (update: Update) (ctx: CancellationToken): Task =
           upcast task {
                    try
                        match update.Message with
                        | :? Message as message -> match BotHelper.parseMessage message.Text with
                                                   | Command.connect, [| session |] ->
                                                       do! connectToSession session message.From.Username message.Chat.Id botClient 

                                                   | _ ->
                                                       let! m = botClient.SendTextMessageAsync(ChatId message.Chat.Id, "Unexpected command")
                                                       return! Task.CompletedTask

                        | _ -> ()

                        match update.PollAnswer with
                        | :? PollAnswer as pollAnswer ->
                            sendPollAnswer pollAnswer.User.Id pollAnswer.PollId pollAnswer.OptionIds.[0]
                            logger.LogInformation("User send answer to {@Poll}", pollAnswer)
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




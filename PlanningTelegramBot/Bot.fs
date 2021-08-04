module PlanningTelegramBot

open System
open System.Threading
open System.Threading.Tasks
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
open Telegram.Bot.Types
open Telegram.Bot.Types
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
                let sessionGrain = siloClient.GetGrain<ISessionGrain> guid
                let! s = sessionGrain.AddParticipant participant
                if not (s.ActiveStory = Unchecked.defaultof<string>)
                then
                             let storyGrain = siloClient.GetGrain<IStoryGrain>(Guid.Parse s.ActiveStory)
                             let! story = storyGrain.GetState participant
                             let! _ = botClient.SendPollAsync((ChatId chatId), $"Current story %s{story.Title}", story.Cards, false, PollType.Regular, allowSendingWithoutReply = true)
                             return ()
                else
                    return ()

                let eventHandler (event: Session.Event): Task = upcast task {
                      match event with
                      | Session.Event.ActiveStorySet e ->
                             let storyGrain = siloClient.GetGrain<IStoryGrain>(UMX.untag e)
                             let! story = storyGrain.GetState participant
                             let! poll = botClient.SendPollAsync((ChatId chatId), $"Current story %s{story.Title}", story.Cards, false, PollType.Regular, allowSendingWithoutReply = true)
                             return! Task.CompletedTask
                      | _ -> return!  Task.CompletedTask
                }


                siloClient
                    .GetStreamProvider("SMS")
                    .GetStream<EventView<Session.Event>>(guid, "DomainEvents")
                    .SubscribeAsync(fun event token -> eventHandler event.Payload)
                    |> ignore

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




namespace Api

open System
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks
open FSharp.Control
open Microsoft.AspNetCore.SignalR

module rec Chat =
    type Message =
        { Group: string
          UserName: string
          Payload: string }

   
    type MessageQueueChat(hub: IHubContext<ChatHub>) =
        let queue = Channel.CreateUnbounded<Message>()
        let cancellation = new CancellationTokenSource()

        let backgroundReader =
            queue.Reader.ReadAllAsync(cancellation.Token)
            |> AsyncSeq.ofAsyncEnum
            |> AsyncSeq.iterAsync
                (fun m ->
                    hub
                        .Clients
                        .Group(m.Group)
                        .SendAsync("chatMessage", m.UserName, m.Payload)
                    |> Async.AwaitTask)

        do
            Async.StartAsTask(backgroundReader, cancellationToken = cancellation.Token)
            |> ignore
        
        member _.Send message = queue.Writer.WriteAsync message
            
        interface IDisposable with
            member this.Dispose() =
                cancellation.Cancel()
                
    type ChatHub(queue: MessageQueueChat)  =
        inherit Hub()

        member this.Join(group: string) : Task =
            this.Groups.AddToGroupAsync(this.Context.ConnectionId, group)

        member this.SendMessage(group: string, message: string) : Task =
            let user = this.Context.User.GetDomainUser()
            
            queue.Send(    
                    { Group = group
                      UserName = user.Name
                      Payload = message }
                )
                .AsTask()


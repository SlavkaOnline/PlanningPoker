namespace Api

open System.Threading.Tasks
open Microsoft.AspNetCore.SignalR
open PlanningPoker.Domain.CommonTypes

module Chat =
    
    type ChatHub() =
        inherit Hub()
             
        member this.Join(group: string): Task =
            this.Groups.AddToGroupAsync(this.Context.ConnectionId, group)
                
        member this.SendMessage(group: string, user: User, message: string): Task =
            this.Clients.Group(group).SendAsync("chatMessage", user, message)


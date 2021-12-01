namespace Api

open System.Threading.Tasks
open Microsoft.AspNetCore.SignalR

module Chat =
    
    type ChatHub() =
        inherit Hub()
             
        member this.Join(group: string): Task =
            this.Groups.AddToGroupAsync(this.Context.ConnectionId, group)
                
        member this.SendMessage(group: string, message: string): Task =
            let user = this.Context.User.GetDomainUser()
            this.Clients.Group(group).SendAsync("chatMessage", user.Name, message)


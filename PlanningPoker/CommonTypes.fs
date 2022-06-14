namespace PlanningPoker.Domain

module CommonTypes =

    open FSharp.UMX

    [<Measure>]
    type UserId

    [<Measure>]
    type SessionId
    
    type User = {
        Id: Guid<UserId>
        Name: string
        Picture: string option
    }
    
    
    [<RequireQualifiedAccess>]
    module Streams =
        
        open System
        
        type Stream = {
            Id: Guid
            Namespace: string
        }
        
        let SessionEvents = {
            Id = Guid.Empty
            Namespace = "events"
        }
        
        let StoreEvents = {
            Id = Guid.Empty
            Namespace = "events"
        }
        
        let SessionDomainEvents = {
            Id = Guid.Parse "F016242C-E203-4FBD-88EA-23BC9686DD4B"
            Namespace = "events"
        }

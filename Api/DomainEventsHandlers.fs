namespace Api

open System
open Api.DomainEventsHandler
open Databases
open Databases.Models
open Microsoft.Extensions.Logging
open PlanningPoker.Domain
open FSharp.UMX

module DomainEventsHandlers =
  
  type SessionStartSaveToAccountHandler(db: DataBaseContext,
                                        logger: ILogger<SessionStartSaveToAccountHandler>) =
  
    interface IDomainEventHandler<Session.DomainEvent.Started> with
      member this.Handle(event) =
        task {
          try 
            do db.AccountSessions.Add(AccountSessionEntity(Guid.NewGuid(), %event.UserId, %event.Id, event.Title, DateTime.UtcNow)) |> ignore
            let! _ = db.SaveChangesAsync()
            return ()
          with
          | :? Exception as e -> logger.LogError(e, "Ошибка при обработке события @{event}", event) 
        }
        


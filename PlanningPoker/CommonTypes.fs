namespace PlanningPoker.Domain

module CommonTypes =

    open System
    open FSharp.UMX

    [<Measure>] type UserId

    [<Measure>] type ObjectId 

    type User = { Id: Guid<UserId>; Name: string }
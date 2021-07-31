namespace PlanningPoker.Domain

module CommonTypes =

    open FSharp.UMX

    [<Measure>]
    type UserId

    [<Measure>]
    type ObjectId

    type User = {
        Id: Guid<UserId>
        Name: string
        Picture: string option
    }

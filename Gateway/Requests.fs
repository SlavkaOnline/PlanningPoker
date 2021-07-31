namespace Gateway

open FSharp.UMX
open System

module Requests =


    [<CLIMutable>]
    type CreateSession = { Title: string }

    [<CLIMutable>]
    type CreateStory = {
        Title: string
        CardsId: string
        CustomCards: string array
    }

    [<CLIMutable>]
    type Vote = { Card: string }

    [<CLIMutable>]
    type SetActiveStory = { Id: string }

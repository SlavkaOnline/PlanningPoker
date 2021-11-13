namespace Gateway

open System
open System.Collections.Generic

module Requests =


    [<CLIMutable>]
    type CreateSession = { Title: string }

    [<CLIMutable>]
    type CreateStory =
        { Title: string
          CardsId: string
          CustomCards: string array }

    [<CLIMutable>]
    type Vote = { Card: string }

    [<CLIMutable>]
    type CreateGroup = { Name: string }


    [<CLIMutable>]
    type MoveParticipantToGroup = { ParticipantId: Guid}

    [<CLIMutable>]
    type CloseStory = {
        Groups: Dictionary<Guid, Guid[]>
    }

    [<CLIMutable>]
    type AuthUserRequest = {
        Name: string
    }

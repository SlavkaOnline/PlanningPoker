namespace Gateway

open System

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

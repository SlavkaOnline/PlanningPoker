namespace Gateway

module Requests =


    [<CLIMutable>]
    type CreateSession = {
        Title: string
    }

    [<CLIMutable>]
    type CreateStory = {
        Title: string
    }

    [<CLIMutable>]
    type Vote = {
        Card: string;
    }
namespace Gateway

module Requests =

    [<CLIMutable>]
    type CreateStory = {
        Title: string
    }

    [<CLIMutable>]
    type Vote = {
        Card: string;
    }
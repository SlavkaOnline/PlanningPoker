namespace PlanningPoker.Domain

type Errors =
    | UnauthorizedAccess
    | StoryNotExist
    | ParticipantAlreadyExist
    | ParticipantNotExist
    | StoryIsClosed
    static member ConvertToExnMessage =
        function
        | UnauthorizedAccess -> "Only the creator can perform this action"
        | StoryNotExist -> "Story not found"
        | ParticipantAlreadyExist -> "Participant has added already"
        | ParticipantNotExist -> "Participant not found"
        | StoryIsClosed -> "It is not possible to perform an action with a closed story"

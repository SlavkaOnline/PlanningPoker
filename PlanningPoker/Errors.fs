namespace PlanningPoker.Domain

exception  PlanningPokerDomainException of string;

type Errors =
    | UnauthorizedAccess
    | StoryNotExist
    | ParticipantAlreadyExist
    | ParticipantNotExist
    | StoryIsClosed
    | StoryIsNotClosed
    | StoryHasntVotes
    static member ConvertToExnMessage =
        function
        | UnauthorizedAccess -> "Only the creator can perform this action"
        | StoryNotExist -> "Story not found"
        | ParticipantAlreadyExist -> "Participant has added already"
        | ParticipantNotExist -> "Participant not found"
        | StoryIsClosed -> "It is not possible to perform an action with a closed story"
        | StoryHasntVotes -> "Story hasn't votes"
        | StoryIsNotClosed -> "Cannot clear not closed story"

    static member RaiseDomainExn err = raise(PlanningPokerDomainException <| Errors.ConvertToExnMessage err)
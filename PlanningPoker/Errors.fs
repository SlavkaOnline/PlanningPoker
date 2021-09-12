namespace PlanningPoker.Domain

exception  PlanningPokerDomainException of string;

type Errors =
    | UnauthorizedAccess
    | StoryNotExist
    | ParticipantAlreadyExist
    | ParticipantNotExist
    | StoryIsClosed
    | StoryIsNotClosed
    | StoryHasNotVotes
    | VotesIsNotExist
    | UnexpectedCardValue
    | CardsHasDuplicatesValues
    | CardsHasNotValues
    | StoryNotStarted
    static member ConvertToExnMessage =
        function
        | UnauthorizedAccess -> "Only the creator can perform this action"
        | StoryNotExist -> "Story not found"
        | ParticipantAlreadyExist -> "Participant has added already"
        | ParticipantNotExist -> "Participant not found"
        | StoryIsClosed -> "It is not possible to perform an action with a closed story"
        | StoryHasNotVotes -> "Story hasn't votes"
        | StoryIsNotClosed -> "Cannot clear not closed story"
        | UnexpectedCardValue -> "Unexpected card value"
        | CardsHasDuplicatesValues -> "Cards has duplicates values"
        | CardsHasNotValues -> "Cards hasn't values"
        | VotesIsNotExist -> "Votes isn't exist"
        | StoryNotStarted -> "Cannot paused not started story"

    static member RaiseDomainExn err = raise(PlanningPokerDomainException <| Errors.ConvertToExnMessage err)
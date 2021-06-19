import { Participant } from './models';

export type SessionEventType = 'ActiveStorySet' | 'StoryAdded' | 'ParticipantAdded' | 'ParticipantRemoved' | 'Started';

export type Event<TEvent> = Readonly<{
    entityId: string;
    type: TEvent;
    order: number;
    payload: string;
}>;

export type ActiveStorySet = Readonly<{
    id: string;
}>;
export type StoryAdded = Readonly<{
    id: string;
}>;

export type ParticipantAdded = Readonly<Participant>;

export type ParticipantRemoved = Readonly<Participant>;

export type StoryEventType = 'Voted' | 'VoteRemoved' | 'StoryClosed' | 'StoryStarted' | 'Cleared';

export type Voted = Readonly<Participant>;
export type VoteRemoved = Readonly<Participant>;
export type Cleared = Readonly<{ startedAt: string }>;

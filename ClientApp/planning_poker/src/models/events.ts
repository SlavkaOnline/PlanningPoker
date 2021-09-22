import { Group, Participant } from './models';

export type SessionEventType =
    | 'ActiveStorySet'
    | 'StoryAdded'
    | 'ParticipantAdded'
    | 'ParticipantRemoved'
    | 'Started'
    | 'GroupAdded'
    | 'GroupRemoved'
    | 'ParticipantMovedToGroup';

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

export type ParticipantAdded = Participant;

export type ParticipantRemoved = Participant;

export type StoryEventType = 'Voted' | 'VoteRemoved' | 'StoryClosed' | 'StoryConfigured' | 'ActiveSet' | 'Cleared';

export type Voted = Readonly<Participant>;
export type VoteRemoved = Readonly<Participant>;
export type ActiveSet = Readonly<{ startedAt: string }>;
export type Cleared = Readonly<{ startedAt: string }>;
export type GroupAdded = Group;
export type GroupRemoved = Group;
export type ParticipantMovedToGroup = Readonly<{ group: Group; user: Omit<Participant, 'groupId'> }>;

import {Participant} from "./models";

export type SessionEventType =
    | 'ActiveStorySet'
    | 'StoryAdded'
    | 'ParticipantAdded'
    | 'ParticipantRemoved'
    | 'Started'


export type Event<TEvent> = Readonly<{
    entityId: string
    type: TEvent
    order: number
    payload: string
}>

export type ActiveStorySet = Readonly<{
    id: string
}>
export type StoryAdded = Readonly<{
    id: string
}>

export type ParticipantAdded = Readonly<{
    id: string
    name: string
}>

export type ParticipantRemoved = Readonly<{
    id: string
    name: string
}>

export type StoryEventType =
    | 'Voted'
    | 'VoteRemoved'
    | 'StoryClosed'
    | 'StoryStarted'

export type Voted = Readonly<Participant>
export type VoteRemoved = Readonly<Participant>

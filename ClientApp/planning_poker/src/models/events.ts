export type EventType =
    | 'ActiveStorySet'
    | 'StoryAdded'
    | 'ParticipantAdded'
    | 'ParticipantRemoved'
    | 'Started'


export type Event = Readonly<{
    type: EventType
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
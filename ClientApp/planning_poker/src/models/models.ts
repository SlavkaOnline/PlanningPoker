export type User = Readonly<{
    id: string
    name: string
    token: string
}>

export type Participant = Readonly<{
    id: string
    name: string
}>

export type Session = Readonly<{
    id: string
    title: string
    version: number
    ownerId: string
    ownerName: string
    activeStory: string | null
    participants: readonly Participant[]
    stories: readonly string[]
}>

export type Story = Readonly<{
    id: string
    title: string
    version: number
    ownerId: string
    ownerName: string
    isClosed: boolean
    voted: readonly Participant[]
    result: string | null
    startedAt: string
    finishedAt: string
    statistics: {[key: string]: VoteResult}
}>

export type VoteResult = Readonly<{
    percent: number
    voters: readonly Participant[]
}>
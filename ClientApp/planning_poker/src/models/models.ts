export type User = Readonly<{
    id: string
    name: string
    token: string
}>

export type Session = Readonly<{ 
    id: string
    title: string
    ownerId: string
    ownerName: string
    participants: readonly User[]
    stories: readonly string[]
}>
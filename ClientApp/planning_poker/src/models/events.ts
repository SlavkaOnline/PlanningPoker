
export type Event = Readonly<{
    type: 'ActiveStorySet'
    order: number
    payload: string
}>

export type ActiveStorySet = Readonly<{
    id: string;
}>
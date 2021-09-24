export type User = Readonly<{
    id: string;
    name: string;
    token: string;
    picture: string | null;
}>;

export type Participant = Readonly<{
    id: string;
    name: string;
    picture: string;
    groupId: string;
}>;

export type VotedParticipant = Readonly<{
    name: string;
    duration: string;
}>;

export type Group = Readonly<{
    id: string;
    name: string;
}>;

export type Session = Readonly<{
    id: string;
    title: string;
    version: number;
    ownerId: string;
    ownerName: string;
    activeStory: string | null;
    defaultGroupId: string;
    groups: readonly Group[];
    participants: readonly Participant[];
    stories: readonly string[];
}>;

export type StatisticsResult = Readonly<{
    [key: string]: VoteResult;
}>;

export type Statistics = Readonly<{
    id: string | null;
    result: StatisticsResult;
}>;

export type Story = Readonly<{
    id: string;
    title: string;
    version: number;
    ownerId: string;
    ownerName: string;
    userCard: string;
    cards: readonly string[];
    isClosed: boolean;
    voted: readonly string[];
    result: string | null;
    startedAt: string;
    duration: string;
    statistics: readonly Statistics[];
}>;

export type VoteResult = Readonly<{
    percent: number;
    voters: readonly VotedParticipant[];
}>;

export type Redirect = Readonly<{
    from: {
        pathname: string;
    };
}>;

export type Cards = Readonly<{
    id: string;
    caption: string;
}>;

const redirectKey = 'redirect';

export function saveRedirect(redirect: Redirect): void {
    localStorage.setItem(redirectKey, JSON.stringify(redirect || { from: { pathname: '/' } }));
}

export function receiveRedirect(): Redirect {
    const redirect = JSON.parse(
        localStorage.getItem(redirectKey) || JSON.stringify({ from: { pathname: '/' } }),
    ) as Redirect;

    return redirect;
}

export function removeRedirect(): void {
    localStorage.removeItem(redirectKey);
}

import React, { createContext, useContext, useEffect, useReducer } from 'react';
import { Participant, Story } from '../models/models';
import { ActiveSet, Cleared, Event, StoryEventType, Voted, VoteRemoved } from '../models/events';
import axios from 'axios';
import { getStory } from '../models/Api';
import { useSession } from './session-context';

const defaultStory: Story = {
    id: '',
    title: '',
    version: -1,
    ownerId: '',
    ownerName: '',
    userCard: '',
    cards: [],
    result: '',
    voted: [],
    isClosed: false,
    duration: '',
    startedAt: '',
    statistics: {},
};

type Init = Readonly<{
    tag: 'init';
    story: Story;
}>;

type ApplyEvent = Readonly<{
    tag: 'applyEvent';
    event: Event<StoryEventType>;
}>;

type Action = Init | ApplyEvent;

const reducer = (state: Story, action: Action): Story => {
    switch (action.tag) {
        case 'init':
            if (action.story.id !== state.id || action.story.version > state.version) {
                return action.story;
            } else {
                return state;
            }
        case 'applyEvent':
            if (state.version > action.event.order) {
                return state;
            } else {
                switch (action.event.type) {
                    case 'Voted': {
                        const voted = JSON.parse(action.event.payload) as Voted;
                        if (state.voted.findIndex((id) => id === voted.id) === -1) {
                            return {
                                ...state,
                                version: action.event.order,
                                voted: [voted.id, ...state.voted] as readonly string[],
                            };
                        } else {
                            return { ...state, version: action.event.order };
                        }
                    }

                    case 'VoteRemoved': {
                        const voteRemoved = JSON.parse(action.event.payload) as VoteRemoved;
                        return {
                            ...state,
                            version: action.event.order,
                            voted: [...state.voted.filter((id) => id !== voteRemoved.id)],
                        };
                    }

                    case 'StoryClosed':
                        return { ...state, isClosed: true };
                    case 'StoryConfigured':
                        return { ...state, version: action.event.order };
                    case 'Cleared': {
                        const cleared = JSON.parse(action.event.payload) as Cleared;
                        return {
                            ...state,
                            version: action.event.order,
                            isClosed: false,
                            statistics: {},
                            result: null,
                            startedAt: cleared.startedAt,
                            userCard: '',
                            voted: [],
                        };
                    }
                    case 'ActiveSet': {
                        const activeSet = JSON.parse(action.event.payload) as ActiveSet;
                        return {
                            ...state,
                            version: action.event.order,
                            startedAt: activeSet.startedAt,
                        };
                    }
                    default:
                        return state;
                }
            }
        default:
            return state;
    }
};

export const storyContext = createContext<{ story: Story; dispatch: React.Dispatch<Action> }>({
    story: defaultStory,
    dispatch: (_) => defaultStory,
});

export const ProvideStory = ({ children }: { children: any }) => {
    const [story, dispatch] = useReducer(reducer, defaultStory);

    const { session } = useSession();

    useEffect(() => {
        if (session.activeStory) {
            const cts = axios.CancelToken.source();
            getStory(session.activeStory, cts.token).then((s) => dispatch({ tag: 'init', story: s }));
            return () => cts.cancel();
        } else {
            dispatch({ tag: 'init', story: defaultStory });
        }
    }, [session.activeStory]);

    return <storyContext.Provider value={{ story, dispatch }}>{children}</storyContext.Provider>;
};

export function useStory() {
    return useContext(storyContext);
}

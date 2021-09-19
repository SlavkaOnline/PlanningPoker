import React, { createContext, useContext, useEffect, useReducer } from 'react';
import { Session } from '../models/models';
import {
    ActiveStorySet,
    Event,
    GroupAdded,
    ParticipantAdded,
    ParticipantRemoved,
    SessionEventType,
    StoryAdded,
} from '../models/events';
import { getSession } from '../models/Api';
import { useParams } from 'react-router-dom';
import axios from 'axios';

const defaultSession: Session = {
    id: '',
    title: '',
    version: 0,
    activeStory: null,
    ownerId: '',
    ownerName: '',
    participants: [],
    defaultGroupId: '',
    groups: [],
    stories: [],
};

type Init = Readonly<{
    tag: 'init';
    session: Session;
}>;

type ApplyEvent = Readonly<{
    tag: 'applyEvent';
    userId: string;
    event: Event<SessionEventType>;
}>;

type Action = Init | ApplyEvent;

const reducer = (state: Session, action: Action) => {
    switch (action.tag) {
        case 'init':
            if (action.session.version > state.version) {
                return action.session;
            } else {
                return state;
            }
        case 'applyEvent':
            if (state.version > action.event.order) {
                return state;
            } else {
                switch (action.event.type) {
                    case 'ActiveStorySet':
                        if (state.ownerId !== action.userId) {
                            const activeStorySet = JSON.parse(action.event.payload) as ActiveStorySet;
                            return { ...state, version: action.event.order, activeStory: activeStorySet.id };
                        } else {
                            return state;
                        }
                    case 'StoryAdded':
                        if (state.ownerId !== action.userId) {
                            const storyAdded = JSON.parse(action.event.payload) as StoryAdded;
                            return {
                                ...state,
                                version: action.event.order,
                                stories: [storyAdded.id, ...state.stories],
                            };
                        } else {
                            return state;
                        }
                    case 'ParticipantAdded': {
                        const participantAdded = JSON.parse(action.event.payload) as ParticipantAdded;
                        return {
                            ...state,
                            version: action.event.order,
                            participants: [participantAdded, ...state.participants],
                        };
                    }
                    case 'ParticipantRemoved': {
                        const participantRemoved = JSON.parse(action.event.payload) as ParticipantRemoved;
                        return {
                            ...state,
                            version: action.event.order,
                            participants: [...state.participants.filter((p) => p.id != participantRemoved.id)],
                        };
                    }
                    case 'Started':
                        return { ...state, version: action.event.order };

                    case 'GroupAdded': {
                        const groupAdded = JSON.parse(action.event.payload) as GroupAdded;
                        return { ...state, version: action.event.order, groups: [groupAdded, ...state.groups] };
                    }

                    default:
                        return { ...state, version: action.event.order };
                }
            }
        default:
            return state;
    }
};

export const sessionContext = createContext<{ session: Session; dispatch: React.Dispatch<Action> }>({
    session: defaultSession,
    dispatch: (_) => defaultSession,
});

export const ProvideSession = ({ children }: { children: any }) => {
    const [session, dispatch] = useReducer(reducer, defaultSession);

    const { id } = useParams<{ id: string }>();

    useEffect(() => {
        const cts = axios.CancelToken.source();
        getSession(id, cts.token).then((s) => dispatch({ tag: 'init', session: s }));
        return () => cts.cancel();
    }, [id]);

    return <sessionContext.Provider value={{ session, dispatch }}>{children}</sessionContext.Provider>;
};

export function useSession() {
    return useContext(sessionContext);
}

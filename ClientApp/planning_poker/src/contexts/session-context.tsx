import React, {createContext, useContext, useEffect, useReducer} from "react";
import {Participant, Session} from "../models/models";
import {
    ActiveStorySet,
    Event,
    ParticipantAdded,
    ParticipantRemoved,
    SessionEventType,
    StoryAdded
} from "../models/events";
import {getSession} from "../models/Api";
import {useParams} from "react-router-dom";
import axios from "axios";
import {ISubscription} from "@microsoft/signalr";
import {useHub} from "./hub-context";
import {useAuth} from "./auth-context";

const defaultSession: Session = {
    id: "",
    title: "",
    version: 0,
    activeStory: null,
    ownerId: "",
    ownerName: "",
    participants: [],
    stories: []

}

type Init = Readonly<{
    tag: "init"
    session: Session
}>

type ApplyEvent = Readonly<{
    tag: "applyEvent"
    userId: string
    event: Event<SessionEventType>;
}>


type Action = | Init | ApplyEvent

const reducer = (state: Session, action: Action) => {

    switch (action.tag) {
        case "init":
            if (action.session.version > state.version) {
                return action.session;
            } else {
                return state;
            }
        case "applyEvent" :
            if (state.version > action.event.order) {
                return state;
            } else {
                switch (action.event.type) {
                    case "ActiveStorySet":
                        if (state.ownerId !== action.userId) {
                            const activeStorySet = JSON.parse(action.event.payload) as ActiveStorySet
                            return {...state, activeStory: activeStorySet.id, version: action.event.order}
                        } else {
                            return state;
                        }
                    case "StoryAdded" :
                        if (state.ownerId !== action.userId) {
                            const storyAdded = JSON.parse(action.event.payload) as StoryAdded
                            return {...state, stories: [storyAdded.id, ...state.stories]}
                        } else {
                            return state;
                        }
                    case "ParticipantAdded" :
                        const participantAdded = JSON.parse(action.event.payload) as ParticipantAdded
                        return {
                            ...state,
                            participants: [({
                                id: participantAdded.id,
                                name: participantAdded.name
                            } as Readonly<Participant>), ...state.participants]
                        }
                    case "ParticipantRemoved" :
                        const participantRemoved = JSON.parse(action.event.payload) as ParticipantRemoved
                        return {...state, participants: [...state.participants.filter(p => p.id != participantRemoved.id)]}
                    case "Started":
                        return state;

                    default:
                        return state;
                }
            }
        default:
            return state;
    }
}

export const sessionContext = createContext<{session: Session, dispatch: React.Dispatch<Action>}>({session: defaultSession, dispatch: (_) => defaultSession});

export const ProvideSession = ({children}: { children: any }) => {
    const [session, dispatch] = useReducer(reducer, defaultSession);

    let {id} = useParams<{ id: string }>();

    useEffect(() => {
            let cts = axios.CancelToken.source();
            getSession(id, cts.token)
                .then(s => dispatch({tag: "init", session: s}));
            return () => cts.cancel();
        }, [id]
    );

    return (
        <sessionContext.Provider value={{session, dispatch}}>
            {children}
        </sessionContext.Provider>
    );
}

export function useSession() {
    return useContext(sessionContext);
}
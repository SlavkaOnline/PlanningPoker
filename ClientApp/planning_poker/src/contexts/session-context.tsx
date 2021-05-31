import React, {createContext, useContext, useEffect, useReducer, useState} from "react";
import {useAuth} from "./auth-context";
import {Participant, Session} from "../models/models";
import {ActiveStorySet, Event, ParticipantAdded, ParticipantRemoved, StoryAdded} from "../models/events";
import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import {getSession} from "../models/Api";
import {useParams} from "react-router-dom";

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
    event: Event;
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
            switch (action.event.type) {
                case "ActiveStorySet":
                    const activeStorySet = JSON.parse(action.event.payload) as ActiveStorySet
                    if (state.ownerId !== action.userId && state.version < action.event.order) {
                        return {...state, activeStory: activeStorySet.id, version: action.event.order}
                    } else {
                        return state;
                    }
                case "StoryAdded" :
                    if (state.ownerId !== action.userId && state.version < action.event.order) {
                        const storyAdded = JSON.parse(action.event.payload) as StoryAdded
                        return {...state, stories: [storyAdded.id, ...state.stories]}
                    } else {
                        return state;
                    }
                case "ParticipantAdded" :
                    const participantAdded = JSON.parse(action.event.payload) as ParticipantAdded
                    return {...state, participants: [({id: participantAdded.id, name: participantAdded.name} as Readonly<Participant>), ...state.participants]}
                case "ParticipantRemoved" :
                    const participantRemoved = JSON.parse(action.event.payload) as ParticipantRemoved
                    return {...state, participants: [...state.participants.filter(p => p.id != participantRemoved.id)]}
                case "Started":
                    return state;

                default:
                    return state;
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
            getSession(id)
                .then(s => dispatch({tag: "init", session: s}));
        }, [id]
    )

    return (
        <sessionContext.Provider value={{session, dispatch}}>
            {children}
        </sessionContext.Provider>
    );
}

export function useSession() {
    return useContext(sessionContext);
}
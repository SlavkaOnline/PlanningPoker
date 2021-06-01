import React, {createContext, useContext, useEffect, useReducer} from "react";
import {Participant, Story} from "../models/models";
import {
    Event,
 SessionEventType,
    StoryEventType, Voted, VoteRemoved
} from "../models/events";
import axios from "axios";
import {getStory} from "../models/Api";
import {useSession} from "./session-context";
import {useHub} from "./hub-context";
import {ISubscription} from "@microsoft/signalr";

const defaultStory: Story = {
    id: "",
    title: "",
    version: -1,
    ownerId: "",
    ownerName: "",
    result: "",
    voted: [],
    isClosed: false,
    finishedAt: "",
    startedAt: "",
    Statistics: {}
}

type Init = Readonly<{
    tag: "init"
    story: Story
}>

type ApplyEvent = Readonly<{
    tag: "applyEvent"
    event: Event<StoryEventType>;
}>


type Action = | Init | ApplyEvent

const reducer = (state: Story, action: Action) => {

    switch (action.tag) {
        case "init":
            if (action.story.id !== state.id || action.story.version > state.version) {
                return action.story;
            } else {
                return state;
            }
        case "applyEvent" :
            if (state.version > action.event.order) {
                return state;
            } else {
                switch (action.event.type) {
                    case "Voted":
                        const voted = JSON.parse(action.event.payload) as Voted
                        return {...state, voted: [voted, ...state.voted] as readonly Participant[]}

                    case "VoteRemoved" :
                        const voteRemoved = JSON.parse(action.event.payload) as VoteRemoved
                        return {...state, voted: [...state.voted.filter(v => v.id !== voteRemoved.id)]}

                    case "StoryClosed" :
                        return {...state, isClosed: true}
                    case "StoryStarted" :
                        return state;
                    default:
                        return state;
                }
            }
        default:
            return state;
    }
}

export const storyContext = createContext<{story: Story, dispatch: React.Dispatch<Action>}>({story: defaultStory, dispatch: (_) => defaultStory});

export const ProvideStory = ({children}: { children: any }) => {
    const hub = useHub();
    const [story, dispatch] = useReducer(reducer, defaultStory);

    const {session} = useSession();

    useEffect(() => {
            if (session.activeStory) {
                let cts = axios.CancelToken.source();
                getStory(session.activeStory, cts.token)
                    .then(s => dispatch({tag: "init", story: s}));
                return () => cts.cancel();
            } else {
                dispatch({tag: "init", story: defaultStory})
            }
        }, [session.activeStory]
    );

    return (
        <storyContext.Provider value={{story, dispatch}}>
            {children}
        </storyContext.Provider>
    );
}

export function useStory() {
    return useContext(storyContext);
}
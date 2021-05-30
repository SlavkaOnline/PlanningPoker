import React, {useEffect, useReducer, useState} from "react";
import {useParams} from "react-router-dom";
import {Session} from "../models/models";
import {createStory, getSession, setActiveStory} from "../models/Api";
import {Typography} from "@material-ui/core";
import {useAuth} from "../contexts/auth-context";
import styles from '../styles/session-page.module.scss'
import {SessionControl} from "../components/session-control";
import {StoriesTable} from "../components/stories-table";
import {HubConnection, HubConnectionBuilder, ISubscription} from "@microsoft/signalr";
import {ActiveStorySet, Event} from "../models/events"

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
                    const payload = JSON.parse(action.event.payload) as ActiveStorySet
                    console.log(payload);
                    if (state.ownerId !== action.userId && state.version < action.event.order) {
                        return {...state, activeStory: payload.id, version: action.event.order}
                    } else {
                        return state
                    }
                default:
                    return state;
            }
        default:
            return state;
    }
}

export const SessionPage = () => {

    let {user} = useAuth()
    let {id} = useParams<{ id: string }>();

    const [session, dispatch] = useReducer(reducer, defaultSession)
    const [connection, setConnection] = useState<HubConnection | null>(null);

    useEffect(() => {
            getSession(id)
                .then(s => dispatch({tag: "init", session: s}));
        }, [id]
    )

    useEffect(() => {
        if (user) {
            const conn = new HubConnectionBuilder()
                .withUrl('/events', {accessTokenFactory: () => user?.token || ""})
                .withAutomaticReconnect()
                .build();
            conn.start()
                .then(() => setConnection(conn));
        }
    }, [user]);

    useEffect(() => {
        let subscriptions: ISubscription<any>;
        if (connection) {
            subscriptions = connection
                .stream('Session', session.id, session.version)
                .subscribe({
                    next: (e: Event) =>
                        dispatch({
                        tag: "applyEvent",
                        event: e,
                        userId: user?.id || ""
                    }),
                    complete: () => console.log('complete'),
                    error: (e: any) => console.log(e)
                });
        }
        return () => subscriptions?.dispose()
    }, [connection])

    function create(title: string) {
        createStory(session.id, title)
            .then(s => dispatch({tag: "init", session: s}))
    }

    function selectStory(id: string) {
        setActiveStory(session.id, id)
            .then(s => dispatch({tag: "init", session: s}))
    }

    return (<>
        <div className={styles.wrapper}>
            <div className={styles.title}>
                <Typography variant="h4">
                    {session.title}
                </Typography>
            </div>
            <div className={styles.workplace}>
                <div className={styles.left}>
                    <div className={styles.playground}>playground</div>
                    <div className={styles.stories}>
                        <StoriesTable onSelect={selectStory} stories={session.stories}
                                      activeStoryId={session.activeStory}/>
                    </div>
                </div>
                <div className={styles.right}>
                    <div className={styles.control}>
                        <SessionControl currentStoryId={session.activeStory} onCreateStory={create}/>
                    </div>
                    <div className={styles.users}> users</div>
                </div>
            </div>
        </div>
    </>)
}
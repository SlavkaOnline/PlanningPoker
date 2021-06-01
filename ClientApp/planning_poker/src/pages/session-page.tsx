import React, {useEffect, useReducer, useState} from "react";
import {Typography} from "@material-ui/core";
import {useAuth} from "../contexts/auth-context";
import styles from '../styles/session-page.module.scss'
import {SessionControl} from "../components/session-control";
import {StoriesTable} from "../components/stories-table";
import {ISubscription} from "@microsoft/signalr";
import {ActiveStorySet, Event, SessionEventType} from "../models/events"
import {useHub} from "../contexts/hub-context";
import {useSession} from "../contexts/session-context";
import {OwnerWrapper} from "../components/OwnerWrapper";
import {UsersList} from "../components/users-list";
import {StoryPlayground} from "../components/story-playground";



export const SessionPage = () => {

    const {user} = useAuth();
    const hub = useHub();
    const {session, dispatch} = useSession()

    useEffect(() => {
        let subscriptions: ISubscription<any>;
        if (session.id && hub) {
            subscriptions = hub
                .stream('Session', session.id, session.version)
                .subscribe({
                    next: (e: Event<SessionEventType>) =>
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
    }, [session.id, hub])

    return (<>
        <div className={styles.wrapper}>
            <div className={styles.title}>
                <Typography variant="h4">
                    {session.title}
                </Typography>
            </div>
            <div className={styles.workplace}>
                <div className={styles.left}>
                    <div className={styles.playground}>
                        <StoryPlayground/>
                    </div>
                    <div className={styles.stories}>
                        <StoriesTable />
                    </div>
                </div>
                <div className={styles.right}>
                    <OwnerWrapper component={<SessionControl/>}/>
                    <div className={styles.users}>
                        <UsersList/>
                    </div>
                </div>
            </div>
        </div>
    </>)
}
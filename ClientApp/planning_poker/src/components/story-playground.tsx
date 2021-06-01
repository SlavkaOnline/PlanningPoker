import React, {useEffect} from "react";
import {Typography} from "@material-ui/core";
import styles from "../styles/story-playground.module.scss"
import {Cards} from "./Cards";
import {useStory} from "../contexts/story-context";
import {useSession} from "../contexts/session-context";
import {ISubscription} from "@microsoft/signalr";
import {StoryEventType, Event} from "../models/events";
import {useHub} from "../contexts/hub-context";

export const StoryPlayground = () => {

    const hub = useHub();
    const {session} = useSession()
    const {story, dispatch} = useStory();

    useEffect(() => {
        let subscriptions: ISubscription<any>;
        if (story.id && hub) {
            subscriptions = hub
                .stream('Story', story.id, story.version)
                .subscribe({
                    next: (e: Event<StoryEventType>) =>
                        dispatch({
                            tag: "applyEvent",
                            event: e,
                        }),
                    complete: () => console.log('complete'),
                    error: (e: any) => console.log(e)
                });
        }
        return () => subscriptions?.dispose()
    }, [story.id, hub])

    return (
        !story.id || session.stories.length == 0
            ? <div>Please select the story or create a new one</div>
            :
            <div className={styles.wrapper}>
                <div className={styles.title}>
                    <Typography variant="h5">
                        {story.title}
                    </Typography>
                </div>
                <div className={styles.playground}>
                    <Cards/>
                </div>
            </div>
    )
}
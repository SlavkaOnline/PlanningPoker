import React, {useEffect, useState} from "react";
import {useSession} from "../contexts/session-context";
import {Story} from "../models/models";
import {getStory} from "../models/Api";
import {Typography} from "@material-ui/core";
import styles from "../styles/story-playground.module.scss"
import {Cards} from "./Cards";

export const StoryPlayground = () => {
    const [story, setStory] = useState<Story | null>(null)
    const {session} = useSession();

    useEffect(() => {
        if (session.activeStory) {
            getStory(session.activeStory)
                .then(s => setStory(s));
        }
    }, [session.activeStory]);


    return (
        story === null
            ? <div>Please select the story</div>
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
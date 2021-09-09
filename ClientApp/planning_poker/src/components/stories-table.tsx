import React, { useEffect, useState } from 'react';
import { getStory, setActiveStory } from '../models/Api';
import { Story } from '../models/models';
import styles from '../styles/stories-table.module.scss';
import { Tooltip, Typography } from '@material-ui/core';
import { OwnerWrapper } from './owner-wrapper';
import { useSession } from '../contexts/session-context';
import { useStory } from '../contexts/story-context';

import StopIcon from '@material-ui/icons/Stop';
import PlayArrowIcon from '@material-ui/icons/PlayArrow';
import PauseIcon from '@material-ui/icons/Pause';
import DoneIcon from '@material-ui/icons/Done';
import { useAuth } from '../contexts/auth-context';

export const StoriesTable = () => {
    const { session, dispatch } = useSession();
    const [stories, setStories] = useState<readonly Story[]>([]);
    const { story } = useStory();
    const { user } = useAuth();

    useEffect(() => {
        if (session.stories.length) {
            const difference = session.stories.filter((s) => !stories.map((s) => s.id).includes(s));
            Promise.all(difference.map((id) => getStory(id))).then((s) => setStories([...s, ...stories]));
        }
    }, [session]);

    useEffect(() => {
        const index = stories.findIndex((s) => s.id === story.id);
        if (index > -1) {
            const arr = [...stories];
            arr[index] = story;
            setStories([...arr]);
        }
    }, [story]);

    function selectStory(id: string) {
        if (session.ownerId === user?.id) {
            setActiveStory(session.id, id).then((s) => dispatch({ tag: 'init', session: s }));
        }
    }

    function getIconAndStyle(story: Story) {
        return story.isClosed
            ? [styles.done, <DoneIcon key={null} className={styles.progress} />]
            : story.id === session.activeStory
            ? [styles.current, <PlayArrowIcon key={null} className={styles.progress} />]
            : story.voted.length === 0
            ? [styles.none, <StopIcon key={null} className={styles.progress} />]
            : [styles.pause, <PauseIcon key={null} className={styles.progress} />];
    }

    return (
        <div className={styles.wrapper}>
            <Typography variant="h6">Stories</Typography>
            <div className={styles.border} />
            <div className={styles.table}>
                {stories.map((story) => {
                    const [style, icon] = getIconAndStyle(story);
                    return (
                        <div key={story.id} className={styles.row + ' ' + style} onClick={() => selectStory(story.id)}>
                            {icon}
                            <div className={styles.name}> {story.title} </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
};

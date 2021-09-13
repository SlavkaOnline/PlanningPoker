import React from 'react';
import { useStory } from '../contexts/story-context';
import { clearStory, closeStory } from '../models/Api';
import styles from '../styles/story-control.module.scss';
import { Button } from '@material-ui/core';

export const StoryControl = () => {
    const { story, dispatch: dispatchStory } = useStory();

    function flipCards() {
        if (story.id) {
            closeStory(story.id).then((s) => dispatchStory({ tag: 'init', story: s }));
        }
    }

    function clear() {
        if (story.id) {
            clearStory(story.id).then((s) => dispatchStory({ tag: 'init', story: s }));
        }
    }

    return (
        <div className={styles.actions}>
            {!story.isClosed ? (
                story.voted.length ? (
                    <Button onClick={() => flipCards()} className={styles.action} variant="text" color="default">
                        Flip cards
                    </Button>
                ) : (
                    <></>
                )
            ) : (
                <Button onClick={() => clear()} className={styles.action} variant="text" color="default">
                    Clear story
                </Button>
            )}
        </div>
    );
};

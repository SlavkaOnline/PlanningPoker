import React, { useEffect, useState } from 'react';
import { Typography } from '@material-ui/core';
import styles from '../styles/story-playground.module.scss';
import { Cards } from './cards';
import { useStory } from '../contexts/story-context';
import { useSession } from '../contexts/session-context';
import { ISubscription } from '@microsoft/signalr';
import { ActiveSet, Cleared, Event, StoryEventType, Voted } from '../models/events';
import { useHub } from '../contexts/hub-context';
import { getStory } from '../models/Api';
import { StoryResult } from './story-result';
import { useSnackbar } from 'notistack';
import { OwnerWrapper } from './owner-wrapper';
import { StoryControl } from './story-control';
import { clipText } from '../models/utils';
import dayjs from 'dayjs';

export const StoryPlayground = () => {
    const hub = useHub();
    const { session } = useSession();
    const { story, dispatch } = useStory();
    const { enqueueSnackbar } = useSnackbar();
    const [duration, setDuration] = useState<string>(dayjs(dayjs().diff(dayjs(story.startedAt))).format('mm:ss'));

    useEffect(() => {
        let subscriptions: ISubscription<any>;
        if (story.id && hub) {
            subscriptions = hub.stream('Story', story.id, story.version).subscribe({
                next: (e: Event<StoryEventType>) => {
                    dispatch({
                        tag: 'applyEvent',
                        event: e,
                    });
                    handleEvent(e);
                },
                complete: () => console.log('complete'),
                error: (e: any) => console.log(e),
            });
        }
        return () => subscriptions?.dispose();
    }, [story.id, hub]);

    function handleEvent(e: Event<StoryEventType>): void {
        if (e.type === 'Voted') {
            const payload = JSON.parse(e.payload) as Voted;
            enqueueSnackbar(`${payload.name} voted`, { variant: 'success' });
        }
        if (e.type === 'StoryClosed') {
            enqueueSnackbar(`Story was closed`, { variant: 'info' });
        }
        if (e.type === 'Cleared') {
            enqueueSnackbar(`Story was cleared`, { variant: 'warning' });
        }
    }

    useEffect(() => {
        let timer: any = null;
        if (story.startedAt) {
            timer = setInterval(() => {
                setDuration(`${dayjs(dayjs().diff(dayjs(story.startedAt))).format('mm:ss')}`);
            }, 500);
        }

        return () => {
            if (timer) {
                clearInterval(timer);
            }
        };
    }, [story.id, story.startedAt]);

    useEffect(() => {
        if (story.isClosed && !story.result) {
            getStory(story.id).then((s) => dispatch({ tag: 'init', story: s }));
        }
    }, [story.isClosed]);

    return !story.id || session.stories.length == 0 ? (
        <div>Please select the story or create a new one</div>
    ) : (
        <div className={styles.wrapper}>
            <div className={styles.title}>
                <Typography variant="h5">{story.title}</Typography>
            </div>
            <div className={styles.work_panel}>
                {!story.isClosed ? <div className={styles.duration}>Duration: {duration}</div> : <></>}
                <div className={styles.action}>
                    <OwnerWrapper component={StoryControl()} />
                </div>
            </div>
            <div className={styles.playground}>
                {!story.isClosed ? <Cards cardsTypes={story.cards} /> : <StoryResult />}
            </div>
        </div>
    );
};

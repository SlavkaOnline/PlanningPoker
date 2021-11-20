import React, { useEffect, useState } from 'react';
import { Typography } from '@material-ui/core';
import styles from '../styles/story-playground.module.scss';
import { Cards } from './cards';
import { useStory } from '../contexts/story-context';
import { useSession } from '../contexts/session-context';
import { ISubscription } from '@microsoft/signalr';
import { Event, StoryEventType, Voted } from '../models/events';
import { useHub } from '../contexts/hub-context';
import { getStory } from '../models/Api';
import { StoryResult } from './story-result';
import { useSnackbar } from 'notistack';
import { OwnerWrapper } from './owner-wrapper';
import { StoryControl } from './story-control';
import TimerIcon from '@material-ui/icons/Timer';
import AccessAlarmIcon from '@material-ui/icons/AccessAlarm';
import dayjs from 'dayjs';
import { Durations } from './durations';

export const StoryPlayground = () => {
    const hub = useHub();
    const { session } = useSession();
    const { story, dispatch } = useStory();
    const { enqueueSnackbar } = useSnackbar();
    const [duration, setDuration] = useState<string>(dayjs(dayjs().diff(dayjs(story.startedAt))).format('mm:ss'));

    useEffect(() => {
        let subscriptions: ISubscription<any>;
        if (story.id && hub) {
            const createSubscriptions = () => {
                return hub.stream('Story', story.id, story.version).subscribe({
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
            };

            subscriptions = createSubscriptions();

            hub.onreconnected((connectionId) => {
                subscriptions.dispose();
                subscriptions = createSubscriptions();
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
            enqueueSnackbar(`Story is closed`, { variant: 'info' });
        }
        if (e.type === 'Cleared') {
            enqueueSnackbar(`Story is cleared`, { variant: 'warning' });
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

    function getTitle(groupId: string | null): string {
        if (session.groups.length === 1 && story.statistics.length === 1 && session.defaultGroupId === groupId) {
            return 'Results';
        }

        return groupId !== null
            ? (session.groups.find((g) => g.id === groupId) || null)?.name || `group_${groupId.substr(0, 4)}`
            : 'Results';
    }

    return !story.id || session.stories.length == 0 ? (
        <div>Please select the story or create a new one</div>
    ) : (
        <div className={styles.wrapper}>
            <div className={styles.title}>
                <Typography variant="h5">{story.title}</Typography>
            </div>
            <div className={styles.work_panel}>
                {!story.isClosed ? <Durations value={duration} /> : <></>}
                <div className={styles.action}>
                    <OwnerWrapper component={StoryControl()} />
                </div>
            </div>
            <div className={styles.playground}>
                {!story.isClosed ? (
                    <Cards cardsTypes={story.cards} />
                ) : (
                    story.statistics.map((s) => (
                        <StoryResult
                            key={s.id || '0'}
                            statistics={s}
                            duration={story.duration}
                            title={getTitle(s.id)}
                        />
                    ))
                )}
            </div>
        </div>
    );
};

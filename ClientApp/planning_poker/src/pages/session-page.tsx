import React, { useEffect } from 'react';
import { Typography } from '@material-ui/core';
import { useAuth } from '../contexts/auth-context';
import styles from '../styles/session-page.module.scss';
import { SessionControl } from '../components/session-control';
import { StoriesTable } from '../components/stories-table';
import { ISubscription } from '@microsoft/signalr';
import { Event, ParticipantAdded, ParticipantRemoved, SessionEventType } from '../models/events';
import { useHub } from '../contexts/hub-context';
import { useSession } from '../contexts/session-context';
import { OwnerWrapper } from '../components/owner-wrapper';
import { UsersList } from '../components/users-list';
import { StoryPlayground } from '../components/story-playground';
import { useSnackbar } from 'notistack';

export const SessionPage = () => {
    const { user } = useAuth();
    const hub = useHub();
    const { session, dispatch } = useSession();
    const { enqueueSnackbar } = useSnackbar();

    useEffect(() => {
        let subscriptions: ISubscription<any>;
        if (session.id && hub) {
            const createSubscriptions = () =>
                hub.stream('Session', session.id, session.version).subscribe({
                    next: (e: Event<SessionEventType>) => {
                        dispatch({
                            tag: 'applyEvent',
                            event: e,
                            userId: user?.id || '',
                        });
                        handleEvent(e);
                    },
                    complete: () => console.log('complete'),
                    error: (e: any) => console.log(e),
                });

            subscriptions = createSubscriptions();

            hub.onreconnected((connectionId) => {
                subscriptions.dispose();
                subscriptions = createSubscriptions();
            });
        }
        return () => subscriptions?.dispose();
    }, [session.id, hub]);

    function handleEvent(e: Event<SessionEventType>): void {
        if (e.type === 'ActiveStorySet') {
            enqueueSnackbar(`Story is switched`, { variant: 'info' });
        }
        if (e.type === 'StoryAdded') {
            enqueueSnackbar(`Story is added`, { variant: 'info' });
        }
        if (e.type === 'ParticipantAdded') {
            const participantAdded = JSON.parse(e.payload) as ParticipantAdded;
            enqueueSnackbar(`${participantAdded.name} joined`, { variant: 'info' });
        }
        if (e.type === 'ParticipantRemoved') {
            const participantAdded = JSON.parse(e.payload) as ParticipantRemoved;
            enqueueSnackbar(`${participantAdded.name} left`, { variant: 'error' });
        }
    }

    return (
        <>
            <div className={styles.wrapper}>
                <div className={styles.title}>
                    <Typography variant="h4">{session.title}</Typography>
                </div>
                <div className={styles.workplace}>
                    <div className={styles.left}>
                        <div className={styles.playground}>
                            <StoryPlayground />
                        </div>
                        <div className={styles.stories}>
                            <StoriesTable />
                        </div>
                    </div>
                    <div className={styles.right}>
                        <SessionControl />
                        <div className={styles.users}>
                            <UsersList />
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
};

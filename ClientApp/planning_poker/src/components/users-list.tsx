import React from 'react';
import { useSession } from '../contexts/session-context';
import { useStory } from '../contexts/story-context';
import { Typography } from '@material-ui/core';
import styles from '../styles/users-list.module.scss'

export const UsersList = () => {
    const { session } = useSession();
    const { story } = useStory();

    function checkVote(userId: string) {
        return story.voted.findIndex((v) => v.id == userId) > -1;
    }

    return (
        <div className={styles.wrapper} >
            <Typography variant="h6">Users</Typography>
            <div className={styles.list}>
                {session.participants.map((p) => (
                    <div key={p.id}>
                        {p.name} &nbsp; {checkVote(p.id) ? ' - Voted' : ''}
                    </div>
                ))}
            </div>
        </div>
    );
};

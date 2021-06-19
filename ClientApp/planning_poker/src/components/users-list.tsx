import React from 'react';
import { useSession } from '../contexts/session-context';
import { useStory } from '../contexts/story-context';
import { Avatar, Typography } from '@material-ui/core';
import styles from '../styles/users-list.module.scss';

export const UsersList = () => {
    const { session } = useSession();
    const { story } = useStory();

    function checkVote(userId: string) {
        return story.voted.findIndex((v) => v.id == userId) > -1;
    }

    return (
        <div className={styles.wrapper}>
            <Typography variant="h6">Users</Typography>
            <div className={styles.list}>
                {session.participants.map((p) => (
                    <div className={styles.item} key={p.id}>
                        <Avatar src={p.picture} />
                        <div className={styles.name + '  ' + (checkVote(p.id) ? styles.voted : styles.novoted)}>
                            {p.name}
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
};

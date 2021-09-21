import React from 'react';
import { Group, Participant } from '../models/models';
import styles from '../styles/users-group.module.scss';
import { useAuth } from '../contexts/auth-context';
import SettingsIcon from '@material-ui/icons/Settings';
import { OwnerWrapper } from './owner-wrapper';
import { Avatar, Tooltip } from '@material-ui/core';
import StarIcon from '@material-ui/icons/Star';
import { useStory } from '../contexts/story-context';
import { useSession } from '../contexts/session-context';
import { UserView } from './user-view';

export type UsersGroupProps = Readonly<{
    group: Group;
    participants: readonly Participant[];
    isVisibleHeader: boolean;
}>;

export const UsersGroup = (props: UsersGroupProps) => {
    const { session } = useSession();
    const { story } = useStory();

    function checkVote(userId: string) {
        return story.voted.includes(userId);
    }

    return (
        <div className={styles.wrapper}>
            {props.isVisibleHeader ? (
                <div className={styles.header}>
                    <div className={styles.name}>{props.group.name}</div>
                    <OwnerWrapper
                        component={
                            <Tooltip title={'Open group settings'}>
                                <SettingsIcon className={styles.settings} />
                            </Tooltip>
                        }
                    />
                </div>
            ) : (
                <></>
            )}
            <div className={styles.list}>
                {props.participants.map((p) => (
                    <UserView key={p.id} user={p} isOwner={p.id === session.ownerId} voted={checkVote(p.id)} />
                ))}
            </div>
        </div>
    );
};

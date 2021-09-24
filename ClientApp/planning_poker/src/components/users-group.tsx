import React, { useState } from 'react';
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
import { GeneralDialog } from './general-dialog';
import { CreateGroupWizard } from './create-group-wizard';
import { ManageUserGroup } from './manage-user-group';
import DeleteIcon from '@material-ui/icons/Delete';
import { removeGroup } from '../models/Api';

export type UsersGroupProps = Readonly<{
    group: Group;
    isVisibleHeader: boolean;
}>;

export const UsersGroup = (props: UsersGroupProps) => {
    const { session, dispatch } = useSession();
    const { story } = useStory();

    const [open, setOpen] = useState(false);

    function checkVote(userId: string) {
        return story.voted.includes(userId);
    }

    function remove() {
        removeGroup(session.id, props.group.id).then((s) => dispatch({ tag: 'init', session: s }));
    }

    return (
        <div className={styles.wrapper}>
            {props.isVisibleHeader ? (
                <div className={styles.header}>
                    <div className={styles.name}>{props.group.name}</div>
                    <OwnerWrapper
                        component={
                            <div className={styles.manage}>
                                {session.defaultGroupId !== props.group.id ? (
                                    <Tooltip title={'Remove the group'}>
                                        <DeleteIcon className={styles.action} onClick={() => remove()} />
                                    </Tooltip>
                                ) : (
                                    <></>
                                )}
                                <Tooltip title={'Open the group settings'}>
                                    <SettingsIcon className={styles.action} onClick={() => setOpen(true)} />
                                </Tooltip>
                            </div>
                        }
                    />
                </div>
            ) : (
                <></>
            )}
            <div className={styles.list}>
                {session.participants
                    .filter((p) => p.groupId === props.group.id)
                    .map((p) => (
                        <UserView key={p.id} user={p} isOwner={p.id === session.ownerId} voted={checkVote(p.id)} />
                    ))}
            </div>

            <GeneralDialog
                open={open}
                onClose={() => setOpen(false)}
                title={'Manage group'}
                content={<ManageUserGroup group={props.group} />}
            />
        </div>
    );
};

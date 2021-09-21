import React from 'react';
import { Group } from '../models/models';
import { useSession } from '../contexts/session-context';
import styles from '../styles/manage-user-group.module.scss';
import { UsersGroup } from './users-group';
import { UserView } from './user-view';
import { Typography } from '@material-ui/core';
import { moveParticipantToGroup } from '../models/Api';

export type ManageUserGroupProps = Readonly<{
    group: Group;
}>;

export const ManageUserGroup = (props: ManageUserGroupProps) => {
    const { session, dispatch } = useSession();
    const { group } = props;

    function move(id: string) {
        moveParticipantToGroup(session.id, group.id, id).then((s) => dispatch({ tag: 'init', session: s }));
    }

    return (
        <div className={styles.wrapper}>
            <div className={styles.blocks}>
                <div className={styles.block}>
                    <Typography variant="h6">Users</Typography>
                    <div className={styles.users}>
                        {session.participants
                            .filter((p) => p.groupId !== group.id)
                            .map((p) => (
                                <div className={styles.user} key={p.id} onClick={() => move(p.id)}>
                                    <UserView user={p} isOwner={session.ownerId === p.id} voted={false} />
                                </div>
                            ))}
                    </div>
                </div>
                <div className={styles.block}>
                    <Typography variant="h6">{group.name}</Typography>
                    <UsersGroup group={group} isVisibleHeader={false} />
                </div>
            </div>
        </div>
    );
};

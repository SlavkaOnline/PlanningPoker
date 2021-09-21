import React from 'react';
import { Group } from '../models/models';
import { useSession } from '../contexts/session-context';
import styles from '../styles/manage-user-group.module.scss';
import { UsersGroup } from './users-group';

export type ManageUserGroupProps = Readonly<{
    group: Group;
}>;

export const ManageUserGroup = (props: ManageUserGroupProps) => {
    const { session } = useSession();

    return (
        <div>
            <div>
                <UsersGroup
                    group={props.group}
                    participants={session.participants.filter((p) => p.groupId === props.group.id)}
                    isVisibleHeader={false}
                />
            </div>
        </div>
    );
};

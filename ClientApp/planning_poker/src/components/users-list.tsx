import React, { useEffect, useState } from 'react';
import { useSession } from '../contexts/session-context';
import { useStory } from '../contexts/story-context';
import { Dialog, Tooltip, Typography } from '@material-ui/core';
import GroupAddIcon from '@material-ui/icons/GroupAdd';
import styles from '../styles/users-list.module.scss';

import { UsersGroup } from './users-group';
import { CreateGroupWizard } from './create-group-wizard';
import { CreateGroupDialog } from './create-group-dialog';

export const UsersList = () => {
    const { session } = useSession();
    const { story } = useStory();

    const [open, setOpen] = useState(false);

    function closeCreateDialog() {
        setOpen(false);
    }

    return (
        <div className={styles.wrapper}>
            <div className={styles.title}>
                <Typography variant="h6">Users</Typography>
                <Tooltip title={'Create a new group'}>
                    <GroupAddIcon className={styles.groups} onClick={() => setOpen(true)} />
                </Tooltip>
                <div className={styles.panel}>
                    <div>votes/users</div>
                    <div className={styles.numbers}>
                        {story.voted.length}/{session.participants.length}
                    </div>
                </div>
            </div>
            <div className={styles.list}>
                {session.groups.map((g) => (
                    <UsersGroup
                        key={g.id}
                        group={g}
                        participants={session.participants.filter((p) => p.groupId === g.id)}
                        isVisibleHeader={true}
                    />
                ))}
            </div>
            <CreateGroupDialog open={open} onClose={closeCreateDialog} />
        </div>
    );
};

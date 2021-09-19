import React, { useEffect, useState } from 'react';
import { Button, FormControl, TextField } from '@material-ui/core';
import { Group } from '../models/models';
import { addGroup } from '../models/Api';
import { useSession } from '../contexts/session-context';

import styles from '../styles/create-group-form.module.scss';

export type CreateGroupFormProps = Readonly<{
    onCreate: (group: Group) => void;
}>;

export const CreateGroupForm = (props: CreateGroupFormProps) => {
    const { session } = useSession();
    const [name, setName] = useState('');

    function submit(e: any) {
        e.preventDefault();
        addGroup(session.id, name).then((s) => {
            const group = s.groups.find((g) => g.name === name);
            if (group) {
                props.onCreate(group);
            }
        });
    }

    return (
        <div className={styles.wrapper}>
            <form onSubmit={submit}>
                <FormControl className={styles.form_block}>
                    <TextField
                        fullWidth
                        label="Group name"
                        variant="outlined"
                        value={name}
                        inputMode="text"
                        required={true}
                        onChange={(e) => setName(e.target.value)}
                    />
                </FormControl>
                <FormControl className={styles.form_block + ' ' + styles.submit}>
                    <Button type={'submit'}>Create</Button>
                </FormControl>
            </form>
        </div>
    );
};

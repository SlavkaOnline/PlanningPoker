import React, { useState } from 'react';
import styles from '../styles/session-creater.module.scss';
import { Button, TextField } from '@material-ui/core';
import { createSession } from '../models/Api';
import { useHistory } from 'react-router-dom';

export const SessionCreator = () => {
    const [title, setTitle] = useState('');
    const history = useHistory();

    async function submit(e: any) {
        e.preventDefault();
        const session = await createSession(title);
        history.push(`/session/${session.id}`);
    }

    return (
        <form className={styles.form} onSubmit={submit} autoComplete="off">
            <div className={styles.title}>
                <TextField
                    autoFocus
                    required
                    className={styles.field}
                    id="outlined-basic"
                    label="Title"
                    variant="outlined"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                />
            </div>
            <div className={styles.create}>
                <Button variant="outlined" color="default" type={'submit'}>
                    Create
                </Button>
            </div>
        </form>
    );
};

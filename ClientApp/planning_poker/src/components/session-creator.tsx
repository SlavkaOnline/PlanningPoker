import React, { useState } from 'react';
import styles from '../styles/session-creater.module.scss';
import { Button, TextField } from '@material-ui/core';
import { createSession } from '../models/Api';
import { useHistory } from 'react-router-dom';
import { BusyWrapper } from './busy-wrapper';

export const SessionCreator = () => {
    const [title, setTitle] = useState('');
    const history = useHistory();

    async function create() {
        const session = await createSession(title);
        history.push(`/session/${session.id}`);
    }

    return (
        <form className={styles.form} autoComplete="off">
            <div className={styles.title}>
                <TextField
                    className={styles.field}
                    id="outlined-basic"
                    label="Title"
                    variant="outlined"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                />
            </div>
            <div className={styles.create}>
                <Button
                    variant="contained"
                    color="default"
                    disabled={title.trim().length === 0}
                    onClick={() => create()}
                >
                    Create
                </Button>
            </div>
        </form>
    );
};

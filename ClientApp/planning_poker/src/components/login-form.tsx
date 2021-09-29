import React, { useEffect, useRef, useState } from 'react';
import { Button, TextField } from '@material-ui/core';

import styles from '../styles/login-form.module.scss';
import { useAuth } from '../contexts/auth-context';
import { useLocation } from 'react-router-dom';
import GoogleButton from 'react-google-button';
import { receiveRedirect, removeRedirect, saveRedirect } from '../models/models';
import { createBrowserHistory } from 'history';

const history = createBrowserHistory({ forceRefresh: true });

export const LoginForm = () => {
    const [userName, setUserName] = useState('');
    const auth = useAuth();
    const location = useLocation<{ from: { pathname: string } }>();

    async function login(e: any) {
        e.preventDefault();
        await auth.signin(userName);
        const { from } = location.state || receiveRedirect();
        removeRedirect();
        history.push(from);
    }

    function loginGoogle() {
        const { from } = receiveRedirect();
        if (from.pathname === '/') {
            saveRedirect(location.state);
        }
        auth.signinGoogle();
    }

    return (
        <form className={styles.form} onSubmit={login} autoComplete="off">
            <div>
                <TextField
                    autoFocus
                    required
                    className={styles.username}
                    id="outlined-basic"
                    label="User name"
                    variant="outlined"
                    value={userName}
                    onChange={(e) => setUserName(e.target.value)}
                />
            </div>
            <div className={styles.login}>
                <Button type={'submit'} className={styles.button} variant="contained" color="primary">
                    Login
                </Button>
                <GoogleButton
                    className={styles.button}
                    onClick={() => {
                        loginGoogle();
                    }}
                />
            </div>
        </form>
    );
};

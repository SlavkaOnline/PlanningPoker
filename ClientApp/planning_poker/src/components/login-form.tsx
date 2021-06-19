import React, { useState } from 'react';
import { Button, TextField } from '@material-ui/core';

import styles from '../styles/login-form.module.scss';
import { useAuth } from '../contexts/auth-context';
import { useHistory, useLocation } from 'react-router-dom';
import GoogleButton from 'react-google-button';

export const LoginForm = () => {
    const [userName, setUserName] = useState('');
    const auth = useAuth();
    const history = useHistory();
    const location = useLocation<{ from: { pathname: string } }>();

    async function login() {
        await auth.signin(userName);
        const { from } = location.state || { from: { pathname: '/' } };
        history.replace(from);
    }

    function loginGoogle() {
        localStorage.setItem('redirect', JSON.stringify(location.state || { from: { pathname: '/' } }));
        auth.signinGoogle();
    }

    return (
        <form className={styles.form} noValidate autoComplete="off">
            <div>
                <TextField
                    className={styles.username}
                    id="outlined-basic"
                    label="User name"
                    variant="outlined"
                    value={userName}
                    onChange={(e) => setUserName(e.target.value)}
                />
            </div>
            <div className={styles.login}>
                <Button
                    type={'submit'}
                    className={styles.button}
                    variant="contained"
                    color="primary"
                    onClick={() => login()}
                >
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

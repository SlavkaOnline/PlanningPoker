import React, { useEffect } from 'react';
import { LoginForm } from '../components/login-form';
import styles from '../styles/login-page.module.scss';
import { useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/auth-context';
import { receiveRedirect, removeRedirect, User } from '../models/models';
import { createBrowserHistory } from 'history';

const history = createBrowserHistory({ forceRefresh: true });

function useQuery() {
    return new URLSearchParams(useLocation().search);
}

export const LoginPage = () => {
    const query = useQuery();
    const { updateUser } = useAuth();

    useEffect(() => {
        const accessToken = query.get('access_token');
        if (accessToken) {
            updateUser(accessToken);
            const { from } = receiveRedirect();
            removeRedirect();
            history.push(from);
        }
    }, []);

    return (
        <div className={styles.wrapper}>
            <div className={styles.form}>
                <LoginForm />
            </div>
        </div>
    );
};

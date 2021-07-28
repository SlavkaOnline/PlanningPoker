import React, { useEffect } from 'react';
import { LoginForm } from '../components/login-form';
import styles from '../styles/login-page.module.scss';
import { useHistory, useLocation } from 'react-router-dom';
import jwt_decode from 'jwt-decode';
import { useAuth } from '../contexts/auth-context';
import { receiveRedirect, removeRedirect, User } from '../models/models';
import { createBrowserHistory } from 'history';

const history = createBrowserHistory({ forceRefresh: true });

function useQuery() {
    return new URLSearchParams(useLocation().search);
}

export const LoginPage = () => {
    const query = useQuery();
    const { user, updateUser } = useAuth();

    useEffect(() => {
        const accessToken = query.get('access_token');

        if (accessToken && !user) {
            const decodedHeader = jwt_decode(accessToken) as any;
            updateUser({
                name: decodedHeader.given_name,
                id: decodedHeader.nameid,
                token: accessToken,
                picture: decodedHeader.picture,
            } as User);
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

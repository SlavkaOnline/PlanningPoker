import React, { useEffect } from 'react';
import { LoginForm } from '../components/login-form';
import styles from '../styles/login-page.module.scss';
import { useHistory, useLocation } from 'react-router-dom';
import jwt_decode from 'jwt-decode';
import { useAuth } from '../contexts/auth-context';
import { User } from '../models/models';

function useQuery() {
    return new URLSearchParams(useLocation().search);
}

export const LoginPage = () => {
    const query = useQuery();
    const { user, updateUser } = useAuth();
    const history = useHistory();

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
            const { from } = JSON.parse(
                localStorage.getItem('redirect') || JSON.stringify({ from: { pathname: '/' } }),
            ) as { from: { pathname: string } };
            localStorage.removeItem('redirect');
            history.replace(from);
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

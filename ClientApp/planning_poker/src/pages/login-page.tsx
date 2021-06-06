import React from 'react';
import { LoginForm } from '../components/login-form';

import styles from '../styles/login-page.module.scss';

export const LoginPage = () => {
    return (
        <div className={styles.wrapper}>
            <div className={styles.form}>
                <LoginForm />
            </div>
        </div>
    );
};

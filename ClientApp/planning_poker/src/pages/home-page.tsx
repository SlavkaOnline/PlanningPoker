import React from 'react';
import { useAuth } from '../contexts/auth-context';
import { SessionCreator } from '../components/session-creator';
import styles from '../styles/home.module.scss';

export const HomePage = () => {
    const { user } = useAuth();

    function renderSessionCreator() {
        if (user != null) {
            return <SessionCreator />;
        } else {
            return <div>Please log in to create a new session</div>;
        }
    }

    return <div className={styles.wrapper}>{renderSessionCreator()}</div>;
};

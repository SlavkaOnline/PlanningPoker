import React from 'react';
import styles from '../styles/session-control.module.scss';
import { Chat } from './chat';

export const SessionControl = () => {
    return (
        <div className={styles.wrapper}>
            <Chat />
        </div>
    );
};

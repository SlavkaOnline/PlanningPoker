import React from 'react';
import styles from '../styles/durations.module.scss';
import AccessAlarmIcon from '@material-ui/icons/AccessAlarm';

export const Durations = (props: { value: string }) => {
    return (
        <div className={styles.duration}>
            <AccessAlarmIcon />
            <span>{props.value}</span>
        </div>
    );
};

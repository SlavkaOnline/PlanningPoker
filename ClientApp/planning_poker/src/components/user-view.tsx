import React from 'react';
import { Participant } from '../models/models';
import styles from '../styles/user-view.module.scss';
import { Avatar } from '@material-ui/core';
import StarIcon from '@material-ui/icons/Star';

export type UserViewProps = Readonly<{
    user: Participant;
    isOwner: boolean;
    voted: boolean;
}>;

export const UserView = (props: UserViewProps) => {
    return (
        <div className={styles.wrapper}>
            <Avatar src={props.user.picture} />
            <div className={styles.name + '  ' + (props.voted ? styles.voted : styles.novoted)}>
                {props.user.name}
                {props.isOwner ? <StarIcon className={styles.star} /> : <></>}
            </div>
        </div>
    );
};

import React, { ReactComponentElement } from 'react';
import { Typography } from '@material-ui/core';
import styles from '../styles/game-info.module.scss';

export type GameInfoProps = Readonly<{
    title: string;
    img: string;
    info: ReactComponentElement<any>;
    action: ReactComponentElement<any> | null;
}>;

export const GameInfo = (props: GameInfoProps) => {
    return (
        <div className={styles.wrapper}>
            <Typography variant="h1">{props.title}</Typography>
            <img className={styles.logo} src={props.img} alt="" />
            <div>{props.info}</div>
            <div className={styles.action}>
                {props.action ? (
                    <div className={styles.creator}>
                        <span>Try with the new session </span> {props.action}
                    </div>
                ) : (
                    <div>Coming soon...</div>
                )}
            </div>
        </div>
    );
};

import React from 'react';
import { Dialog, DialogTitle } from '@material-ui/core';
import CloseIcon from '@material-ui/icons/Close';
import styles from '../styles/general-dialog.module.scss';

export type CreateGroupDialogProps = Readonly<{
    content: JSX.Element;
    title: string;
    open: boolean;
    onClose: () => void;
}>;

export const GeneralDialog = (props: CreateGroupDialogProps) => {
    return (
        <Dialog {...props} className={styles.wrapper} aria-labelledby="simple-dialog-title">
            <div className={styles.title}>
                <DialogTitle id="simple-dialog-title">{props.title}</DialogTitle>
                <CloseIcon className={styles.close} onClick={props.onClose} />
            </div>
            {props.content}
        </Dialog>
    );
};
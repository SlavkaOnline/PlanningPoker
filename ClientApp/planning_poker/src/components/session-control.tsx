import React, { useEffect, useState } from 'react';
import { TextField, Tooltip } from '@material-ui/core';
import styles from '../styles/session-control.module.scss';
import FileCopySharpIcon from '@material-ui/icons/FileCopySharp';

export const SessionControl = () => {
    return (
        <div className={styles.wrapper}>
            <div className={styles.link}>
                <TextField
                    className={styles.field}
                    disabled
                    label="Share link"
                    variant="outlined"
                    defaultValue={window.location.href}
                />
                <Tooltip title={'Copy to clipboard'}>
                    <FileCopySharpIcon
                        className={styles.copy}
                        color="action"
                        onClick={() => {
                            navigator.clipboard.writeText(window.location.href);
                        }}
                    />
                </Tooltip>
            </div>
        </div>
    );
};

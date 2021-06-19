import React, { useState } from 'react';
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField, Tooltip } from '@material-ui/core';
import styles from '../styles/session-control.module.scss';
import { clearStory, closeStory, createStory } from '../models/Api';
import { useSession } from '../contexts/session-context';
import { useStory } from '../contexts/story-context';
import FileCopySharpIcon from '@material-ui/icons/FileCopySharp';

export const SessionControl = () => {
    const [open, setOpen] = useState(false);
    const [storyName, setStoryName] = useState('');
    const { session, dispatch } = useSession();
    const { story, dispatch: dispatchStory } = useStory();

    function create() {
        if (storyName) {
            setOpen(false);
            createStory(session.id, storyName).then((s) => dispatch({ tag: 'init', session: s }));
        }
    }

    function flipCards() {
        if (story.id) {
            closeStory(story.id).then((s) => dispatchStory({ tag: 'init', story: s }));
        }
    }

    function clear() {
        if (story.id) {
            clearStory(story.id).then((s) => dispatchStory({ tag: 'init', story: s }));
        }
    }

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
                        color="primary"
                        onClick={() => {
                            navigator.clipboard.writeText(window.location.href);
                        }}
                    />
                </Tooltip>
            </div>
            <div className={styles.actions}>
                <Button className={styles.action} variant="contained" color="primary" onClick={() => setOpen(true)}>
                    Create story
                </Button>
                {!story.isClosed ? (
                    story.voted.length ? (
                        <Button
                            onClick={() => flipCards()}
                            className={styles.action}
                            variant="contained"
                            color="primary"
                        >
                            Flip cards
                        </Button>
                    ) : (
                        <></>
                    )
                ) : (
                    <Button onClick={() => clear()} className={styles.action} variant="contained" color="primary">
                        Clear story
                    </Button>
                )}
            </div>

            <Dialog open={open} aria-labelledby="form-dialog-title">
                <DialogTitle id="form-dialog-title"> Create New Story</DialogTitle>
                <DialogContent className={styles.dialog}>
                    <TextField
                        autoFocus
                        margin="dense"
                        id="name"
                        label="Story title"
                        type="text"
                        variant="outlined"
                        fullWidth
                        onChange={(e) => setStoryName(e.target.value)}
                    />
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setOpen(false)} color="primary">
                        Cancel
                    </Button>
                    <Button onClick={create} color="primary">
                        Create
                    </Button>
                </DialogActions>
            </Dialog>
        </div>
    );
};

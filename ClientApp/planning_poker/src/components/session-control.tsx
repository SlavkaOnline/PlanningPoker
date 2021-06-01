import React, {useState} from "react";
import {Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField} from "@material-ui/core";
import styles from "../styles/session-control.module.scss"
import {Story} from "../models/models";
import {closeStory, createStory} from "../models/Api";
import {useSession} from "../contexts/session-context";
import {useStory} from "../contexts/story-context";


export const SessionControl = () => {

    const [open, setOpen] = useState(false);
    const [storyName, setStoryName] = useState("");
    const {session, dispatch} = useSession()
    const {story, dispatch: dispatchStory} = useStory();

    function create() {
        if (storyName) {
            setOpen(false);
            createStory(session.id, storyName)
                .then(s => dispatch({tag: "init", session: s}));
        }
    }

    function flipCards() {
        if (story.id) {
            closeStory(story.id)
                .then(s => dispatchStory({tag: "init", story: s}));
        }
    }

    return (
        <div className={styles.wrapper}>
            <TextField
                className={styles.link}
                disabled
                label="Share link"
                variant="outlined"
                defaultValue={window.location.href}
            />
            <div className={styles.actions}>
                <Button
                    className={styles.action}
                    variant="contained" color="primary"
                    onClick={() => setOpen(true)}
                >
                    Create story
                </Button>
                {!story.isClosed
                    ? <Button
                        onClick={() => flipCards()}
                        className={styles.action}
                        variant="contained" color="primary">
                        Flip cards
                    </Button>
                    : <></>
                }
            </div>

            <Dialog
                open={open} aria-labelledby="form-dialog-title">
                <DialogTitle id="form-dialog-title"> Create New Story</DialogTitle>
                <DialogContent>
                    <TextField
                        autoFocus
                        margin="dense"
                        id="name"
                        label="Story title"
                        type="text"
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
    )
}

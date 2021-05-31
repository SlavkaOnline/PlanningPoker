import React, {useState} from "react";
import {Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField} from "@material-ui/core";
import styles from "../styles/session-control.module.scss"
import {Story} from "../models/models";
import {createStory} from "../models/Api";
import {useSession} from "../contexts/session-context";


export const SessionControl = () => {

    const [open, setOpen] = useState(false);
    const [story, setStory] = useState("");
    const {session, dispatch} = useSession()

    function create() {
        if(story) {
            setOpen(false);
            createStory(session.id, story)
                .then(s => dispatch({tag: "init", session: s}));
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
                <Button
                    className={styles.action}
                    variant="contained" color="primary">
                    Flip cards
                </Button>
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
                        onChange={(e) => setStory(e.target.value)}
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

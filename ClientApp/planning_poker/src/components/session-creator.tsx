import React, {useState} from "react";
import styles from "../styles/login-form.module.scss";
import {Button, TextField} from "@material-ui/core";
import {createSession} from "../models/Api";
import {useHistory} from "react-router-dom";
import {BusyWrapper} from "./busy-wrapper";

export const SessionCreator = () => {

    const [title, setTitle] = useState("");
    const [isBusy, setBusy] = useState(false)
    const history = useHistory();

    async function create() {
        setBusy(true);
        const session = await createSession(title);
        setBusy(false);
        history.push(`/session/${session.id}`)
    }

    return (

        <form
            className={styles.form}
            noValidate autoComplete="off">
            <div>
                <TextField
                    className={styles.username}
                    id="outlined-basic"
                    label="Title"
                    variant="outlined"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                />
            </div>
            <div className={styles.login}>
                <BusyWrapper Component={
                    <Button variant="contained" color="primary" onClick={() => create()}>
                        Create
                    </Button>
                }/>
            </div>
        </form>)
}
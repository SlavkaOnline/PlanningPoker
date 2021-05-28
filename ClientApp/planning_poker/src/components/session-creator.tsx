import React, {useState} from "react";
import styles from "../styles/login-form.module.scss";
import {Button, TextField} from "@material-ui/core";
import {createSession} from "../models/Api";

export const SessionCreator = () => {

    const [title, setTitle ] = useState("");
    
    async function create() {
        const session = await createSession(title);
        console.log(JSON.stringify(session));
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
            <Button variant="contained" color="primary" onClick={() => create()}>
                Create
            </Button>
        </div>
    </form>)
}
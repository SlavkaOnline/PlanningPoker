import React from "react";
import {Button, TextField} from "@material-ui/core";

export const Login = () => {

    return (
        <div>
            <form noValidate autoComplete="off">
                <div>
                    <TextField id="outlined-basic" label="User name" variant="outlined"/>
                </div>
                <div>
                    <Button variant="contained" color="primary">
                        Login
                    </Button>
                </div>
            </form>
        </div>)
}
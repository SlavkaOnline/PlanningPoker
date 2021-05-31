import React from "react";
import {useSession} from "../contexts/session-context";

export const UsersList = () => {

    const {session} = useSession();

    return (
        <div>
            {
                session.participants.map(p => <div key={p.id} >{p.name}</div>)
            }
        </div>
    )
}
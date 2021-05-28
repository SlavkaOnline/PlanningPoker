import React from "react";
import {useAuth} from "../contexts/auth-context";
import {SessionCreator} from "../components/session-creator";

export const HomePage = () => {
    const {user} = useAuth();
    
    function renderSessionCreator()
    {
        if (user != null) {
           return (<SessionCreator/>)
        } else {
            return (<></>)
        }
    }
    
    return (
        <>
        <div>
            Home
        </div>
            {renderSessionCreator()}
        </>)
}
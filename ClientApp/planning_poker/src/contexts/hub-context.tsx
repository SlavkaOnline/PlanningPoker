import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import React, {createContext, useContext, useEffect, useReducer, useState} from "react";
import {useAuth} from "./auth-context";
import {useParams} from "react-router-dom";

export const hubContext = createContext<HubConnection | null>(null);

export const ProvideHub = ({children}: { children: any }) => {
    const { user } = useAuth();
    const [hub, setHub] = useState<HubConnection | null>(null)

    useEffect(() => {
        if (user) {
            const hub = new HubConnectionBuilder()
                .withUrl('/events', {accessTokenFactory: () => user?.token || ""})
                .withAutomaticReconnect()
                .build();
            hub.start()
                .then(() => setHub(hub));
        }
    }, [user]);

    return (
        <hubContext.Provider value={hub}>
            {children}
        </hubContext.Provider>
    );
}

export function useHub() {
    return useContext(hubContext);
}
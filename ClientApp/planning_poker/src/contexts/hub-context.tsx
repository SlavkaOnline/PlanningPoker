import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import React, { createContext, useContext, useEffect, useState } from 'react';
import { useAuth } from './auth-context';
import { useSnackbar } from 'notistack';

export const hubContext = createContext<HubConnection | null>(null);

export const ProvideHub = ({ children }: { children: any }) => {
    const { user } = useAuth();
    const [hub, setHub] = useState<HubConnection | null>(null);
    const { enqueueSnackbar } = useSnackbar();

    useEffect(() => {
        if (user) {
            const hub = new HubConnectionBuilder()
                .withUrl('/events', { accessTokenFactory: () => user?.token || '' })
                .withAutomaticReconnect()
                .configureLogging('error')
                .build();
            hub.start().then(() => setHub(hub));

            hub.onreconnecting((err) => {
                enqueueSnackbar('Connection to server lost', { variant: 'error' });
            });

            hub.onreconnected((connectionId) => {
                enqueueSnackbar('Connection restored', { variant: 'success' });
            });
        }
    }, [user]);

    return <hubContext.Provider value={hub}>{children}</hubContext.Provider>;
};

export function useHub() {
    return useContext(hubContext);
}

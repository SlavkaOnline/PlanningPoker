import React, {createContext, useContext, useEffect, useState} from "react";
import {login} from "../models/Api";
import {User} from "../models/models";

type AuthProps = Readonly<{
    user: User | null
    signin: (name: string) => void
    signout: () => void
}>

const AuthPropsDefault = {
    user: null,
    signin: (name: string) => {
    },
    signout: () => {
    }

}

export const authContext = createContext<AuthProps>(AuthPropsDefault);

export const ProvideAuth = ({children}: { children: any }) => {
    const auth = useProvideAuth();
    return (
        <authContext.Provider value={auth}>
            {children}
        </authContext.Provider>
    );
}

export function useAuth() {
    return useContext(authContext);
}

function useProvideAuth() {
    const [user, setUser] = useState<User | null>(null);

    useEffect(() => {
        const localUser = localStorage.getItem('user');
        if (localUser) {
            setUser(JSON.parse(localUser));
        }
    }, [])

    const signin = async (name: string) => {
        const user = await login(name);
        localStorage.setItem('user', JSON.stringify(user));
        setUser(user)
    };

    const signout = () => {
        localStorage.removeItem('user');
        setUser(null);
    };

    return {
        user,
        signin,
        signout
    };
}
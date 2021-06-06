import React, { createContext, useContext, useEffect, useState } from 'react';
import { login } from '../models/Api';
import { User } from '../models/models';

type AuthState = Readonly<{
    user: User | null;
    signin: (name: string) => void;
    signout: () => void;
}>;

const authPropsDefault: AuthState = {
    user: null,
    signin: (name: string) => {
        return;
    },
    signout: () => {
        return;
    },
};

export const authContext = createContext<AuthState>(authPropsDefault);

export const ProvideAuth = ({ children }: { children: any }) => {
    const auth = useProvideAuth();
    return <authContext.Provider value={auth}>{children}</authContext.Provider>;
};

export function useAuth() {
    return useContext(authContext);
}

function useProvideAuth() {
    const [user, setUser] = useState<User | null>(
        localStorage.getItem('user') ? JSON.parse(localStorage.getItem('user') || '') : null,
    );

    const signin = async (name: string) => {
        const user = await login(name);
        localStorage.setItem('user', JSON.stringify(user));
        setUser(user);
    };

    const signout = () => {
        localStorage.removeItem('user');
        setUser(null);
    };

    return {
        user,
        signin,
        signout,
    };
}

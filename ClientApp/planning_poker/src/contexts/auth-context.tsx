import React, { createContext, useContext, useState } from 'react';
import { login, loginGoogle } from '../models/Api';
import { User } from '../models/models';

type AuthState = Readonly<{
    user: User | null;
    signin: (name: string) => void;
    signinGoogle: () => void;
    signout: () => void;
    updateUser: (user: User) => void;
}>;

const authPropsDefault: AuthState = {
    user: null,
    signin: (name: string) => {
        return;
    },
    signinGoogle: () => {
        return;
    },
    signout: () => {
        return;
    },
    updateUser: (user: User) => {
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

    const signinGoogle = async () => {
        await loginGoogle();
    };

    const signout = () => {
        localStorage.removeItem('user');
        setUser(null);
    };

    const updateUser = (user: User) => {
        localStorage.setItem('user', JSON.stringify(user));
        setUser(user);
    };

    return {
        user,
        signin,
        signinGoogle,
        signout,
        updateUser,
    };
}

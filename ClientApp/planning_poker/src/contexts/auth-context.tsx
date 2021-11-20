import React, { createContext, useContext, useState } from 'react';
import { login, loginGoogle } from '../models/Api';
import { AuthUser, User } from '../models/models';
import jwt_decode from 'jwt-decode';

type AuthState = Readonly<{
    user: User | null;
    signin: (name: string) => void;
    signinGoogle: () => void;
    signout: () => void;
    updateUser: (accessToken: string) => void;
    getAccessToken: () => string | null;
}>;

const authPropsDefault: AuthState = {
    getAccessToken(): string | null {
        return null;
    },
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
    updateUser: (accessToken: string) => {
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
    const decodeUser = (accessToken: string): User => {
        const decodedHeader = jwt_decode(accessToken) as any;
        return {
            name: decodedHeader.given_name,
            id: decodedHeader.nameid,
            picture: decodedHeader.picture,
        };
    };

    const getAccessToken = (): string | null => {
        const value = localStorage.getItem('user');
        if (value) {
            return (JSON.parse(value) as AuthUser).token;
        } else {
            return null;
        }
    };

    const receiveUser = (): User | null => {
        const token = getAccessToken();
        if (token) {
            return decodeUser(token);
        } else {
            return null;
        }
    };

    const [user, setUser] = useState<User | null>(receiveUser());

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

    const updateUser = (accessToken: string) => {
        const authUser: AuthUser = { token: accessToken };
        const user = decodeUser(accessToken);
        localStorage.setItem('user', JSON.stringify(authUser));
        setUser(user);
    };

    return {
        user,
        signin,
        signinGoogle,
        signout,
        updateUser,
        getAccessToken,
    };
}

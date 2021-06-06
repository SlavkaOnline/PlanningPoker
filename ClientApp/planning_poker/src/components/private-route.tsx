import React from 'react';
import { useAuth } from '../contexts/auth-context';
import { Redirect, Route } from 'react-router-dom';

export const PrivateRoute = ({ children, ...rest }: { children: any; [key: string]: any }) => {
    const { user } = useAuth();

    return (
        <Route
            {...rest}
            render={({ location }) =>
                user ? (
                    children
                ) : (
                    <Redirect
                        to={{
                            pathname: '/login',
                            state: { from: location },
                        }}
                    />
                )
            }
        />
    );
};

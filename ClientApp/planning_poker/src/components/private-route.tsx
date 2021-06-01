import React from "react";
import {useAuth} from "../contexts/auth-context";
import {Route, Redirect} from "react-router-dom";

export const PrivateRoute = ({ children, ...rest }: {children: any, [key: string]: any}) => {
    let auth = useAuth();
    return (
        <Route
            {...rest}
            render={({ location }) =>
                auth.user ? (
                    children
                ) : (
                    <Redirect
                        to={{
                            pathname: "/login",
                            state: { from: location }
                        }}
                    />
                )
            }
        />
    );
}
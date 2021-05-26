import React from 'react';
import {Navbar} from "./components/navbar";
import {HomePage} from "./pages/home-page";
import {BrowserRouter as Router, Route, Switch} from "react-router-dom";
import {LoginPage} from "./pages/login-page";
import {ProvideAuth} from "./contexts/auth-context";

export const App = () => {

    return (
        <>
            <ProvideAuth>
                <Router>
                    <Navbar/>
                    <Switch>
                        <Route exact path="/">
                            <HomePage/>
                        </Route>
                        <Route path="/login">
                            <LoginPage/>
                        </Route>
                    </Switch>
                </Router>
            </ProvideAuth>
        </>
    );
}


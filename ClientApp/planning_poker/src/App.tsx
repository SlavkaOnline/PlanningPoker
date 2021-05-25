import React from 'react';
import {Navbar} from "./components/navbar";
import {Home} from "./pages/home";
import {
    BrowserRouter as Router,
    Switch,
    Route,
    Link
} from "react-router-dom";
import {Login} from "./pages/login";

export const App = () => {

    return (
        <>
            <Router>
            <Navbar/>
            <Switch>
                <Route exact path="/">
                    <Home/>
                </Route>
                <Route path="/login">
                    <Login />
                </Route>
            </Switch>
        </Router>
        </>
    );
}


import React from 'react';
import { Navbar } from './components/navbar';
import { HomePage } from './pages/home-page';
import { BrowserRouter as Router, Route, Switch } from 'react-router-dom';
import { LoginPage } from './pages/login-page';
import { ProvideAuth } from './contexts/auth-context';
import { SessionPage } from './pages/session-page';
import { ProvideHub } from './contexts/hub-context';
import { ProvideSession } from './contexts/session-context';
import { ProvideStory } from './contexts/story-context';
import { PrivateRoute } from './components/private-route';

export const App = () => {
    return (
        <>
            <ProvideAuth>
                <Router>
                    <Navbar />
                    <Switch>
                        <Route exact path="/">
                            <HomePage />
                        </Route>
                        <Route path="/login">
                            <LoginPage />
                        </Route>
                        <PrivateRoute path="/session/:id">
                            <ProvideHub>
                                <ProvideSession>
                                    <ProvideStory>
                                        <SessionPage />
                                    </ProvideStory>
                                </ProvideSession>
                            </ProvideHub>
                        </PrivateRoute>
                    </Switch>
                </Router>
            </ProvideAuth>
        </>
    );
};

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
import { SnackbarOrigin, SnackbarProvider } from 'notistack';

export const App = () => {
    const snackBarAnchor: SnackbarOrigin = { horizontal: 'right', vertical: 'bottom' };
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
                            <SnackbarProvider maxSnack={5} autoHideDuration={2000} anchorOrigin={snackBarAnchor}>
                                <ProvideHub>
                                    <ProvideSession>
                                        <ProvideStory>
                                            <SessionPage />
                                        </ProvideStory>
                                    </ProvideSession>
                                </ProvideHub>
                            </SnackbarProvider>
                        </PrivateRoute>
                    </Switch>
                </Router>
            </ProvideAuth>
        </>
    );
};

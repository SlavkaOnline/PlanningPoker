import React from 'react';
import AppBar from '@material-ui/core/AppBar';
import Toolbar from '@material-ui/core/Toolbar';
import Typography from '@material-ui/core/Typography';
import IconButton from '@material-ui/core/IconButton';
import MenuIcon from '@material-ui/icons/Menu';
import {Link} from "react-router-dom";

import '../styles/navbar.scss'


export const Navbar = () => {

    return (
        <div className="navbar">
                            <IconButton edge="start" color="inherit" aria-label="menu">
                                <MenuIcon/>
                            </IconButton>
                        <div className="navbar_title">
                            <Typography variant="h6">
                                <Link className="navbar_link" to="/"> Planning poker </Link>
                            </Typography>
                        </div>
                        <div className="navbar_login">
                            <Typography>
                                <Link className="navbar_link" to="/login">
                                    login
                                </Link>
                            </Typography>
                        </div>
        </div>
    );
}
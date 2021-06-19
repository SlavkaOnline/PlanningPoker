import React from 'react';
import Typography from '@material-ui/core/Typography';
import IconButton from '@material-ui/core/IconButton';
import MenuIcon from '@material-ui/icons/Menu';
import { Link } from 'react-router-dom';

import styles from '../styles/navbar.module.scss';
import { useAuth } from '../contexts/auth-context';
import { User } from '../models/models';
import { Avatar, Button } from '@material-ui/core';

export const Navbar = () => {
    const { user, signout } = useAuth();

    function getAction(user: User | null) {
        if (!user) {
            return (
                <Typography>
                    <Button component={Link} className={styles.link} to="/login">
                        login
                    </Button>
                </Typography>
            );
        } else {
            return (
                <div className={styles.nameLoguot}>
                    <Avatar src={user.picture || ''} />
                    <div className={styles.name}>{user.name}</div>
                    <Typography>
                        <Button component={Link} className={styles.link} to="/" onClick={() => signout()}>
                            logout
                        </Button>
                    </Typography>
                </div>
            );
        }
    }

    return (
        <div className={styles.navbar}>
            <IconButton edge="start" color="inherit" aria-label="menu">
                <MenuIcon />
            </IconButton>
            <div className={styles.title}>
                <Typography variant="h6">
                    <Link className={styles.link} to="/">
                        {' '}
                        Planning poker{' '}
                    </Link>
                </Typography>
            </div>
            <div className={styles.login}>{getAction(user)}</div>
        </div>
    );
};

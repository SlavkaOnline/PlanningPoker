import React, { useEffect, useState } from 'react';
import { getStory, setActiveStory } from '../models/Api';
import { Story } from '../models/models';
import styles from '../styles/stories-table.module.scss';
import {
    Button,
    Paper,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Typography,
} from '@material-ui/core';
import { OwnerWrapper } from './owner-wrapper';
import { useSession } from '../contexts/session-context';
import { useStory } from '../contexts/story-context';

export const StoriesTable = () => {
    const { session, dispatch } = useSession();
    const [stories, setStories] = useState<readonly Story[]>([]);
    const { story } = useStory();

    useEffect(() => {
        if (session.stories.length) {
            const difference = session.stories.filter((s) => !stories.map((s) => s.id).includes(s));
            Promise.all(difference.map((id) => getStory(id))).then((s) => setStories([...s, ...stories]));
        }
    }, [session]);

    useEffect(() => {
        const index = stories.findIndex((s) => s.id === story.id);
        if (index > -1) {
            const arr = [...stories];
            arr[index] = story;
            setStories([...arr]);
        }
    }, [story]);

    function selectStory(id: string) {
        setActiveStory(session.id, id).then((s) => dispatch({ tag: 'init', session: s }));
    }

    return (
        <div className={styles.wrapper}>
            <Typography variant="h6">Stories</Typography>
            <TableContainer component={Paper} className={styles.table}>
                <Table size="small" aria-label="a dense table" stickyHeader>
                    <TableHead>
                        <TableRow>
                            <TableCell>Name</TableCell>
                            <TableCell align="right">Status</TableCell>
                            <TableCell align="right">Result</TableCell>
                            <TableCell align="right">Votes&nbsp;count</TableCell>
                            <OwnerWrapper component={<TableCell align="right">Actions &nbsp;</TableCell>} />
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {stories.map((story) => (
                            <TableRow className={styles.row} key={story.id}>
                                <TableCell component="th" scope="row">
                                    {story.title}
                                </TableCell>
                                <TableCell align="right">
                                    {story.isClosed
                                        ? 'Finished'
                                        : story.id === session.activeStory
                                        ? 'Selected'
                                        : story.voted.length === 0
                                        ? 'Not started'
                                        : 'In progress'}
                                </TableCell>
                                <TableCell align="right">{story.result ? story.result : ''}</TableCell>
                                <TableCell align="right">
                                    {story.voted.length}/{session.participants.length}
                                </TableCell>
                                <OwnerWrapper
                                    component={
                                        <TableCell align="right">
                                            {story.id !== session.activeStory ? (
                                                <Button
                                                    onClick={() => selectStory(story.id)}
                                                    variant="contained"
                                                    color="primary"
                                                >
                                                    Select
                                                </Button>
                                            ) : (
                                                <></>
                                            )}
                                        </TableCell>
                                    }
                                />
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
        </div>
    );
};

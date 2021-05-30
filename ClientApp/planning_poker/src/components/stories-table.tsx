import React, {useEffect, useRef, useState} from "react";
import {getStory} from "../models/Api";
import {Story} from "../models/models";
import styles from "../styles/stories-table.module.scss";
import {Button, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow} from "@material-ui/core";

type StoriesTableProps = Readonly<{
    onSelect: (id: string) => void
    stories: readonly string[]
    activeStoryId: string | null
}>


export const StoriesTable = (props: StoriesTableProps) => {

    const [stories, setStories] = useState<readonly Story[]>([])

    useEffect(() => {
            const difference = props.stories.filter(s => !stories.map(s => s.id).includes(s));
            Promise.all(difference.map(id => getStory(id)))
                .then(s => setStories([...s, ...stories]));
    }, [props.stories])


    return (
        <TableContainer component={Paper} className={styles.table}>
            <Table size="small" aria-label="a dense table" stickyHeader>
                <TableHead>
                    <TableRow>
                        <TableCell>Name</TableCell>
                        <TableCell align="right">Status</TableCell>
                        <TableCell align="right">Result</TableCell>
                        <TableCell align="right">Votes&nbsp;count</TableCell>
                        <TableCell align="right">Actions &nbsp;</TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {stories.map((story) => (
                        <TableRow key={story.id}>
                            <TableCell component="th" scope="row">
                                {story.title}
                            </TableCell>
                            <TableCell align="right">{(story.isClosed ? "Finished" : story.voted.length > 0 ? "In work" : "Not started")}</TableCell>
                            <TableCell align="right">{(story.result ? story.result : "")}</TableCell>
                            <TableCell align="right">{story.voted}</TableCell>
                            <TableCell align="right">
                                {story.id !== props.activeStoryId
                                ?<Button
                                        onClick={() => props.onSelect(story.id)}
                                        variant="contained" color="primary">
                                        Select
                                    </Button>
                                : <></>}

                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </TableContainer>
    )
}
import React, { useEffect, useState } from 'react';
import {
    Button,
    Checkbox,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    FormControl,
    FormControlLabel,
    FormLabel,
    Radio,
    RadioGroup,
    TextField,
    Tooltip,
} from '@material-ui/core';
import styles from '../styles/session-control.module.scss';
import { clearStory, closeStory, createStory, getCards } from '../models/Api';
import { useSession } from '../contexts/session-context';
import { useStory } from '../contexts/story-context';
import { useForm } from 'react-hook-form';
import FileCopySharpIcon from '@material-ui/icons/FileCopySharp';
import { Cards } from '../models/models';

type StoryBuilder = Readonly<{
    name: string;
    cardsId: string | null;
    isCustom: boolean;
    customCards: string;
    persistCustom: boolean;
}>;

export const SessionControl = () => {
    const [open, setOpen] = useState(false);
    const {
        register,
        handleSubmit,
        reset,
        getValues,
        setValue,
        formState: { errors },
    } = useForm();
    const { session, dispatch } = useSession();
    const { story, dispatch: dispatchStory } = useStory();
    const [cards, setCards] = useState<readonly Cards[]>([]);

    useEffect(() => {
        getCards().then((cards) => setCards(cards));
    }, []);

    const onSubmit = (storyBuilder: StoryBuilder) => {
        setOpen(false);
        createStory(
            session.id,
            storyBuilder.name,
            storyBuilder.cardsId,
            !storyBuilder.cardsId,
            storyBuilder.customCards?.split(',').filter((v) => v) || [],
        ).then((s) => dispatch({ tag: 'init', session: s }));
        const custom = storyBuilder.customCards;
        reset();
        setValue('customCards', custom);
    };

    function flipCards() {
        if (story.id) {
            closeStory(story.id).then((s) => dispatchStory({ tag: 'init', story: s }));
        }
    }

    function clear() {
        if (story.id) {
            clearStory(story.id).then((s) => dispatchStory({ tag: 'init', story: s }));
        }
    }

    return (
        <div className={styles.wrapper}>
            <div className={styles.link}>
                <TextField
                    className={styles.field}
                    disabled
                    label="Share link"
                    variant="outlined"
                    defaultValue={window.location.href}
                />
                <Tooltip title={'Copy to clipboard'}>
                    <FileCopySharpIcon
                        className={styles.copy}
                        color="primary"
                        onClick={() => {
                            navigator.clipboard.writeText(window.location.href);
                        }}
                    />
                </Tooltip>
            </div>
            <div className={styles.actions}>
                <Button className={styles.action} variant="contained" color="primary" onClick={() => setOpen(true)}>
                    Create story
                </Button>
                {!story.isClosed ? (
                    story.voted.length ? (
                        <Button
                            onClick={() => flipCards()}
                            className={styles.action}
                            variant="contained"
                            color="primary"
                        >
                            Flip cards
                        </Button>
                    ) : (
                        <></>
                    )
                ) : (
                    <Button onClick={() => clear()} className={styles.action} variant="contained" color="primary">
                        Clear story
                    </Button>
                )}
            </div>

            <Dialog open={open} aria-labelledby="form-dialog-title">
                <form onSubmit={handleSubmit(onSubmit)}>
                    <DialogTitle id="form-dialog-title"> Create New Story</DialogTitle>
                    <DialogContent className={styles.dialog}>
                        <TextField
                            {...register('name')}
                            autoFocus
                            margin="dense"
                            id="name"
                            label="Story title"
                            type="text"
                            variant="outlined"
                            fullWidth
                        />
                        <FormControl component="fieldset">
                            <FormLabel component="legend">Cards type</FormLabel>
                            <RadioGroup aria-label="Cards type" {...register('cardsId')}>
                                {cards.map((c) => (
                                    <FormControlLabel key={c.id} value={c.id} control={<Radio />} label={c.caption} />
                                ))}
                                <FormControlLabel value={''} control={<Radio />} label="Custom" />
                            </RadioGroup>
                            <TextField
                                multiline
                                fullWidth
                                rows={2}
                                defaultValue={getValues('persistCustom') || ''}
                                variant="outlined"
                                {...register('customCards')}
                            />
                        </FormControl>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={() => setOpen(false)} color="primary">
                            Cancel
                        </Button>
                        <Button type={'submit'} color="primary">
                            Create
                        </Button>
                    </DialogActions>
                </form>
            </Dialog>
        </div>
    );
};

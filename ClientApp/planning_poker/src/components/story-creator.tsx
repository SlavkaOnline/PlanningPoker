import React, { useEffect, useState } from 'react';
import {
    Button,
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
} from '@material-ui/core';
import styles from '../styles/session-control.module.scss';
import { CustomCardsInput } from './custom-cards-input';
import { useSession } from '../contexts/session-context';
import { useStory } from '../contexts/story-context';
import { Cards } from '../models/models';
import { createStory, getCards } from '../models/Api';

type Validator<T> = Readonly<{
    predicate: (value: T) => boolean;
    message: string;
}>;
type StoryForm = Readonly<{
    title: string;
    cardsId: string;
    customCards: readonly string[];
    isModified: boolean;
    validators: {
        [k in keyof Omit<StoryForm, 'errors' | 'validators' | 'isModified' | 'cardsId'>]?: Validator<any>;
    };
    errors: { [k in keyof Omit<StoryForm, 'errors' | 'validators' | 'isModified' | 'cardsId'>]?: string | null };
}>;

export const StoryCreator = () => {
    const [open, setOpen] = useState(false);
    const [storyForm, setStoryFrom] = useState<StoryForm>({
        title: '',
        cardsId: '',
        customCards: [],
        errors: {},
        validators: {
            title: { predicate: (title: string) => !title, message: 'Title is required' },
            customCards: {
                predicate: (cards: readonly string[]) => cards.length < 2,
                message: 'Cards count should be greater than one',
            },
        },
        isModified: false,
    });
    const { session, dispatch } = useSession();
    const { story, dispatch: dispatchStory } = useStory();

    const [cards, setCards] = useState<readonly Cards[]>([]);

    useEffect(() => {
        getCards().then((cards) => setCards(cards));
    }, []);

    useEffect(() => {
        if (cards.length) {
            setStoryFrom({ ...storyForm, cardsId: cards[0].id });
        }
    }, [cards]);

    function create() {
        if (!Object.keys(storyForm.errors).length && storyForm.isModified) {
            setOpen(false);
            createStory(session.id, storyForm.title, storyForm.cardsId, storyForm.customCards).then((s) =>
                dispatch({ tag: 'init', session: s }),
            );
            setStoryFrom({ ...storyForm, title: '', isModified: false, errors: {} });
        }
    }

    function createAndAdd() {
        if (!Object.keys(storyForm.errors).length && storyForm.isModified) {
            createStory(session.id, storyForm.title, storyForm.cardsId, storyForm.customCards).then((s) =>
                dispatch({ tag: 'init', session: s }),
            );
            setStoryFrom({ ...storyForm, title: '' });
        }
    }

    function cancel() {
        setOpen(false);
        setStoryFrom({ ...storyForm, title: '', isModified: false, errors: {}, cardsId: cards[0]?.id || '' });
    }

    function inputCards(cards: readonly string[]) {
        if (storyForm.validators['customCards']?.predicate(cards)) {
            const errors = { ...storyForm.errors };
            errors['customCards'] = storyForm.validators['customCards']?.message;
            setStoryFrom({ ...storyForm, errors: errors, isModified: true });
        } else {
            const errors = { ...storyForm.errors };
            delete errors['customCards'];
            setStoryFrom({ ...storyForm, errors: errors, customCards: cards, isModified: true });
        }
    }
    function setCardsId(event: React.ChangeEvent<HTMLInputElement>) {
        const errors = { ...storyForm.errors };
        delete errors['customCards'];
        setStoryFrom({ ...storyForm, cardsId: event.target.value, isModified: true, errors: {} });
    }

    function setCustomCardsId() {
        setStoryFrom({ ...storyForm, cardsId: '', isModified: true });
    }

    function setTitle(event: React.ChangeEvent<HTMLInputElement>) {
        const title = event.target.value;
        if (storyForm.validators['title']?.predicate(title)) {
            const errors = { ...storyForm.errors };
            errors['title'] = storyForm.validators['title']?.message;
            setStoryFrom({ ...storyForm, title: title, errors: errors, isModified: true });
        } else {
            const errors = { ...storyForm.errors };
            delete errors['title'];
            setStoryFrom({ ...storyForm, errors: errors, title: title, isModified: true });
        }
    }

    return (
        <>
            <Button className={styles.action} variant="text" color="default" onClick={() => setOpen(true)}>
                Create story
            </Button>

            <Dialog className={styles.dialog} open={open} aria-labelledby="form-dialog-title">
                <form>
                    <DialogTitle id="form-dialog-title"> Create New Story</DialogTitle>
                    <DialogContent className={styles.dialog}>
                        <FormControl fullWidth={true}>
                            <FormControl component="fieldset">
                                <TextField
                                    autoFocus
                                    margin="dense"
                                    id="name"
                                    label="Story title"
                                    type="text"
                                    variant="outlined"
                                    fullWidth={true}
                                    onInput={setTitle}
                                />
                                <p className={styles.error}> {storyForm.errors?.title} </p>
                            </FormControl>
                            <FormControl component="fieldset">
                                <FormLabel component="legend">Cards type</FormLabel>
                                <RadioGroup aria-label="Cards type" value={storyForm.cardsId} onChange={setCardsId}>
                                    {cards.map((c) => (
                                        <FormControlLabel
                                            key={c.id}
                                            value={c.id}
                                            control={<Radio />}
                                            label={c.caption}
                                        />
                                    ))}
                                    <FormControlLabel
                                        value={''}
                                        control={<Radio />}
                                        label="Custom (ex: simple-dimple, pop-it)"
                                    />
                                </RadioGroup>
                                <CustomCardsInput
                                    onCardsInput={inputCards}
                                    onFocus={setCustomCardsId}
                                    initValue={storyForm.customCards}
                                />
                                <p className={styles.error}> {storyForm.errors?.customCards} </p>
                            </FormControl>
                        </FormControl>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={cancel} color="default">
                            Cancel
                        </Button>
                        <Button disabled={!storyForm.isModified} onClick={createAndAdd} color="default">
                            Create and Add
                        </Button>
                        <Button disabled={!storyForm.isModified} onClick={create} color="default">
                            Create
                        </Button>
                    </DialogActions>
                </form>
            </Dialog>
        </>
    );
};

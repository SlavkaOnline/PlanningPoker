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
import { GeneralDialog } from './general-dialog';
import { CreateStoryForm } from './create-story-form';

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
    function closeCreateDialog() {
        setOpen(false);
    }

    return (
        <>
            <Button className={styles.action} variant="text" color="default" onClick={() => setOpen(true)}>
                Create story
            </Button>
            <GeneralDialog
                content={<CreateStoryForm onCreate={closeCreateDialog} />}
                title={'Create a new story'}
                open={open}
                onClose={closeCreateDialog}
            />
        </>
    );
};

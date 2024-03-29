import React, { useEffect, useState } from 'react';
import styles from '../styles/cards.module.scss';
import { removeVote, vote } from '../models/Api';
import { useStory } from '../contexts/story-context';

type CardProps = Readonly<{
    isSelect: boolean;
    type: string;
    onSelect: () => void;
}>;

const Card = (props: CardProps) => {
    return (
        <div onClick={() => props.onSelect()} className={`${styles.card} ${props.isSelect ? styles.select : ''}`}>
            <div className={styles.inner}>{props.type}</div>
        </div>
    );
};

export const Cards = (props: { cardsTypes: readonly string[] }) => {
    const { story, dispatch } = useStory();
    const [selected, setSelected] = useState<string>(story.userCard);

    useEffect(() => {
        setSelected(story.userCard);
    }, [story.id]);

    function select(card: string) {
        const oldValue = selected;
        const newValue = selected === card ? '' : card;
        setSelected(newValue);
        (oldValue === card ? removeVote(story.id) : vote(story.id, newValue))
            .then((s) => dispatch({ tag: 'init', story: s }))
            .catch(() => setSelected(oldValue));
    }

    return (
        <div className={styles.wrapper}>
            <div className={styles.cards}>
                {props.cardsTypes.map((c) => (
                    <Card onSelect={() => select(c)} key={c} isSelect={selected === c} type={c} />
                ))}
            </div>
        </div>
    );
};

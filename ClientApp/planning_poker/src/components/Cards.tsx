import React, {useState} from "react";
import styles from "../styles/cards.module.scss"
import {removeVote, vote} from "../models/Api";
import {useStory} from "../contexts/story-context";

const cardsTypes = ['XXS', 'XS', 'S', 'M', 'L', 'XL', 'XXL', 'Question']

type CardProps = Readonly<{
    isSelect: boolean
    type: string
    onSelect: () => void
}>

const Card = (props: CardProps) => {

    return (
        <div
            onClick={() => props.onSelect()}
            className={`${styles.card} ${props.isSelect ? styles.select : ""}`}>
            <div className={styles.inner}>
                {props.type}
            </div>
        </div>)
}

export const Cards = () => {

    const {story, dispatch} = useStory();
    const [selected, setSelected] = useState<string>("");

    function select(card: string){
        const oldValue = selected;
        let newValue = selected === card ? "" : card;
        setSelected(newValue);
        (oldValue === card ? removeVote(story.id) : vote(story.id, newValue) )
                .then(s => dispatch({tag: "init", story: s}))
                .catch(_ => setSelected(oldValue));
    }

    return (
        <div className={styles.wrapper}>
            <div className={styles.cards}>
                {cardsTypes.map(c => <Card
                    onSelect={() => select(c)}
                    key={c}
                    isSelect={selected === c}
                    type={c}/>)}
            </div>
        </div>
    )
}
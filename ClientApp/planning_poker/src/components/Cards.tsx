import React from "react";
import styles from "../styles/cards.module.scss"

const arr = ['XXS', 'XS', 'S', 'M', 'L', 'XL', 'XXL', 'Question']
type CardType = typeof arr[7];


type CardProps = Readonly<{
    type: CardType
}>

const Card = (props: CardProps) => {

    return (
        <div className={styles.card}>
            <div className={styles.inner}>
                {props.type}
            </div>
        </div>)
}

export const Cards = () => {
    return (
        <div className={styles.wrapper}>
            <div className={styles.cards}>
                {arr.map(t => <Card type={t}/>)}
            </div>
        </div>
    )
}
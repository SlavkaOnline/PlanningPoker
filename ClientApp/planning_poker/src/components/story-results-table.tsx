import React from 'react';
import { StatisticsResult } from '../models/models';
import styles from '../styles/story-results-table.module.scss';

export type StoryResultsTable = Readonly<{
    statistics: StatisticsResult;
}>;

export const StoryResultsTable = (props: StoryResultsTable) => {
    return (
        <div className={styles.wrapper}>
            {Object.keys(props.statistics)
                .sort((a, b) => props.statistics[b].percent - props.statistics[a].percent)
                .map((r) => (
                    <div key={r} className={styles.result}>
                        <div className={styles.card}>
                            <span className={styles.value}>{r}</span>
                            &nbsp;
                            <span>{`${props.statistics[r].percent}%`}</span>
                        </div>
                        <div className={styles.participants}>
                            {props.statistics[r].voters.map((voter) => (
                                <div key={voter.name} className={styles.participant}>
                                    <span>-&nbsp;{voter.name}</span>
                                    <span>&nbsp;{`(${voter.duration})`}</span>
                                </div>
                            ))}
                        </div>
                    </div>
                ))}
        </div>
    );
};

import React from 'react';
import { Chart } from 'react-google-charts';
import styles from '../styles/story-result.module.scss';
import { StoryResultsTable } from './story-results-table';
import { Statistics } from '../models/models';

type StoryResult = Readonly<{
    statistics: Statistics;
    duration: string;
    title: string;
}>;

export const StoryResult = (props: StoryResult) => {
    return (
        <div className={styles.wrapper}>
            {props.statistics ? (
                <Chart
                    className={styles.chart}
                    chartType={'PieChart'}
                    graph_id={props.title}
                    width={'100%'}
                    height={'100%'}
                    loader={<div>Loading Chart</div>}
                    data={[
                        ['Card', 'Percent'],
                        ...Object.keys(props.statistics.result).map((card) => [
                            card,
                            props.statistics.result[card].voters.length,
                        ]),
                    ]}
                    options={{
                        title: props.title,
                        is3D: true,
                    }}
                />
            ) : (
                <></>
            )}
            <div className={styles.stats}>
                <div className={styles.duration}>Duration: {props.duration}</div>
                <div className={styles.table}>
                    <StoryResultsTable statistics={props.statistics.result} />
                </div>
            </div>
        </div>
    );
};

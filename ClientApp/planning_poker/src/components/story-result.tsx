import React from 'react';
import { useStory } from '../contexts/story-context';
import { Chart } from 'react-google-charts';
import styles from '../styles/story-result.module.scss';
import { StoryResultsTable } from './story-results-table';

export const StoryResult = () => {
    const { story } = useStory();

    return (
        <div className={styles.wrapper}>
            {story.statistics ? (
                <Chart
                    className={styles.chart}
                    chartType={'PieChart'}
                    graph_id="PieChart"
                    width={'100%'}
                    height={'100%'}
                    loader={<div>Loading Chart</div>}
                    data={[
                        ['Card', 'Percent'],
                        ...Object.keys(story.statistics).map((card) => [card, story.statistics[card].voters.length]),
                    ]}
                    options={{
                        title: 'Results',
                    }}
                />
            ) : (
                <></>
            )}
            <div className={styles.stats}>
                <div className={styles.duration}>Working time: {story.duration}</div>
                <div className={styles.table}>
                    <StoryResultsTable statistics={story.statistics} />
                </div>
            </div>
        </div>
    );
};

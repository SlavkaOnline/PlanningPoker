import React from 'react';
import { useStory } from '../contexts/story-context';
import { Chart } from 'react-google-charts';
import styles from '../styles/story-result.module.scss';

export const StoryResult = () => {
    const { story } = useStory();

    return (
        <div className={styles.wrapper}>
            {story.statistics ? (
                <Chart
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
            <div className={styles.tables}>
                {Object.keys(story.statistics).map((card) => (
                    <div key={card}>
                        {card} - {story.statistics[card].voters.map((v) => v.name).join(', ')}
                    </div>
                ))}
            </div>
        </div>
    );
};

import React from 'react';
import { useAuth } from '../contexts/auth-context';
import { SessionCreator } from '../components/session-creator';
import styles from '../styles/home.module.scss';
import { GameInfo } from '../components/game-info';
import planningPoker from '../assets/plannnig_poker.png';
import orderingRule from '../assets/ordering_rule.png';
import dotVoting from '../assets/dot_voting.png';

export const HomePage = () => {
    const { user } = useAuth();

    function renderSessionCreator() {
        if (user != null) {
            return <SessionCreator />;
        } else {
            return <div>Please log in to create a new session</div>;
        }
    }

    function getDotVotingInfo() {
        return (
            <>
                <p>
                    Dot Voting is the visual result of the Agile estimation concerning the priority of the task. It
                    allows the team to see the whole picture for the entire set of tasks by using a number of points
                    (markers). The main idea behind this Agile estimation technique lies in giving public access to all
                    the required tasks and the ability of team members to sequentially define tasks by their complexity
                    using a limited set of points. t’s an effective method when the number of tasks is small, and works
                    great for their visual analysis. But it’s not the most reliable method of assessment, as it is very
                    superficial and serves as more of a tool for deciding the order of tasks in a sprint.
                </p>
                <ol>
                    <li>
                        The leader lays out the necessary set of tasks on a board (each task is described on a separate
                        sticker, with the stickers being organized by priority: for example, from left to right – from
                        highest to lowest priority)
                    </li>
                    <li>
                        Each member of the team is given a limited number of dots (or the ability to indicate them with
                        a marker)
                    </li>
                    <li>
                        Each team member comes up to the board and distributes the points according to the complexity of
                        the tasks: for example, an employee has 5 points for three tasks. They will put one dot under
                        the first (highest priority), three dots under the second (medium priority), and the last
                        remaining dot under the third with the lowest priority.
                    </li>
                    <li>
                        The team leader rearranges the tasks (stickers) in accordance with the complexity of the tasks,
                        and not by priority, and the team decides on which task will be the best to start the sprint
                        with.
                    </li>
                </ol>
            </>
        );
    }

    function getOrderingRuleInfo() {
        return (
            <>
                <p>
                    Ordering Rule is a game estimation technique that involves the interaction of all members of the
                    team; it aims at equating a set of tasks to a difficulty scale. The scale reflects the complexity of
                    the task and has a minimum initial value and a maximum final value in terms of labor intensity.
                    Intermediate difficulty segments are set between the lowest and ultimate values.
                </p>
                <ol>
                    <li>The team leader randomly distributes all the necessary tasks on a rating scale</li>
                    <li>
                        Each team member takes turns doing one of the permitted actions:
                        <ul>
                            <li>move one task left or right by a single increment</li>
                            <li>
                                discuss the task with colleagues and ask the necessary questions about its
                                implementation
                            </li>
                            <li>skip the turn</li>
                        </ul>
                    </li>
                    <li>
                        The estimation of the tasks is finished when all of the colleagues skip their turns, indicating
                        that they agree with the achieved estimation for all the tasks.
                    </li>
                </ol>
            </>
        );
    }

    function getPlanningPokerInfo() {
        return (
            <>
                <p>
                    The principle is that each team member receives a deck of cards with a list of points and gives his
                    evaluation of the task after all the clarifications and discussions on its implementation.
                </p>
                <ol>
                    <li>The members receive cards.</li>
                    <li>They discuss the task and its nuances.</li>
                    <li>
                        Each member of the team chooses – in his opinion – the most suitable value for the task’s
                        complexity. Then, he puts it on the table face down, so that the teammates can’t see the
                        selected grade and don’t have their opinions swayed.{' '}
                    </li>
                    <li>
                        When all members of the team have evaluated the task, they simultaneously flip their cards,
                        rating up.{' '}
                    </li>
                    <li>Those who have selected the lowest and the highest scores explain their choices.</li>
                    <li>
                        An average rating is decided, or, based on the comments of members of the team, a re-voting is
                        carried out from the previous step to reduce the results to the lowest possible rating spread.
                    </li>
                </ol>
            </>
        );
    }

    return (
        <div className={styles.wrapper}>
            <div className={styles.game}>
                <GameInfo
                    title={'Planning Poker'}
                    img={planningPoker}
                    info={getPlanningPokerInfo()}
                    action={renderSessionCreator()}
                />
            </div>
            <div className={styles.game}>
                <GameInfo title={'Ordering Rule'} img={orderingRule} info={getOrderingRuleInfo()} action={null} />
            </div>
            <div className={styles.game}>
                <GameInfo title={'Dot Voting'} img={dotVoting} info={getDotVotingInfo()} action={null} />
            </div>
        </div>
    );
};

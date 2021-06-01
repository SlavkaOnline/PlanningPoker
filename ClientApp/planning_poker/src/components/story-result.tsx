import React from "react";
import {useStory} from "../contexts/story-context";

export const StoryResult = () => {

    const {story} = useStory()

    return (<div>
        {Object.keys(story.statistics).map(card =>
            <div key={card}>{card}
                <span> - {story.statistics[card].percent}%</span> {story.statistics[card].voters.map(v => v.name).join(", ")}</div>)}
    </div>)

}
import React from "react";
import {useSession} from "../contexts/session-context";
import {useStory} from "../contexts/story-context";

export const UsersList = () => {

    const {session} = useSession();
    const {story} = useStory();

    function checkVote(userId: string) {
        return story.voted.findIndex(v => v.id == userId) > -1;
    }

    return (
        <div>
            {
                session.participants.map(p => <div key={p.id} >{p.name} &nbsp; {checkVote(p.id) ? ' - Voted' : ''}</div>)
            }
        </div>
    )
}
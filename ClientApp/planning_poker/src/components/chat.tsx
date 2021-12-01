import React, { useEffect, useReducer, useState } from 'react';
import ChatIcon from '@material-ui/icons/Chat';
import SendIcon from '@material-ui/icons/Send';
import { GeneralDialog } from './general-dialog';
import { useSession } from '../contexts/session-context';
import { useHub } from '../contexts/hub-context';
import Badge from '@material-ui/core/Badge';

import styles from '../styles/chat.module.scss';
import { Button } from '@material-ui/core';
import { Session } from '../models/models';

type Message = Readonly<{
    id: string;
    user: string;
    payload: string;
}>;

type ChatDialogProps = Readonly<{
    messages: readonly Message[];
    onSend: (text: string) => void;
}>;

const ChatDialog = (props: ChatDialogProps) => {
    const [text, setText] = useState('');

    const onEnterPress = (e: any) => {
        if (e.keyCode == 13 && e.shiftKey == false) {
            e.preventDefault();
            send();
        }
    };

    function send() {
        props.onSend(text);
        setText('');
    }

    return (
        <div className={styles.wrapper}>
            <div className={styles.messages}>
                {props.messages.map((m) => (
                    <div key={m.id}>
                        <div>{m.user}</div>
                        <div className={styles.text}>{m.payload}</div>
                    </div>
                ))}
            </div>
            <div className={styles.panel}>
                <div className={styles.controls}>
                    <textarea
                        onKeyDown={onEnterPress}
                        className={styles.input}
                        autoFocus={true}
                        value={text}
                        onChange={(e) => setText(e.target.value)}
                    />
                    <Button className={styles.action} color={'default'} onClick={send}>
                        <SendIcon />
                    </Button>
                </div>
            </div>
        </div>
    );
};

type ChatState = Readonly<{
    messages: Message[];
    isOpenDialog: boolean;
    unread: number;
}>;

type setDialogState = Readonly<{
    tag: 'openDialog';
    state: boolean;
}>;

type ReceiveMessage = Readonly<{
    tag: 'receiveMessage';
    message: Message;
}>;

type Action = setDialogState | ReceiveMessage;

const reducer = (state: ChatState, action: Action) => {
    switch (action.tag) {
        case 'openDialog':
            return { ...state, isOpenDialog: action.state, unread: 0 };
        case 'receiveMessage':
            state.messages.unshift(action.message);
            return {
                ...state,
                unread: state.isOpenDialog ? state.unread : state.unread + 1,
            };
    }
};

export const Chat = () => {
    const [state, dispatch] = useReducer(reducer, { messages: [], isOpenDialog: false, unread: 0 });
    const hub = useHub();
    const { session } = useSession();

    useEffect(() => {
        hub?.send('Join', session.id);
        hub?.on('chatMessage', (id, user, message) => {
            dispatch({ tag: 'receiveMessage', message: { id: id, user: user, payload: message } });
        });
    }, [hub]);

    function send(text: string) {
        hub?.send('SendMessage', session.id, text);
    }

    return (
        <div>
            <div onClick={() => dispatch({ tag: 'openDialog', state: true })}>
                <Badge color="secondary" badgeContent={state.unread}>
                    <ChatIcon />
                </Badge>
                <div>Chat</div>
            </div>
            <GeneralDialog
                content={<ChatDialog messages={state.messages} onSend={send} />}
                title={'Chat'}
                open={state.isOpenDialog}
                onClose={() => dispatch({ tag: 'openDialog', state: false })}
            />
        </div>
    );
};

import React, { useEffect, useReducer, useRef, useState } from 'react';
import ChatIcon from '@material-ui/icons/Chat';
import SendIcon from '@material-ui/icons/Send';
import { GeneralDialog } from './general-dialog';
import { useSession } from '../contexts/session-context';
import { useHub } from '../contexts/hub-context';
import Badge from '@material-ui/core/Badge';

import styles from '../styles/chat.module.scss';
import { Avatar, Button } from '@material-ui/core';
import { ISubscription } from '@microsoft/signalr';
import { useAuth } from '../contexts/auth-context';

type ChatUser = Readonly<{
    id: string;
    name: string;
    picture: string;
}>;

type Message = Readonly<{
    id: string;
    user: ChatUser;
    text: string;
}>;

type ChatDialogProps = Readonly<{
    messages: readonly Message[];
    onSend: (text: string) => void;
}>;

const ChatDialog = (props: ChatDialogProps) => {
    const [text, setText] = useState('');
    const { user } = useAuth();
    const messagesView = useRef<any>();
    const onEnterPress = (e: any) => {
        if (e.keyCode == 13 && e.shiftKey == false) {
            e.preventDefault();
            send();
            messagesView.current.scrollIntoView({
                behavior: 'smooth',
                block: 'nearest',
                inline: 'start',
            });
        }
    };

    function send() {
        const msg = text.trim();
        if (msg) {
            props.onSend(text);
            setText('');
        }
    }

    const getMessageTypeStyle = (id: string) => (id === user?.id ? styles.my : '');

    return (
        <div className={styles.wrapper}>
            <div ref={messagesView} className={styles.messages}>
                {props.messages.map((m) => (
                    <div className={styles.messageBlock + ' ' + getMessageTypeStyle(m.user.id)} key={m.id}>
                        <Avatar src={m.user.picture} />
                        <div className={styles.message + ' ' + getMessageTypeStyle(m.user.id)}>
                            <div className={styles.name}>{m.user.name}</div>
                            <div className={styles.text}>{m.text}</div>
                        </div>
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
                        <SendIcon className={styles.send} />
                    </Button>
                </div>
            </div>
        </div>
    );
};

type ChatState = Readonly<{
    messages: readonly Message[];
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
        case 'receiveMessage': {
            const messages = [action.message, ...state.messages];
            return {
                ...state,
                messages: messages,
                unread: state.isOpenDialog ? state.unread : state.unread + 1,
            };
        }
    }
};

export const Chat = () => {
    const [state, dispatch] = useReducer(reducer, { messages: [], isOpenDialog: false, unread: 0 });
    const hub = useHub();
    const { session } = useSession();

    useEffect(() => {
        let subscriptions: ISubscription<any> | null;
        const createSubscriptions = () =>
            hub?.stream('Chat', session.id).subscribe({
                next: (message: Message) => {
                    dispatch({ tag: 'receiveMessage', message });
                },
                complete: () => console.log('complete'),
                error: (e: any) => console.log(e),
            }) || null;

        subscriptions = createSubscriptions();

        hub?.onreconnected((connectionId) => {
            subscriptions?.dispose();
            subscriptions = createSubscriptions();
        });
        return () => subscriptions?.dispose();
    }, [session.id, hub]);

    function send(text: string) {
        hub?.send('SendMessage', session.id, text);
    }

    return (
        <div>
            <div className={styles.chat} onClick={() => dispatch({ tag: 'openDialog', state: true })}>
                <Badge color="secondary" badgeContent={state.unread} max={9}>
                    <ChatIcon className={styles.icon} />
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

import React, { useEffect, useState } from 'react';
import { TextField } from '@material-ui/core';

type CustomCardsInputProps = Readonly<{
    initValue: readonly string[];
    onCardsInput: (value: readonly string[]) => void;
    onFocus: () => void;
}>;

export const CustomCardsInput = (props: CustomCardsInputProps) => {
    const [str, setStr] = useState(props.initValue.join(', '));

    useEffect(() => {
        if (str.length) {
            const array = str
                .split(',')
                .map((s) => s.trim())
                .filter((s) => s !== '');
            props.onCardsInput(array);
        }
    }, [str]);

    function checkStr(event: React.ChangeEvent<HTMLInputElement>) {
        const s = event.target.value;
        if (s.length > 0 && s[s.length - 1] === ' ') {
            if (s.length >= 2 && ![' ', ','].includes(s[s.length - 2])) {
                setStr(s.slice(0, s.length - 1) + ', ');
            } else {
                setStr(s);
            }
        } else {
            setStr(s);
        }
    }

    return (
        <>
            <TextField
                multiline
                fullWidth
                rows={2}
                variant="outlined"
                value={str}
                onInput={checkStr}
                onFocus={(_) => props.onFocus()}
            />
        </>
    );
};

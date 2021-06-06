import React from 'react';
import { useAuth } from '../contexts/auth-context';
import { useSession } from '../contexts/session-context';

export type OwnerWrapperProps = Readonly<{
    component: JSX.Element;
}>;

export const OwnerWrapper = (props: OwnerWrapperProps) => {
    const { user } = useAuth();
    const { session } = useSession();

    return user?.id === session.ownerId ? props.component : <></>;
};

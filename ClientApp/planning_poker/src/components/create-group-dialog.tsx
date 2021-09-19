import React from 'react';
import { Dialog } from '@material-ui/core';
import { CreateGroupWizard } from './create-group-wizard';

export type CreateGroupDialogProps = Readonly<{
    open: boolean;
    onClose: () => void;
}>;

export const CreateGroupDialog = (props: CreateGroupDialogProps) => {
    return (
        <Dialog {...props}>
            <CreateGroupWizard />
        </Dialog>
    );
};

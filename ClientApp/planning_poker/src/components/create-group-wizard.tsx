import React, { useState } from 'react';
import { Step, StepLabel, Stepper, TextField } from '@material-ui/core';
import { CreateGroupForm } from './create-group-form';
import { Group } from '../models/models';

import styles from '../styles/create-group-wizard.module.scss';
import { ManageUserGroup } from './manage-user-group';

export const CreateGroupWizard = () => {
    const [activeStep, setActiveStep] = useState(0);
    const steps = ['Create a new group', 'Move users'];
    const [group, setGroup] = useState<Group | null>(null);

    function onGroupCreated(g: Group) {
        setActiveStep(1);
        setGroup(g);
    }

    function getComponentForStep(step: number) {
        switch (step) {
            case 0:
                return <CreateGroupForm onCreate={onGroupCreated} />;
            case 1:
                if (group) {
                    return <ManageUserGroup group={group} />;
                } else {
                    return <></>;
                }
        }
    }

    return (
        <div className={styles.wrapper}>
            <Stepper activeStep={activeStep}>
                {steps.map((label, index) => {
                    const stepProps: { completed?: boolean } = {};
                    const labelProps: { optional?: React.ReactNode } = {};
                    return (
                        <Step key={label} {...stepProps}>
                            <StepLabel {...labelProps}>{label}</StepLabel>
                        </Step>
                    );
                })}
            </Stepper>
            <div className={styles.body}>{getComponentForStep(activeStep)}</div>
        </div>
    );
};

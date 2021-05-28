import React, {useEffect, useRef, useState} from 'react'
import {CircularProgress} from "@material-ui/core";
import styles from '../styles/busy-wrapper.module.scss'

export type BusyWrapperProps = Readonly<{
    Component: React.ElementRef<any>
}>

export const BusyWrapper = (props: BusyWrapperProps) => {
    const [isBusy, setBusy] = useState(false)

    const refComponent = useRef<any>()

    useEffect(() => {
            if (refComponent.current) {
                (refComponent.current as HTMLElement).onclick = function () {
                    setBusy(true);
                };
            }
            return;
        }
        , [props])

    function renderComponent() {
        if (isBusy) {
            return (
                <div className={styles.busy}>
                    <CircularProgress/>
                </div>)
        } else {
            return (
                <div ref={refComponent}>
                    props.Component
                </div>)
        }

    }


    return (
        renderComponent()
    )
}
module Tests.Helper

module Aggregate =
    let createHandler
        (producer: 'state -> 'command -> Result<'event, 'error>)
        (reducer: 'state -> 'event -> 'state) : 'command -> 'state  -> Result<'state, 'error> =

        let handler cmd state =
            producer state cmd
            |> Result.map(reducer state)

        handler
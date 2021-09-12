namespace PlanningPoker.Extensions

[<System.Runtime.CompilerServices.Extension>]
[<AutoOpen>]
module OptionExtensions =

        [<System.Runtime.CompilerServices.Extension>]
        [<CompiledName("GetValue")>]
        let getValue(value: 'a option) =
            match value with
            | Some v -> v
            | _ -> Unchecked.defaultof<'a>
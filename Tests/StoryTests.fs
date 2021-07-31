namespace Tests

open PlanningPoker.Domain
open FSharp.UMX
open Xunit

module StoryTests =

    [<Fact>]
    let ``Array cards has duplicates``() =
        let cards = [| %"1"; %"1"|]
        match Story.validateCards cards  with
        | Error e -> e = Errors.CardsHasDuplicatesValues
        | Ok _-> false

    [<Fact>]
    let ``Array cards hasn't duplicates``() =
        let cards = [| %"1"; %"2"|]
        match Story.validateCards cards  with
        | Error _ -> false
        | Ok _-> true


    [<Fact>]
    let ``Array shouldn't be empty``() =
        let cards = [||]
        match Story.validateCards cards  with
        | Error e -> e = Errors.CardsHasNotValues
        | Ok _-> false


namespace Tests

module PropertyTests =
    open System
    open FsCheck.Xunit
    open PlanningPoker.Domain
    open Xunit
    open FSharp.UMX

    let private equalFloat (a: float) (b: float) (c: float) = Math.Abs a - b < c

    [<Property>]
    let ``Sum of elements of statistics equal 100`` (story: ActiveStory) =
        let sum =
            fst (Story.calculateStatistics story (Started <| DateTime.UtcNow.AddMinutes(10.0)))
            |> Map.toSeq
            |> Seq.map snd
            |> Seq.map (fun r -> r.Percent)
            |> Seq.sum

        equalFloat sum 100.0 1.0


    [<Property>]
    let ``The result card of statistics has max percent`` (story: ActiveStory) =
        let stats, card = Story.calculateStatistics story (Started <| DateTime.UtcNow.AddMinutes(10.0))
        let maxVote =
            stats
            |> Map.toSeq
            |> Seq.sortByDescending (fun (_, voteResult) -> voteResult.Percent)
            |> Seq.head

        let maxPercent = (snd maxVote).Percent
        let maxCards = stats
                       |> Map.toSeq
                       |> Seq.filter (fun (_, voteResult) -> equalFloat voteResult.Percent maxPercent 0.1)
                       |> Seq.map fst
                       |> Seq.toArray

        Array.contains card maxCards

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
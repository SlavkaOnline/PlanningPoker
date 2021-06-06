namespace Tests

module PropertyTests =
    open System
    open FsCheck.Xunit
    open PlanningPoker.Domain

    let private equalFloat (a: float) (b: float) (c: float) = Math.Abs a - b < c

    [<Property>]
    let ``Sum of elements of statistics equal 100`` (story: ActiveStory) =
        let sum =
            fst (Story.calculateStatistics story)
            |> Map.toSeq
            |> Seq.map snd
            |> Seq.map (fun r -> r.Percent)
            |> Seq.sum

        equalFloat sum 100.0 1.0


    [<Property>]
    let ``The result card of statistics has max percent`` (story: ActiveStory) =
        let stats, card = Story.calculateStatistics story
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

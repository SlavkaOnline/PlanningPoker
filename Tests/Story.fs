module Tests.Story

open System
open System.Collections.Generic
open FsCheck.Xunit
open PlanningPoker.Domain
open PlanningPoker.Domain.CommonTypes
open Xunit
open FSharp.UMX
open Swensen.Unquote

let private equalFloat (a: float) (b: float) (c: float) = Math.Abs a - b < c

let getUser () =
    { Id = % Guid.NewGuid()
      Name = ""
      Picture = Some "" }

[<Property>]
let ``Sum of elements of statistics equal 100`` (votes: Map<User, Vote>) =
    let sum =
        fst (Story.calculateStatistics (DateTime.UtcNow.AddMinutes(10.0)) votes)
        |> Map.toSeq
        |> Seq.map snd
        |> Seq.map (fun r -> r.Percent)
        |> Seq.sum

    equalFloat sum 100.0 1.0


[<Property>]
let ``The result card of statistics has max percent`` (votes: Map<User, Vote>) =
    let stats, card =
        Story.calculateStatistics (DateTime.UtcNow.AddMinutes(10.0)) votes

    let maxVote =
        stats
        |> Map.toSeq
        |> Seq.sortByDescending (fun (_, voteResult) -> voteResult.Percent)
        |> Seq.head

    let maxPercent = (snd maxVote).Percent

    let maxCards =
        stats
        |> Map.toSeq
        |> Seq.filter (fun (_, voteResult) -> equalFloat voteResult.Percent maxPercent 0.1)
        |> Seq.map fst
        |> Seq.toArray

    Array.contains card maxCards

[<Fact>]
let ``Array cards has duplicates`` () =
    let cards = [| % "1"; % "1" |]

    match Story.validateCards cards with
    | Error e -> e = Errors.CardsHasDuplicatesValues
    | Ok _ -> false

[<Fact>]
let ``Array cards hasn't duplicates`` () =
    let cards = [| % "1"; % "2" |]

    match Story.validateCards cards with
    | Error _ -> false
    | Ok _ -> true


[<Fact>]
let ``Array cards shouldn't be empty`` () =
    let cards = [||]

    match Story.validateCards cards with
    | Error e -> e = Errors.CardsHasNotValues
    | Ok _ -> false


[<Fact>]
let ``The closed story for one group has one statistics`` () =

    let stats =
        Story.calculateStatisticsForGroups
            Map.empty
            DateTime.UtcNow
            [| { Id = Guid.NewGuid()
                 Participants = [||] } |]

    test <@ stats.Length = 1 @>

[<Fact>]
let ``The closed story for more one group has statistics without Id`` () =

    let stats =
        Story.calculateStatisticsForGroups
            Map.empty
            DateTime.UtcNow
            [| { Id = Guid.NewGuid()
                 Participants = [||] }
               { Id = Guid.NewGuid()
                 Participants = [||] } |]

    test <@ stats.[0].Id.IsNone @>


[<Fact>]
let ``The closed story for more one group has statistics without Id with stats for all votes and other by participants count`` () =

    let user1 = getUser(), {Card = %"card1"; VotedAt = DateTime.UtcNow }
    let user2 = getUser(), {Card = %"card2"; VotedAt = DateTime.UtcNow }
    let user3 = getUser(), {Card = %"card3"; VotedAt = DateTime.UtcNow }

    let stats =
        Story.calculateStatisticsForGroups
            ([|user1; user2; user3|] |> Map.ofArray)
            DateTime.UtcNow
            [| { Id = Guid.NewGuid()
                 Participants = [| (fst user1).Id |] }
               { Id = Guid.NewGuid()
                 Participants = [| (fst user2).Id; (fst user3).Id |] } |]

    test <@ ((fst stats.[0].Result) |> Map.toArray).Length = 3 @>
    test <@ ((fst stats.[1].Result) |> Map.toArray).Length = 1 @>
    test <@ ((fst stats.[2].Result) |> Map.toArray).Length = 2 @>



[<Property>]
let ``The closed story for more one group has has one more group`` (groups: StatisticsGroup array) =

    let stats =
        Story.calculateStatisticsForGroups Map.empty DateTime.UtcNow groups

    test
        <@ if groups.Length > 1 then
               stats.Length - groups.Length = 1
           else if groups.Length = 1 then
               stats.Length = groups.Length
           else
               stats.Length = 1 @>

[<Fact>]
let ``The statistics id equals groups id`` () =
    let groupsId = [| Guid.NewGuid(); Guid.NewGuid() |]

    let stats =
        Story.calculateStatisticsForGroups
            Map.empty
            DateTime.UtcNow
            (groupsId
             |> Array.map (fun groupId -> { Id = groupId; Participants = [||] }))
        |> Array.toList
        |> List.tail
        |> List.toArray
        |> Array.map (fun s -> s.Id.Value)

    test <@ groupsId = stats @>
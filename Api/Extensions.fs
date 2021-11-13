namespace Api

open Microsoft.AspNetCore.Builder
open System
open System.Security.Claims
open PlanningPoker.Domain.CommonTypes
open FSharp.UMX

[<AutoOpen>]
module Extensions =

    type WebApplication with
        member inline this.MapGet(routerString, handler: Func<'a, 'b>) = this.MapGet(pattern = routerString, handler = handler)
        member inline this.MapGet(routerString, handler: Func<'a, 'b, 'c>) = this.MapGet(pattern = routerString, handler = handler)
        member inline this.MapGet(routerString, handler: Func<'a, 'b, 'c, 'd>) = this.MapGet(pattern = routerString, handler = handler)
        member inline this.MapGet(routerString, handler: Func<'a, 'b, 'c, 'd, 'e>) = this.MapGet(pattern = routerString, handler = handler)
        member inline this.MapGet(routerString, handler: Func<'a, 'b, 'c, 'd, 'e, 'f>) = this.MapGet(pattern = routerString, handler = handler)

        member inline this.MapPost(routerString, handler: Func<'a, 'b>) = this.MapPost(pattern = routerString, handler = handler)
        member inline this.MapPost(routerString, handler: Func<'a, 'b, 'c>) = this.MapPost(pattern = routerString, handler = handler)
        member inline this.MapPost(routerString, handler: Func<'a, 'b, 'c, 'd>) = this.MapPost(pattern = routerString, handler = handler)
        member inline this.MapPost(routerString, handler: Func<'a, 'b, 'c, 'd, 'e>) = this.MapPost(pattern = routerString, handler = handler)
        member inline this.MapPost(routerString, handler: Func<'a, 'b, 'c, 'd, 'e, 'f>) = this.MapPost(pattern = routerString, handler = handler)

        member inline this.MapDelete(routerString, handler: Func<'a, 'b>) = this.MapDelete(pattern = routerString, handler = handler)
        member inline this.MapDelete(routerString, handler: Func<'a, 'b, 'c>) = this.MapDelete(pattern = routerString, handler = handler)
        member inline this.MapDelete(routerString, handler: Func<'a, 'b, 'c, 'd>) = this.MapDelete(pattern = routerString, handler = handler)
        member inline this.MapDelete(routerString, handler: Func<'a, 'b, 'c, 'd, 'e>) = this.MapDelete(pattern = routerString, handler = handler)
        member inline this.MapDelete(routerString, handler: Func<'a, 'b, 'c, 'd, 'e, 'f>) = this.MapDelete(pattern = routerString, handler = handler)

    type ClaimsPrincipal with
        member this.GetDomainUser() : User =
            let id = this.FindFirst(fun c -> c.Type = ClaimTypes.NameIdentifier) |> Option.ofObj |> Option.map(fun v -> v.Value) |> Option.defaultValue "" |> Guid.Parse
            let name = this.FindFirst(fun c -> c.Type = ClaimTypes.GivenName) |> Option.ofObj |> Option.map(fun v -> v.Value)  |> Option.defaultValue ""
            let picture = this.FindFirst("picture") |> Option.ofObj  |> Option.map(fun v -> v.Value)

            {Id = %id; Name = name; Picture = picture }

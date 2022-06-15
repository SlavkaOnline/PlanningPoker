namespace Api

open System
open Microsoft.AspNetCore.Http

module Errors =
    
    type AppErrors =
    | Bug of exn
    | CreateNewAccount of string array
     static member toWebError(appError: AppErrors): IResult =
        match appError with
        | Bug e -> Results.BadRequest("Internal error")
        | CreateNewAccount errs -> Results.BadRequest (String.Join(",", errs))
    


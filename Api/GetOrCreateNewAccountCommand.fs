namespace Api

open System
open Api.Application
open Databases.Models
open Gateway.Views
open Microsoft.AspNetCore.Identity
open Microsoft.EntityFrameworkCore

module Commands = 

    type GetOrCreateNewAccountCommandArgs =
        { Id: Guid
          Email: string
          Name: string
          Picture: string }

    type GetOrCreateNewAccountCommand(userManager: UserManager<Account>, jwtTokenProvider: JwtTokenProvider) =

        interface ICommand<GetOrCreateNewAccountCommandArgs, AuthUser> with
            member _.Execute(arg) =
                task {
                    let! account = userManager.Users.SingleOrDefaultAsync(fun x -> x.Email = arg.Email)

                    match Option.ofObj account with
                    | Some u ->
                        return
                            Ok
                            <| { Token = jwtTokenProvider.CreateToken(u.Id, u.UserName, u.Email, arg.Picture) }
                    | None ->
                        let! newUserResult = userManager.CreateAsync(Account(arg.Id, arg.Name, arg.Email))

                        if newUserResult.Succeeded then
                            return
                                Ok
                                <| { Token = jwtTokenProvider.CreateToken(arg.Id, arg.Name, arg.Email, arg.Picture) }
                        else
                            return
                                newUserResult.Errors
                                |> Seq.map (fun e -> e.Description)
                                |> Array.ofSeq
                                |> Errors.CreateNewAccount
                                |> Error
                }

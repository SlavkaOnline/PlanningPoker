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
          UserName: string
          Name: string
          Picture: string }

    type GetOrCreateNewAccountCommand(userManager: UserManager<Account>, jwtTokenProvider: JwtTokenProvider) =

        interface ICommand<GetOrCreateNewAccountCommandArgs, AuthUser> with
            member _.Execute(arg) =
                task {
                    let! account = userManager.FindByEmailAsync(arg.Email)

                    match Option.ofObj account with
                    | Some acc ->
                        acc.Name <- arg.Name
                        acc.UserName <- arg.UserName
                        let! _ = userManager.UpdateAsync(acc)
                        let! _ = userManager.UpdateNormalizedUserNameAsync(acc)
                        return
                            Ok
                            <| { Token = jwtTokenProvider.CreateToken(acc.Id, acc.UserName, acc.Email, arg.Picture) }
                    | None ->
                        let! newUserResult = userManager.CreateAsync(Account(arg.Id, arg.UserName, arg.Email, arg.Name))

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

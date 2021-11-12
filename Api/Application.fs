module Application

open Microsoft.IdentityModel.Tokens
open System.Text
open System
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Options



type AppSettings = {
     CardsTypes: CardType seq
}
and CardType = {
     Id: string
     Caption: string
     Cards: string array
}

type JwtTokenProvider(configurations: IConfiguration) =

    let sskey = SymmetricSecurityKey(Encoding.UTF8.GetBytes configurations.["Jwt:Key"])

    member _.CreateToken(id: Guid, userName: string, picture: string): string =
        let claims = [|
            Claim(JwtRegisteredClaimNames.NameId, id.ToString())
            Claim(JwtRegisteredClaimNames.GivenName, userName)
            Claim("picture", picture)
        |]

        let credentials = SigningCredentials(sskey, SecurityAlgorithms.HmacSha512Signature)

        let tokenDescriptor = SecurityTokenDescriptor()
        tokenDescriptor.Subject <- ClaimsIdentity(claims)
        tokenDescriptor.Expires <- DateTime.Now.AddDays 31.
        tokenDescriptor.SigningCredentials <- credentials

        let tokenHandler = JwtSecurityTokenHandler()

        let token = tokenHandler.CreateToken(tokenDescriptor)

        tokenHandler.WriteToken token




type CardsTypeProvider(appSettings: IOptions<AppSettings>) =
    let cardsTypes = appSettings.Value.CardsTypes
                     |> Seq.groupBy(fun t -> t.Id)
                     |> Seq.map(fun (key, values) -> key, Seq.head values)
                     |> dict

    member _.GetCardsByTypeId(id: string): string array =
        match cardsTypes.TryGetValue id with
        | true, cards -> cards.Cards
        | _ -> [||]


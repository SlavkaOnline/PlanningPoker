module Application

open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open System.Text
open System
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Options
open Microsoft.OpenApi.Models
open Swashbuckle.AspNetCore.SwaggerGen
open Microsoft.Extensions.DependencyInjection


[<CLIMutable>]
type AppSettings = {
     CardTypes: CardType array
}
and [<CLIMutable>]
CardType = {
     Id: string
     Caption: string
     Cards: string array
}

type JwtTokenProvider(configurations: IConfiguration) =

    let key = SymmetricSecurityKey(Encoding.UTF8.GetBytes configurations.["Jwt:Key"])

    member _.CreateToken(id: Guid, userName: string, picture: string): string =
        let claims = [|
            Claim(JwtRegisteredClaimNames.NameId, id.ToString())
            Claim(JwtRegisteredClaimNames.GivenName, userName)
            Claim("picture", picture)
        |]

        let credentials = SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature)

        let tokenDescriptor = SecurityTokenDescriptor()
        tokenDescriptor.Subject <- ClaimsIdentity(claims)
        tokenDescriptor.Expires <- DateTime.Now.AddDays 31.
        tokenDescriptor.SigningCredentials <- credentials

        let tokenHandler = JwtSecurityTokenHandler()

        let token = tokenHandler.CreateToken(tokenDescriptor)

        tokenHandler.WriteToken token




type CardsTypeProvider(appSettings: IOptions<AppSettings>) =

    let cardsTypes = appSettings.Value.CardTypes
                     |> Seq.groupBy(fun t -> t.Id)
                     |> Seq.map(fun (key, values) -> key, Seq.head values)
                     |> dict

    member this.CardsTypes with get() = cardsTypes.Values

    member _.GetCardsByTypeId(id: string): string array =
        match cardsTypes.TryGetValue id with
        | true, cards -> cards.Cards
        | _ -> [||]

type ConfigureSwaggerOptions() =

    interface IConfigureOptions<SwaggerGenOptions> with
        member this.Configure(options: SwaggerGenOptions) =
            let openApiSecurityScheme = OpenApiSecurityScheme()
            openApiSecurityScheme.Name <- "Authorization"
            openApiSecurityScheme.Type <- SecuritySchemeType.ApiKey
            openApiSecurityScheme.Scheme <- JwtBearerDefaults.AuthenticationScheme
            openApiSecurityScheme.BearerFormat <- "JWT"
            openApiSecurityScheme.In <- ParameterLocation.Header
            openApiSecurityScheme.Description <- "JWT Authorization header using the Bearer scheme."

            let reference = OpenApiReference()
            reference.Type <- ReferenceType.SecurityScheme
            reference.Id <- JwtBearerDefaults.AuthenticationScheme

            let openApiSecurityScheme = OpenApiSecurityScheme()
            openApiSecurityScheme.Reference <- reference

            let openApiSecurityRequirement = OpenApiSecurityRequirement();
            openApiSecurityRequirement.Add(openApiSecurityScheme, Array.empty<string>)

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, openApiSecurityScheme)
            options.AddSecurityRequirement(openApiSecurityRequirement)


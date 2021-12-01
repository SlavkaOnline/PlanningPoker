namespace Api

open System.Security.Claims
open Api.Chat
open GrainInterfaces
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.Options
open Microsoft.IdentityModel.Tokens
open System.Text
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.Google
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.IdentityModel.Protocols.OpenIdConnect
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Diagnostics
open Microsoft.OpenApi.Models
open PlanningPoker.Domain
open System.Net
open Microsoft.AspNetCore.Http.Features
open EventsDeliveryHub
open Microsoft.AspNetCore.Http.Connections
open Gateway.Requests
open Gateway.Views
open Swashbuckle.AspNetCore.SwaggerGen

#nowarn "20"

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Orleans
open Orleans.Hosting
open Orleans.Configuration
open Grains
open Application
open System.Text.Json
open FSharp.UMX

type Program = class end

module Program =
    let exitCode = 0


    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Host.UseOrleans
            (fun siloBuilder ->
                siloBuilder
                    .AddMemoryGrainStorage("InMemory")
                    .AddMemoryGrainStorage("PubSubStore")
                    .AddLogStorageBasedLogConsistencyProvider()
                    .AddSimpleMessageStreamProvider(
                        "SMS",
                        fun (configureStream: SimpleMessageStreamProviderOptions) ->
                            configureStream.FireAndForgetDelivery <- true
                    )
                    .ConfigureApplicationParts(fun parts ->
                        parts
                            .AddApplicationPart(typeof<SessionGrain>.Assembly)
                            .WithReferences()
                        |> ignore)
                    .UseLocalhostClustering()
                |> ignore)


        builder.Services.AddSingleton<JwtTokenProvider>()
        builder.Services.AddSingleton<CardsTypeProvider>()

        builder
            .Services
            .AddAuthentication(fun x ->
                x.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
                x.DefaultChallengeScheme <- OpenIdConnectDefaults.AuthenticationScheme)
            .AddCookie()
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                fun x ->

                    let tokenValidationParameters = TokenValidationParameters()
                    tokenValidationParameters.ValidateIssuerSigningKey <- true

                    tokenValidationParameters.IssuerSigningKey <-
                        SymmetricSecurityKey(Encoding.UTF8.GetBytes builder.Configuration.["Jwt:Key"])

                    tokenValidationParameters.ValidateIssuer <- false
                    tokenValidationParameters.ValidateAudience <- false

                    let events = JwtBearerEvents()

                    events.OnMessageReceived <-
                        (fun context ->
                            let accessToken =
                                context.Request.Query.["access_token"].ToString()

                            let path = context.HttpContext.Request.Path

                            if
                                (String.IsNullOrEmpty(accessToken) |> not)
                                && (path.StartsWithSegments(PathString "/events") || path.StartsWithSegments(PathString "/chat") )
                            then
                                context.Token <- accessToken
                                Task.CompletedTask
                            else
                                Task.CompletedTask)

                    events.OnAuthenticationFailed <-
                        (fun context ->
                            upcast task {
                                       context.Response.StatusCode <- StatusCodes.Status401Unauthorized
                                       context.Response.ContentType <- "application/json; charset=utf-8"

                                       do!
                                           context.Response.WriteAsync(
                                               JsonSerializer.Serialize(
                                                   {| Message = "An error occurred processing your authentication." |}
                                               )
                                           )
                                   })

                    x.TokenValidationParameters <- tokenValidationParameters
                    x.Events <- events
                    x.SaveToken <- true
            )
            .AddOpenIdConnect(
                GoogleDefaults.AuthenticationScheme,
                GoogleDefaults.DisplayName,
                (fun options ->
                    options.SignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                    options.Authority <- "https://accounts.google.com"
                    options.ClientId <- builder.Configuration.["Google:ClientId"]
                    options.ClientSecret <- builder.Configuration.["Google:ClientSecret"]
                    options.CallbackPath <- PathString "/signin-oidc"
                    options.ResponseType <- OpenIdConnectResponseType.CodeIdToken
                    options.GetClaimsFromUserInfoEndpoint <- true
                    options.SaveTokens <- true
                    options.CorrelationCookie.SameSite <- SameSiteMode.Unspecified
                    options.NonceCookie.SameSite <- SameSiteMode.Unspecified
                    options.Scope.Add("email"))
            )

        builder.Services.AddAuthorization
            (fun options ->

                let policy =
                    AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)

                policy.RequireAuthenticatedUser()
                options.DefaultPolicy <- policy.Build())

        builder
            .Services
            .AddSignalR()
            .AddJsonProtocol(fun options ->
                options.PayloadSerializerOptions.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase)

        builder.Services.AddCors
            (fun options ->
                options.AddDefaultPolicy
                    (fun (builder: CorsPolicyBuilder) ->
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                        |> ignore))

        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"))
        builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>()
        builder.Services.AddEndpointsApiExplorer()
        let openApiInfo = OpenApiInfo()
        openApiInfo.Title <- "WebApi"
        openApiInfo.Version <- "v1"
        builder.Services.AddSwaggerGen(fun c -> (c.SwaggerDoc("v1", openApiInfo)))

        let app = builder.Build()

        if app.Environment.IsDevelopment() then
           app.UseDeveloperExceptionPage();
           app.UseSwagger()
           app.UseSwaggerUI(fun c -> c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi v1")) |> ignore
        else
            ()

        let forwardedHeadersOptions = ForwardedHeadersOptions()
        forwardedHeadersOptions.ForwardedHeaders <- ForwardedHeaders.XForwardedFor ||| ForwardedHeaders.XForwardedProto
        app.UseForwardedHeaders(forwardedHeadersOptions)

        app.UseCors()
        app.UseCookiePolicy()
        app.UseAuthentication()
        app.UseAuthorization()

        app.UseExceptionHandler
            (fun x ->
                x.Run
                    (fun context ->
                        upcast task {
                                   let exceptionHandlerPathFeature =
                                       context.Features.Get<IExceptionHandlerPathFeature>()

                                   let err = exceptionHandlerPathFeature.Error

                                   let code, message =
                                       match err with
                                       | :? PlanningPokerDomainException as ex -> HttpStatusCode.BadRequest, ex.Data0
                                       | _ -> HttpStatusCode.InternalServerError, err.Message

                                   context.Response.StatusCode <- int code
                                   context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase <- message
                                   do! context.Response.CompleteAsync()
                               }))

        app.MapHub<DomainEventHub>("/events", (fun options -> options.Transports <- HttpTransportType.WebSockets))
        app.MapHub<ChatHub>("/chat", (fun options -> options.Transports <- HttpTransportType.WebSockets))

        app.MapPost(
            "/api/login",
            fun ([<FromServices>] jwtTokenProvider: JwtTokenProvider) ([<FromBody>] request: AuthUserRequest) ->
                let id = Guid.NewGuid()

                let token =
                    jwtTokenProvider.CreateToken(id, request.Name, "")

                { Token = token }
        )

        app.MapGet(
            "/api/login/google-login",
            fun ([<FromQuery>] returnUrl: string) (linker: LinkGenerator) ->
                let properties = AuthenticationProperties()
                properties.RedirectUri <- linker.GetPathByName("GoogleResponse", {| returnUrl = returnUrl |})
                Results.Challenge(properties, [| GoogleDefaults.AuthenticationScheme |])
        )

        app.MapGet(
                "/api/login/google",
                fun ([<FromServices>] jwtTokenProvider: JwtTokenProvider) (returnUrl: string) ([<FromServices>] ctx: HttpContext) ->
                    task {
                        let! result = ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)

                        if not result.Succeeded then
                            return Results.BadRequest()
                        else
                            let name =
                                $"{result
                                       .Ticket
                                       .Principal
                                       .FindFirst(
                                           ClaimTypes.GivenName
                                       )
                                       .Value} {result
                                                    .Ticket
                                                    .Principal
                                                    .FindFirst(
                                                        ClaimTypes.Surname
                                                    )
                                                    .Value}"

                            let picture =
                                result.Ticket.Principal.FindFirst("picture").Value

                            let id = Guid.NewGuid()

                            let token =
                                jwtTokenProvider.CreateToken(id, name, picture)

                            do! ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                            return Results.Redirect($"{returnUrl}?access_token={token}")
                    }
            )
            .WithName("GoogleResponse")


        app.MapGet(
            "/api/sessions/{id:guid}",
            fun (id: Guid) ([<FromService>] silo: IClusterClient) -> silo.GetGrain<ISessionGrain>(id).GetState()
        ).RequireAuthorization()

        app.MapPost(
            "/api/sessions",
            fun ([<FromBody>] request: CreateSession) ([<FromService>] silo: IClusterClient) ([<FromServices>] ctx: HttpContext) ->
                task {
                    return!
                        (silo.GetGrain<ISessionGrain> <| Guid.NewGuid())
                            .Start(request.Title, ctx.User.GetDomainUser())
                }
        ).RequireAuthorization()

        app.MapPost(
            "/api/sessions/{id:guid}/stories",
            fun (id: Guid) ([<FromBody>] request: CreateStory) ([<FromService>] silo: IClusterClient) ([<FromServices>] ctx: HttpContext) ([<FromServices>] cardsTypeProvider: CardsTypeProvider) ->
                task {
                    return!
                        (silo.GetGrain<ISessionGrain> id)
                            .AddStory(
                                ctx.User.GetDomainUser(),
                                request.Title,
                                if String.IsNullOrEmpty(request.CardsId) then
                                    request.CustomCards
                                else
                                    cardsTypeProvider.GetCardsByTypeId(request.CardsId)
                            )
                }
        ).RequireAuthorization()

        app.MapPost(
            "/api/sessions/{id:guid}/activestory/{storyId:guid}",
            fun (id: Guid) (storyId: Guid) ([<FromService>] silo: IClusterClient) ([<FromServices>] ctx: HttpContext) ->
                task {
                    return!
                        silo
                            .GetGrain<ISessionGrain>(id)
                            .SetActiveStory(ctx.User.GetDomainUser(), storyId, DateTime.UtcNow)
                }
        ).RequireAuthorization()

        app.MapGet(
            "/api/sessions/cards_types",
            fun ([<FromServices>] cardsTypeProvider: CardsTypeProvider) ->
                cardsTypeProvider.CardsTypes
                |> Seq.map (fun c -> { Id = c.Id; Caption = c.Caption })
                |> Seq.toArray
        ).RequireAuthorization()

        app.MapPost(
            "/api/sessions/{id:guid}/groups",
            fun (id: Guid) ([<FromBody>] request: CreateGroup) ([<FromService>] silo: IClusterClient) ([<FromServices>] ctx: HttpContext) ->
                task {
                    return!
                        silo
                            .GetGrain<ISessionGrain>(id)
                            .AddGroup(
                                ctx.User.GetDomainUser(),
                                { Id = % Guid.NewGuid()
                                  Name = request.Name }
                            )
                }
        ).RequireAuthorization()

        app.MapDelete(
            "/api/sessions/{id:guid}/groups/{groupId:guid}",
            fun (id: Guid) (groupId: Guid) ([<FromService>] silo: IClusterClient) ([<FromServices>] ctx: HttpContext) ->
                task {
                    return! silo
                                .GetGrain<ISessionGrain>(id)
                                .RemoveGroup(ctx.User.GetDomainUser(), groupId)
                }
            ).RequireAuthorization()

        app.MapPost(
            "/api/sessions/{id:guid}/groups/{groupId:guid}/participants",
            fun (id: Guid) (groupId: Guid) (request: MoveParticipantToGroup) ([<FromService>] silo: IClusterClient) ([<FromServices>] ctx: HttpContext) ->
                task {
                    return! silo
                                .GetGrain<ISessionGrain>(id)
                                .MoveParticipantToGroup(ctx.User.GetDomainUser(), request.ParticipantId, groupId)
                }
            ).RequireAuthorization()

        app.MapGet(
            "/api/stories/{id:guid}",
            fun (id: Guid) ([<FromService>] silo: IClusterClient) ([<FromServices>] ctx: HttpContext) ->
                task {
                    return! silo
                                .GetGrain<IStoryGrain>(id)
                                .GetState(ctx.User.GetDomainUser())
                }
            ).RequireAuthorization()

        app.MapPost(
            "/api/stories/{id:guid}/vote",
            fun (id: Guid) (request: Vote) ([<FromService>] silo: IClusterClient) ([<FromServices>] ctx: HttpContext) ->
                task {
                    return! silo
                                .GetGrain<IStoryGrain>(id)
                                .Vote(ctx.User.GetDomainUser(), request.Card, DateTime.UtcNow)
                }
            ).RequireAuthorization()

        app.MapDelete(
            "/api/stories/{id:guid}/vote",
            fun (id: Guid)([<FromService>] silo: IClusterClient) ([<FromServices>] ctx: HttpContext) ->
                task {
                    return! silo
                                .GetGrain<IStoryGrain>(id)
                                .RemoveVote(ctx.User.GetDomainUser())
                }
            ).RequireAuthorization()

        app.MapPost(
            "/api/stories/{id:guid}/closed",
            fun (id: Guid) (request: CloseStory) ([<FromService>] silo: IClusterClient) ([<FromServices>] ctx: HttpContext) ->
                task {
                    return! silo
                                .GetGrain<IStoryGrain>(id)
                                .Close(ctx.User.GetDomainUser(), DateTime.UtcNow, request.Groups)
                }
            ).RequireAuthorization()

        app.MapPost(
            "/api/stories/{id:guid}/cleared",
            fun (id: Guid) ([<FromService>] silo: IClusterClient) ([<FromServices>] ctx: HttpContext) ->
                task {
                    return! silo
                                .GetGrain<IStoryGrain>(id)
                                .Clear(ctx.User.GetDomainUser(), DateTime.UtcNow)
                }
            ).RequireAuthorization()

        app.Run()

        exitCode

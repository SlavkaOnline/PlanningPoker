namespace Api

open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open System.Text
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.Google
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.IdentityModel.Protocols.OpenIdConnect
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Diagnostics
open PlanningPoker.Domain
open System.Net
open Microsoft.AspNetCore.Http.Features
open EventsDelivery.EventsDeliveryHub
open Microsoft.AspNetCore.Http.Connections

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


module Program =
    let exitCode = 0

    type Service() = 
        member _.Hello() = "Hello world :)"

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Host.UseOrleans(fun siloBuilder ->
            siloBuilder
                    .AddMemoryGrainStorage("InMemory")
                    .AddMemoryGrainStorage("PubSubStore")
                    .AddLogStorageBasedLogConsistencyProvider()
                    .AddSimpleMessageStreamProvider("SMS", fun (configureStream: SimpleMessageStreamProviderOptions) -> configureStream.FireAndForgetDelivery <- true)
                    .ConfigureApplicationParts(fun parts -> parts.AddApplicationPart(typeof<SessionGrain>.Assembly).WithReferences() |> ignore)
                    .UseLocalhostClustering()
                    |> ignore
        )
        
        
        builder.Services.AddSingleton<JwtTokenProvider>()
        builder.Services.AddSingleton<CardsTypeProvider>()

        builder.Services
            .AddAuthentication(fun x ->
                x.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
                x.DefaultChallengeScheme <- OpenIdConnectDefaults.AuthenticationScheme
            ).AddCookie()
             .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, fun x ->

                let tokenValidationParameters = TokenValidationParameters()
                tokenValidationParameters.ValidateIssuerSigningKey <- true
                tokenValidationParameters.IssuerSigningKey <- SymmetricSecurityKey(Encoding.UTF8.GetBytes builder.Configuration.["Jwt:Key"])
                tokenValidationParameters.ValidateIssuer <- false
                tokenValidationParameters.ValidateAudience <- false

                let events =  JwtBearerEvents()
                events.OnMessageReceived <- (fun context ->
                                let accessToken = context.Request.Query.["access_token"]
                                let path = context.HttpContext.Request.Path

                                if (String.IsNullOrEmpty(accessToken) |> not) && path.StartsWithSegments("/events") then
                                   context.Token <- accessToken
                                   Task.CompletedTask
                                else
                                    Task.CompletedTask
                )

                events.OnAuthenticationFailed <- (fun context ->
                    task {
                        context.Response.StatusCode <- StatusCodes.Status401Unauthorized
                        context.Response.ContentType <- "application/json; charset=utf-8";
                        do! context.Response.WriteAsync(JsonSerializer.Serialize({|Message = "An error occurred processing your authentication."|}))
                    }
                )
                x.TokenValidationParameters <- tokenValidationParameters
                x.Events <- events
                x.SaveToken <- true
         )
         .AddOpenIdConnect(GoogleDefaults.AuthenticationScheme, GoogleDefaults.DisplayName, (fun options ->
            options.SignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme;
            options.Authority <- "https://accounts.google.com";
            options.ClientId <- builder.Configuration["Google:ClientId"];
            options.ClientSecret <- builder.Configuration["Google:ClientSecret"];
            options.CallbackPath <- "/signin-oidc";
            options.ResponseType <- OpenIdConnectResponseType.CodeIdToken;
            options.GetClaimsFromUserInfoEndpoint <- true;
            options.SaveTokens <- true;
            options.CorrelationCookie.SameSite <- SameSiteMode.Unspecified;
            options.NonceCookie.SameSite <- SameSiteMode.Unspecified;
            options.Scope.Add("email");
         ))

        builder.Services.AddAuthorization(fun options ->

            let policy = AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
            policy.RequireAuthenticatedUser()
            options.DefaultPolicy <- policy.Build()
        )

        builder.Services.AddSignalR()
            .AddJsonProtocol(fun options -> options.PayloadSerializerOptions.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase)

        builder.Services.AddCors(fun options ->
            options.AddDefaultPolicy(fun (builder: CorsPolicyBuilder) ->
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    |> ignore
            )
        )

        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"))

        builder.Services.AddSingleton<Service>()

        let app = builder.Build()

        app.UseCors()
        app.UseCookiePolicy()
        app.UseAuthentication()
        app.UseAuthorization()
        app.UseExceptionHandler(fun x -> x.Run(fun context ->
            task {
                let exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>()
                let err = exceptionHandlerPathFeature.Error
                let code, message = 
                    match err  with
                    | :? PlanningPokerDomainException as ex -> HttpStatusCode.BadRequest, ex.Data0
                    | _ -> HttpStatusCode.InternalServerError, err.Message
                context.Response.StatusCode <- int code
                context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase <- message
                do! context.Response.CompleteAsync()
            }
        ))

        app.MapHub<DomainEventHub>("/events", fun options -> options.Transports <- HttpTransportType.WebSockets)

        let handler(service: Service) = service.Hello() 
        app.MapGet("/hello", handler).RequireAuthorization()

        app.Run()

        exitCode

using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EventsDelivery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using PlanningPoker.Domain;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApi.Application;
using WebApi.Infra;


namespace WebApi
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllers();
			services.AddSingleton<JwtTokenProvider>();

			services.AddAuthentication(x =>
				{
					x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
					x.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
				})
				.AddCookie()
				.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme,
					x =>
					{
						x.SaveToken = true;
						x.TokenValidationParameters = new TokenValidationParameters()
						{
							ValidateIssuerSigningKey = true,
							IssuerSigningKey =
								new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"])),
							ValidateIssuer = false,
							ValidateAudience = false
						};
						x.Events = new JwtBearerEvents
						{
							OnMessageReceived = context =>
							{
								var accessToken = context.Request.Query["access_token"];

								// If the request is for our hub...
								var path = context.HttpContext.Request.Path;
								if (!string.IsNullOrEmpty(accessToken) &&
								    (path.StartsWithSegments("/events")))
								{
									// Read the token out of the query string
									context.Token = accessToken;
								}

								return Task.CompletedTask;
							},
							OnAuthenticationFailed = async context =>
							{
								context.Response.StatusCode = StatusCodes.Status401Unauthorized;
								context.Response.ContentType = "application/json; charset=utf-8";
								const string message = "An error occurred processing your authentication.";
								var result = JsonConvert.SerializeObject(new {message});
								await context.Response.WriteAsync(result);
							}
						};
					})
				.AddOpenIdConnect(GoogleDefaults.AuthenticationScheme,
					GoogleDefaults.DisplayName,
					options =>
					{
						options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
						options.Authority = "https://accounts.google.com";
						options.ClientId = Configuration["Google:ClientId"];
						options.ClientSecret = Configuration["Google:ClientSecret"];
						options.CallbackPath = "/signin-oidc";
						options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
						options.GetClaimsFromUserInfoEndpoint = true;
						options.SaveTokens = true;
						options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
						options.NonceCookie.SameSite = SameSiteMode.Unspecified;
						options.Scope.Add("email");
					});

			services.AddAuthorization(options =>
			{
				var policy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme);
				policy.RequireAuthenticatedUser();
				options.DefaultPolicy = policy.Build();
			});

			services.AddSignalR()
				.AddJsonProtocol(options =>
				{
					options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
				});


			services.AddCors(options =>
			{
				options.AddPolicy("AllowAll",
					builder =>
					{
						builder
							.AllowAnyOrigin()
							.AllowAnyMethod()
							.AllowAnyHeader();
					});
			});
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
			services.AddSingleton<CardsTypeProvider>();
			services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
			services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo {Title = "WebApi", Version = "v1"}); });
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi v1"));
			}

			app.UseForwardedHeaders(new ForwardedHeadersOptions
			{
				ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
			});
			//app.UseHttpsRedirection();
			app.UseCors("AllowAll");
			app.UseRouting();

			app.UseCookiePolicy();
			app.UseAuthentication();
			app.UseAuthorization();
			app.UseExceptionHandler(a => a.Run(async context =>
			{
				var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
				var exception = exceptionHandlerPathFeature.Error;
				var (code, message) = exception switch
				{
					PlanningPokerDomainException ex => (code: HttpStatusCode.BadRequest, ex.Data0),
					_ => (code: HttpStatusCode.InternalServerError, exception.Message)
				};
				context.Response.StatusCode = (int) code;
				context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = message;
				await context.Response.CompleteAsync();
			}));

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapHub<EventsDeliveryHub.DomainEventHub>("/events",
					options => { options.Transports = HttpTransportType.WebSockets; });

				endpoints.MapControllers();
			});
		}
	}
}
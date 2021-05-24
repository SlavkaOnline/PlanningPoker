using System;
using System.Text.Json;
using System.Threading.Tasks;
using EventsDelivery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using WebApi.Application;


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
            services.AddRazorPages();
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddDefaultTokenProviders();
            services.AddSingleton<IUserStore<ApplicationUser>, UserStore>();
            services.AddSingleton<IRoleStore<IdentityRole>, RoleStore>();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    options.ClaimsIssuer = "UserName";
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
                    options.Events = new CookieAuthenticationEvents()
                    {
                        OnRedirectToLogin = redirectContext =>
                        {
                            var uri = redirectContext.RedirectUri;
                            UriHelper.FromAbsolute(uri, out var scheme, out var host, out var path, out var query, out var fragment);
                            uri = UriHelper.BuildAbsolute(scheme, host, path);
                            redirectContext.Response.Redirect(uri);
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddSignalR()
                .AddJsonProtocol(options => {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });


            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder
                            .WithOrigins("http://localhost:3000")
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                            .WithExposedHeaders("Location");
                    });
            });

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

            app.UseHttpsRedirection();

            app.UseRouting();

           app.UseCors("AllowAll");
           app.UseAuthentication();
           app.UseAuthorization();
           app.UseEndpoints(endpoints =>
            {

                endpoints.MapHub<EventsDeliveryHub.DomainEventHub>("/events", options =>
                {
                    options.Transports =
                        HttpTransportType.WebSockets |
                        HttpTransportType.LongPolling;
                });

                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}
using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using IdentityService.Data;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace IdentityService;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();
        // builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        // .AddCookie();

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddSingleton<ICorsPolicyService>((container) =>
        {
            var logger = container.GetRequiredService<ILogger<DefaultCorsPolicyService>>();

            return new DefaultCorsPolicyService(logger)
            {
                // AllowedOrigins = { "https://foo", "https://bar" }
                AllowAll = true
            };
        });

        builder.Services
            .AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                if (builder.Environment.IsEnvironment("Docker"))
                {
                    options.IssuerUri = "http://localhost:5000";
                }

                // if (builder.Environment.IsProduction())
                // {
                //     options.IssuerUri = "https://id.trycatchlearn.com";
                // }
            })
            .AddInMemoryIdentityResources(Config.IdentityResources)
            .AddInMemoryApiScopes(Config.ApiScopes)
            // .AddInMemoryClients(Config.Clients(builder.Configuration))
            .AddInMemoryClients(Config.Clients)
            .AddAspNetIdentity<ApplicationUser>()
            .AddProfileService<CustomProfileService>();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        builder.Services.AddAuthentication();
        // builder.Services.AddAuthentication(options =>
        // {
        //     options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //     options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //     options.DefaultForbidScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //     options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //     options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //     options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        // });

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCors("AllowAll");
        app.UseStaticFiles();
        app.UseRouting();

        if (app.Environment.IsProduction())
        {
            app.Use(async (ctx, next) =>
            {
                var serverUrls = ctx.RequestServices.GetRequiredService<IServerUrls>();
                // serverUrls.Origin = serverUrls.Origin = "https://id.trycatchlearn.com";
                await next();
            });
        }


        app.UseIdentityServer();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();
            // .RequireAuthorization();

        return app;
    }
}
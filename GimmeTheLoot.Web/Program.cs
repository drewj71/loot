using GimmeTheLoot.Shared.Authentication;
using GimmeTheLoot.Shared.Services;
using GimmeTheLoot.Web.Components;
using GimmeTheLoot.Web.Data;
using GimmeTheLoot.Web.Services;
using Going.Plaid;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;
using System.Text;

namespace GimmeTheLoot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddMudServices();
            builder.Services.AddMudHybridServices();

            builder.Services.AddScoped<JwtAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
            builder.Services.AddScoped<ITokenStorage, WebTokenStorage>();

            var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

            // Register HttpClient
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl)
            });

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(7027, listenOptions => listenOptions.UseHttps());
            });

            var jwtKey = builder.Configuration["Jwt:Key"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = key
                };
            });
            builder.Services.AddAuthorization();

            builder.Services.AddSingleton<PlaidClient>(_ =>
            {
                return new PlaidClient(
                    Going.Plaid.Environment.Sandbox,
                    clientId: builder.Configuration["Plaid:ClientId"],
                    secret: builder.Configuration["Plaid:Secret"]
                );
            });

            builder.Services.AddScoped<PlaidService>();
            builder.Services.AddScoped<CacheService>();
            builder.Logging.AddConsole();

            var app = builder.Build();

            app.UseAuthentication();
            app.UseAuthorization();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();
            app.MapControllers();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddAdditionalAssemblies(typeof(Shared._Imports).Assembly);

            app.Run();
        }
    }
}

using System.ComponentModel;
using GimmeTheLoot.Shared.Services;
using GimmeTheLoot.Web.Services;
using Microsoft.AspNetCore.Localization;
using MudBlazor.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class StartupExtensions
    {
        public static IServiceCollection AddMudHybridServices(this IServiceCollection services)
        {
            services.AddMudServices();

            //Dependency Injection ProtectedLocalStorageService
            services.AddScoped<ISecureStorage, ProtectedLocalStorageService>();

            services.AddScoped<IPlatformFactor, PlatformFactor>();

            services.AddControllers();
            services.AddLocalization();

            return services;
        }

        public static IApplicationBuilder UseDigiYewMudBlazor(this IApplicationBuilder app)
        {
            var supportedCultures = new[] { "en-US", "zh-TW" };
            app.UseRequestLocalization(supportedCultures);

            ((IEndpointRouteBuilder)app).MapGet("/SetCulture", (HttpContext context, string culture, string redirectUri) =>
            {
                if (culture != null)
                {
                    context.Response.Cookies.Append(
                        CookieRequestCultureProvider.DefaultCookieName,
                        CookieRequestCultureProvider.MakeCookieValue(
                            new RequestCulture(culture)));
                }
                return Results.Redirect(redirectUri);
            });

            ((IEndpointRouteBuilder)app).MapControllers();
            return app;
        }
    }
}
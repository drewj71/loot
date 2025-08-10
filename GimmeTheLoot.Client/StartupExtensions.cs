using System.ComponentModel;
using GimmeTheLoot.Client.Services;
using GimmeTheLoot.Shared.Services;
using MudBlazor.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class StartupExtensions
    {
        public static IServiceCollection AddMudHybridServices(this IServiceCollection services)
        {
            services.AddMudServices();

            services.AddLocalization();

            //DI JwtIdentityService
            services.AddScoped<GimmeTheLoot.Shared.Services.ISecureStorage, SecureStorageService>();
            services.AddSingleton<IPlatformFactor, PlatformFactor>();
            services.AddSingleton<IPreferencesFactor, PreferencesFactor>();

            //Must add this line for Authorization
            services.AddAuthorizationCore();

            return services;
        }
    }
}
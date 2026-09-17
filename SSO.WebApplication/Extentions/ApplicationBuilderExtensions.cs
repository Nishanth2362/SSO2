using Microsoft.AspNetCore.Localization;
using SSO.Application.Configuaration;
using SSO.Common.Constants.Application;
using SSO.Common.Constants.Localization;
using SSO.Infrastructure;
using SSO.WebApplication.Hubs;
using SSO.WebApplication.Middlewares;
using System.Globalization;

namespace SSO.WebApplication.Server.Extensions
{
    internal static class ApplicationBuilderExtensions
    {
        internal static IApplicationBuilder UseExceptionHandling(
            this IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            _ = app.UseExceptionHandler("/Home/Error");

            return app;
        }

        internal static IApplicationBuilder UseForwarding(this IApplicationBuilder app, IConfiguration configuration)
        {
            _ = app.UseCors();
            _ = app.UseForwardedHeaders();

            return app;
        }

        internal static void ConfigureSwagger(this IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
        {
            if (!env.IsDevelopment() && !configuration.GetValue<bool>("SecuritySettings:EnableSwaggerInProduction"))
            {
                return;
            }

            _ = app.UseSwagger();
            _ = app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", typeof(Program).Assembly.GetName().Name);
                options.RoutePrefix = "swagger";
                options.DisplayRequestDuration();
            });
        }

        internal static IApplicationBuilder UseEndpoints(this IApplicationBuilder app)
        {
            return app.UseEndpoints(endpoints =>
            {
                //endpoints.MapRazorPages();
                _ = endpoints.MapControllers();
                //endpoints.MapFallbackToFile("index.html");
                _ = endpoints.MapHub<SignalRHub>(ApplicationConstants.SignalR.HubUrl);
            });
        }

        internal static IApplicationBuilder UseRequestLocalizationByCulture(this IApplicationBuilder app)
        {
            CultureInfo[] supportedCultures = LocalizationConstants.SupportedLanguages.Select(l => new CultureInfo(l.Code)).ToArray();
            _ = app.UseRequestLocalization(options =>
            {
                options.SupportedUICultures = supportedCultures;
                options.SupportedCultures = supportedCultures;
                options.DefaultRequestCulture = new RequestCulture(supportedCultures.First());
                options.ApplyCurrentCultureToResponseHeaders = true;
            });

            _ = app.UseMiddleware<RequestCultureMiddleware>();

            return app;
        }

        internal static async Task<IApplicationBuilder> Initialize(this IApplicationBuilder app, IConfiguration _configuration)
        {
            using IServiceScope serviceScope = app.ApplicationServices.CreateScope();

            IEnumerable<DbInitializers> initializers = serviceScope.ServiceProvider.GetServices<DbInitializers>();

            foreach (DbInitializers initializer in initializers)
            {
                await initializer.Initialize();
            }

            return app;
        }

        private static AppConfiguration GetApplicationSettings(IConfiguration configuration)
        {
            IConfigurationSection applicationSettingsConfiguration = configuration.GetSection(nameof(AppConfiguration));
            return applicationSettingsConfiguration.Get<AppConfiguration>()!;
        }
    }
}

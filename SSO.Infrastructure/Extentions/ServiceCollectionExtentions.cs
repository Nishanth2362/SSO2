using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Server;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Serialization.Serializers;
using SSO.Application.Interfaces.Services;
using SSO.Application.Interfaces.Services.Features;
using SSO.Application.Interfaces.Services.Storage;
using SSO.Application.Interfaces.Services.Storage.Provider;
using SSO.Application.Serialization.JsonConverters;
using SSO.Application.Serialization.Options;
using SSO.Application.Serialization.Serializers;
using SSO.Domain.Entities;
using SSO.Infrastructure.Contexts;
using SSO.Infrastructure.Services;
using SSO.Infrastructure.Services.Features;
using SSO.Infrastructure.Services.OpenXml;
using SSO.Infrastructure.Services.Repositories;
using SSO.Infrastructure.Services.Storage;
using SSO.Infrastructure.Services.Storage.Provider;
using SSO.Shared.Wrapper;
using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace SSO.Infrastructure.Extentions
{
    public static class ServiceCollectionExtentions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            return services
            .AddTransient(typeof(IRepositoryAsync<,>), typeof(RepositoryAsync<,>))
                .AddTransient(typeof(IUnitOfWork<>), typeof(UnitOfWork<>))
                .AddScoped<IRoleService, RoleService>()
                .AddScoped<IClientService,ClientService>()
                .AddScoped<IScopeServices,ScopeServices>()
                .AddScoped<IDataTableService, DataTableService>()
                .AddScoped<IAccessControlService, AccessControlService>()
                .AddTransient(typeof(IDapperRepository), typeof(DapperRepository))
                .AddScoped<DbInitializers>();
        }
        public static IServiceCollection AddServerStorage(this IServiceCollection services)
        {
            return AddServerStorage(services, null!);
        }

        public static IServiceCollection AddServerStorage(this IServiceCollection services, Action<SystemTextJsonOptions> configure)
        {
            return services
                .AddScoped<IJsonSerializer, SystemTextJsonSerializer>()
                .AddScoped<IStorageProvider, ServerStorageProvider>()
                .AddScoped<IServerStorageService, ServerStorageService>()
                .AddScoped<ISyncServerStorageService, ServerStorageService>()
                .Configure<SystemTextJsonOptions>(configureOptions =>
                {
                    configure?.Invoke(configureOptions);
                    if (!configureOptions.JsonSerializerOptions.Converters.Any(c => c.GetType() == typeof(TimespanJsonConverter)))
                    {
                        configureOptions.JsonSerializerOptions.Converters.Add(new TimespanJsonConverter());
                    }
                });
        }
        public static IServiceCollection AddInfrastructureExtentions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ITemplateRepository, TemplateRepository>();
            services.AddScoped<ITemplateResolver, TemplateResolver>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IExcelService, ExcelService>();
            services.AddScoped<IInvoiceJob, SSO.Infrastructure.Services.Jobs.InvoiceJob>();
            services.AddScoped<ISystemMaintenanceJob, SSO.Infrastructure.Services.Jobs.SystemMaintenanceJob>();
            // Add your infrastructure-related service registrations here
            services.AddDataProtection()
                    .PersistKeysToDbContext<ApplicationDbContext>()
                    .SetApplicationName("Enterprise.SSO")
                    //.ProtectKeysWithDpapiNG()
                    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

            var issuer = configuration.GetSection("AppConfiguration")["Issuer"] ?? "https://localhost:7017";

            services.AddOpenIddict()
                .AddCore(options =>
                    {
                        options.UseEntityFrameworkCore()
                                .UseDbContext<ApplicationDbContext>()
                                .ReplaceDefaultEntities<ApplicationClient, ApplicationAuthorization, ApplicationScope, ApplicationToken, Guid>();
                    }).AddServer(options =>
                    {
                        // =========================
                        // ENDPOINTS
                        // =========================
                        options.SetAuthorizationEndpointUris("/connect/authorize")
                               .SetTokenEndpointUris("/connect/token")
                               .SetRevocationEndpointUris("/connect/revoke")
                               .SetIntrospectionEndpointUris("/connect/introspect")
                               .SetEndSessionEndpointUris("/connect/endsession")
                               .SetDeviceAuthorizationEndpointUris("/connect/device")
                               .SetEndUserVerificationEndpointUris("/connect/verify")
                               //.SetVerificationEndpointUris("/connect/verify")     // device flow page
                               .SetUserInfoEndpointUris("/connect/userinfo");

                        // =========================
                        // FLOWS (ALL CLIENT TYPES)
                        // =========================
                        options.AllowAuthorizationCodeFlow()      // SPA / WEB / NATIVE
                               .AllowRefreshTokenFlow()
                               .AllowClientCredentialsFlow()      // MACHINE
                               .AllowPasswordFlow()               // Legacy / trusted apps
                               .AllowDeviceAuthorizationFlow()    // TV / IoT
                               .AllowTokenExchangeFlow();         // Delegation (On-Behalf-Of)

                        // =========================
                        // TOKEN FORMATS
                        // =========================
                        options.UseReferenceAccessTokens();       // enterprise revocation support
                        options.UseReferenceRefreshTokens();

                        // =========================
                        // SECURITY HARDENING
                        // =========================
                        options.RequireProofKeyForCodeExchange(); // PKCE for SPA / Native

                        // Encryption & signing
                        var certPath = Path.Combine(AppContext.BaseDirectory, "Cert", configuration["Certificate:Path"]!);
                        var certPassword = configuration.GetValue<string>("Certificate:Password")!;
                        if (!string.IsNullOrWhiteSpace(certPath) && File.Exists(certPath) && !string.IsNullOrWhiteSpace(certPassword))
                        {
                            var cert = X509CertificateLoader.LoadPkcs12FromFile(
                            certPath,
                            certPassword,
                            X509KeyStorageFlags.MachineKeySet |
                            X509KeyStorageFlags.Exportable |
                            X509KeyStorageFlags.EphemeralKeySet);

                            options.AddEncryptionCertificate(cert);
                            options.AddSigningCertificate(cert);

                        }
                        else
                        {
                            options.AddDevelopmentEncryptionCertificate()
                                   .AddDevelopmentSigningCertificate();
                        }

                        // =========================
                        // SCOPES (GLOBAL)
                        // =========================
                        options.RegisterScopes(
                            "openid",
                            "profile",
                            "email",
                            "roles",
                            "offline_access",
                            "api"
                        );

                        // =========================
                        // ASP.NET CORE PIPELINE
                        // =========================
                        options.UseAspNetCore()
                               .EnableAuthorizationEndpointPassthrough()
                               .EnableEndSessionEndpointPassthrough()
                               .EnableTokenEndpointPassthrough()
                               .EnableUserInfoEndpointPassthrough()
                               .EnableEndUserVerificationEndpointPassthrough();

                    }).AddValidation(options =>
                    {
                        options.SetIssuer(issuer);
                        //options.AddAudiences("resource_server_api");
                        options.UseLocalServer();
                        options.UseSystemNetHttp();

                        // ⭐ This is from OpenIddict.Validation.AspNetCore
                        options.UseAspNetCore();
                    });
            return services;
        }
    }
}

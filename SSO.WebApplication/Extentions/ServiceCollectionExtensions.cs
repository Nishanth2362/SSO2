using Hangfire;
using Hangfire.MySql;
using Hangfire.Oracle.Core;
using Hangfire.PostgreSql;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Newtonsoft.Json;
using Serilog;
using SSO.Application.Configuaration;
using SSO.Application.Interfaces.Serialization.Options;
using SSO.Application.Interfaces.Serialization.Serializers;
using SSO.Application.Interfaces.Serialization.Settings;
using SSO.Application.Interfaces.Services;
using SSO.Application.Serialization.JsonConverters;
using SSO.Application.Serialization.Options;
using SSO.Application.Serialization.Serializers;
using SSO.Application.Serialization.Settings;
using SSO.Common.Constants.Application;
using SSO.Common.Constants.Localization;
using SSO.Common.Constants.Permission;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Infrastructure.Contexts;
using SSO.Infrastructure.Services;
using SSO.WebApplication.Localization;
using SSO.WebApplication.Managers.Preferences;
using SSO.WebApplication.Services;
using SSO.WebApplication.Settings;
using System.Data;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using System.Transactions;
namespace SSO.WebApplication.Server.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        internal static async Task<IStringLocalizer> GetRegisteredServerLocalizerAsync<T>(this IServiceCollection services) where T : class
        {
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            await SetCultureFromServerPreferenceAsync(serviceProvider);
            IStringLocalizer<T>? localizer = serviceProvider.GetService<IStringLocalizer<T>>();
            await serviceProvider.DisposeAsync();
            return localizer!;
        }

        internal static IServiceCollection AddForwarding(this IServiceCollection services, IConfiguration configuration)
        {
            IConfigurationSection applicationSettingsConfiguration = configuration.GetSection(nameof(AppConfiguration));
            AppConfiguration config = applicationSettingsConfiguration.Get<AppConfiguration>();
            if (config.BehindSSLProxy)
            {
                _ = services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    if (!string.IsNullOrWhiteSpace(config.ProxyIP))
                    {
                        string ipCheck = config.ProxyIP;
                        if (IPAddress.TryParse(ipCheck, out IPAddress? proxyIP))
                        {
                            options.KnownProxies.Add(proxyIP);
                        }
                        else
                        {
                            Log.Logger.Warning("Invalid Proxy IP of {IpCheck}, Not Loaded", ipCheck);
                        }
                    }
                });

                _ = services.AddCors(options =>
                {
                    var configuredOrigins = configuration.GetSection("SecuritySettings:AllowedCorsOrigins").Get<string[]>() ?? Array.Empty<string>();
                    var allowedOrigins = configuredOrigins
                        .Concat(GetApplicationOrigins(config.ApplicationUrl))
                        .Where(origin => !string.IsNullOrWhiteSpace(origin))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    options.AddDefaultPolicy(
                        builder =>
                        {
                            if (allowedOrigins.Length == 0)
                            {
                                return;
                            }

                            _ = builder
                                .WithOrigins(allowedOrigins)
                                .AllowCredentials()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                        });
                });
            }

            return services;
        }

        private static async Task SetCultureFromServerPreferenceAsync(IServiceProvider serviceProvider)
        {
            ServerPreferenceManager? storageService = serviceProvider.GetService<ServerPreferenceManager>();
            if (storageService != null)
            {
                // TODO - should implement ServerStorageProvider to work correctly!
                CultureInfo culture = await storageService.GetPreference() is ServerPreference preference
                    ? (new(preference.LanguageCode))
                    : (new(LocalizationConstants.SupportedLanguages.FirstOrDefault()?.Code ?? "en-US"));
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
        }

        internal static IServiceCollection AddServerLocalization(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(IStringLocalizer<>), typeof(ServerLocalizer<>));
            return services;
        }

        internal static AppConfiguration GetApplicationSettings(
           this IServiceCollection services,
           IConfiguration configuration)
        {
            IConfigurationSection applicationSettingsConfiguration = configuration.GetSection(nameof(AppConfiguration));
            _ = services.Configure<AppConfiguration>(applicationSettingsConfiguration);
            return applicationSettingsConfiguration.Get<AppConfiguration>();
        }


        public static void RegisterSwagger(this IServiceCollection services, IStringLocalizer<ServerCommonResources>? localizer = null)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Visitor Management API",
                    Description = "Visitor and Vehicle Controll",
                    TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "Amoebatronix Private Limited",
                        Email = "support@amoebatronix.com",
                        Url = new Uri("https://www.amoebatronix.com/"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    c.IncludeXmlComments(xmlPath);
            });
        }




        internal static IServiceCollection AddDatabase(
     this IServiceCollection services,
     IConfiguration configuration)
        {
            var provider = configuration.GetValue<string>("DbSettings:Provider")?.ToLower();
            var conn = configuration.GetValue<string>("DbSettings:DefaultConnection")!
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");


            Console.WriteLine($"Detected Connection String: {conn}");

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                var c = conn.ToLowerInvariant();

                // ================= ORACLE =================
                if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.Oracle.ToLower()))
                {
                    Console.WriteLine("Database Provider: ORACLE");

                    options.UseOracle(conn, o =>
                        o.UseOracleSQLCompatibility(
                            OracleSQLCompatibility.DatabaseVersion23));
                    return;
                }

                // ================= POSTGRES =================
                if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.PostgreSql.ToLower()))
                {
                    Console.WriteLine("Database Provider: POSTGRESQL");
                    options.UseNpgsql(conn);
                    return;
                }

                // ================= MYSQL =================
                if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.Mysql.ToLower()))
                {
                    Console.WriteLine("Database Provider: MYSQL/MARIADB");
                    options.UseMySQL(conn);
                    return;
                }

                // ================= SQL SERVER =================
                if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.SqlServer.ToLower()))
                {
                    Console.WriteLine("Database Provider: SQL SERVER");
                    options.UseSqlServer(conn);
                    return;
                }

                throw new NotSupportedException("Database provider not recognized.");
            });

            return services;
        }
        
        internal static IServiceCollection ConfigureHangefire(this IServiceCollection services, IConfiguration configuration)
        {
            // AUTO-CONFIGURE HANGFIRE TO USE THE EXACT SAME DATABASE
            var provider = configuration.GetValue<string>("DbSettings:Provider")?.ToLower();
            var conn = configuration.GetValue<string>("DbSettings:DefaultConnection")!
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");


            if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.SqlServer.ToLower()))
            {
                // Sql Server (works with both Pomelo and official provider)
                services.AddHangfire(x => x.UseStorage(new SqlServerStorage(conn, new SqlServerStorageOptions()
                {
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    DashboardJobListLimit = 50000,
                    TransactionTimeout = TimeSpan.FromMinutes(1),
                    SchemaName = "Hangfire"
                })));
            }           
            else if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.Mysql.ToLower()))
            {
                // MySQL / MariaDB (works with both Pomelo and official provider)
                services.AddHangfire(x => x.UseStorage(new MySqlStorage(conn, new MySqlStorageOptions()
                {
                    TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    DashboardJobListLimit = 50000,
                    TransactionTimeout = TimeSpan.FromMinutes(1),
                    TablesPrefix = "Hangfire"
                })));
            }
            else if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.Oracle.ToLower()))
            {
                // Oracle
                services.AddHangfire(x => x.UseStorage(new OracleStorage(conn, new OracleStorageOptions()
                {
                    TransactionIsolationLevel = System.Data.IsolationLevel.ReadUncommitted,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    DashboardJobListLimit = 50000,
                    TransactionTimeout = TimeSpan.FromMinutes(1),
                    SchemaName = "Hangfire"
                })));
            }
            else if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.PostgreSql.ToLower()))
            {
                _ = services.AddHangfire(x => x.UseStorage(new PostgreSqlStorage(conn, new PostgreSqlStorageOptions()
                {
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    SchemaName = "Hangfire",
                    UseNativeDatabaseTransactions = true,
                    AllowUnsafeValues = false,
                    DeleteExpiredBatchSize = 1000,
                    DistributedLockTimeout = TimeSpan.FromMinutes(1),
                    EnableLongPolling = true,
                    EnableTransactionScopeEnlistment = true,
                    InvisibilityTimeout = TimeSpan.FromMinutes(5),
                    TransactionSynchronisationTimeout = TimeSpan.FromMinutes(1),
                    UseSlidingInvisibilityTimeout = true
                })));

            }
            else
            {
               
            }

            services.AddHangfireServer(x =>
            {
                x.ServerName = "Visitor Management";
                //x.TimeZoneResolver = new DefaultTimeZoneResolver();
            });
            return services;
        }
        internal static IServiceCollection AddCurrentUserService(this IServiceCollection services)
        {
            _ = services.AddHttpContextAccessor();
            _ = services.AddScoped<ICurrentUserService, CurrentUserService>();
            return services;
        }

        internal static IServiceCollection AddSecurityHardening(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var path = httpContext.Request.Path.Value ?? string.Empty;
                    var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var isSensitivePath =
                        path.StartsWith("/Login", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith("/connect", StringComparison.OrdinalIgnoreCase);

                    return isSensitivePath
                        ? RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: $"{remoteIp}:{path}",
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 20,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0,
                                AutoReplenishment = true
                            })
                        : RateLimitPartition.GetNoLimiter($"{remoteIp}:default");
                });
            });

            return services;
        }
        internal static IServiceCollection AddSerialization(this IServiceCollection services)
        {
            _ = services
                .AddScoped<IJsonSerializerOptions, SystemTextJsonOptions>()
                .Configure<SystemTextJsonOptions>(configureOptions =>
                {
                    if (!configureOptions.JsonSerializerOptions.Converters.Any(c => c.GetType() == typeof(TimespanJsonConverter)))
                    {
                        configureOptions.JsonSerializerOptions.Converters.Add(new TimespanJsonConverter());
                    }
                });
            _ = services.AddScoped<IJsonSerializerSettings, NewtonsoftJsonSettings>();

            _ = services.AddScoped<IJsonSerializer, SystemTextJsonSerializer>(); // you can change it
            return services;
        }

        internal static IServiceCollection AddIdentity(this IServiceCollection services)
        {
            _ = services
                .AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequireUppercase = true;
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = true;

                    // Lockout settings.
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddClaimsPrincipalFactory<ApplicationClaimsPrincipalFactory>();

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Name = ".SSO.Identity";
                options.Cookie.IsEssential = true;

                options.LoginPath = "/Login";
                options.LogoutPath = "/Login/Logout";
                options.AccessDeniedPath = "/Login/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Cookie.MaxAge = options.ExpireTimeSpan;
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    if (IsApiOrAjaxRequest(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (IsApiOrAjaxRequest(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });

            return services;
        }

        internal static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            _ = services.AddTransient<IDateTimeService, SystemDateTimeService>();
            _ = services.Configure<MailConfiguration>(configuration.GetSection("MailConfiguration"));
            _ = services.AddTransient<IMailService, SMTPMailService>();
            return services;
        }

        internal static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            _ = services.AddTransient<IUploadService, UploadService>();
            _ = services.AddTransient<IAuditService, AuditService>();
            _ = services.AddTransient<IEmailTemplateService, EmailTemplateService>();
            return services;
        }

        internal static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services, AppConfiguration config)
        {
            byte[] key = Encoding.UTF8.GetBytes(config.Secret);
            const string applicationAuthScheme = "ApplicationAuth";
            const string openIddictValidationScheme = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            services
                .AddAuthentication(authentication =>
                {
                    authentication.DefaultScheme = applicationAuthScheme;
                    authentication.DefaultAuthenticateScheme = applicationAuthScheme;
                    authentication.DefaultChallengeScheme = applicationAuthScheme;
                    authentication.DefaultForbidScheme = applicationAuthScheme;
                })
                .AddPolicyScheme(applicationAuthScheme, applicationAuthScheme, options =>
                {
                    options.ForwardDefaultSelector = context => SelectAuthenticationScheme(context.Request);
                })
                .AddJwtBearer(bearer =>
                {
#if DEBUG
                    bearer.RequireHttpsMetadata = false;
#else
                    bearer.RequireHttpsMetadata = true;
#endif
                    bearer.SaveToken = true;
                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = config.Issuer,
                        ValidateAudience = true,
                        ValidAudience = config.Audience,
                        RoleClaimType = ClaimTypes.Role,
                        ClockSkew = TimeSpan.Zero
                    };

                    IStringLocalizer localizer = GetRegisteredServerLocalizerAsync<ServerCommonResources>(services).GetAwaiter().GetResult();

                    bearer.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            Microsoft.Extensions.Primitives.StringValues accessToken = context.Request.Query["access_token"];

                            // If the request is for our hub...
                            PathString path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments(ApplicationConstants.SignalR.HubUrl))
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = c =>
                        {
                            if (c.Exception is SecurityTokenExpiredException)
                            {
                                c.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                c.Response.ContentType = "application/json";
                                string result = JsonConvert.SerializeObject(Result.Fail(localizer["The Token is expired."]));
                                return c.Response.WriteAsync(result);
                            }
                            else
                            {
                                c.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                c.Response.ContentType = "application/json";
                                var result = JsonConvert.SerializeObject(Result.Fail(localizer["An unhandled error has occurred."]));
                                return c.Response.WriteAsync(result);
                            }
                        },
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            if (!context.Response.HasStarted)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.ContentType = "application/json";
                                string result = JsonConvert.SerializeObject(Result.Fail(localizer["You are not Authorized."]));
                                return context.Response.WriteAsync(result);
                            }

                            return Task.CompletedTask;
                        },
                        OnForbidden = context =>
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            context.Response.ContentType = "application/json";
                            string result = JsonConvert.SerializeObject(Result.Fail(localizer["You are not authorized to access this resource."]));
                            return context.Response.WriteAsync(result);
                        },
                    };
                });
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                options.FallbackPolicy = options.DefaultPolicy;

                foreach (FieldInfo? prop in typeof(Permissions).GetNestedTypes().SelectMany(c => c.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)))
                {
                    object? propertyValue = prop.GetValue(null);
                    if (propertyValue is not null)
                    {
                        options.AddPolicy(propertyValue.ToString()!, policy =>
                        {
                            policy.RequireAuthenticatedUser();
                            policy.RequireClaim(ApplicationClaimTypes.Permission, propertyValue.ToString()!);
                        });
                    }
                }
            });
            return services;
        }

        private static IEnumerable<string> GetApplicationOrigins(string? applicationUrl)
        {
            if (!Uri.TryCreate(applicationUrl, UriKind.Absolute, out var uri))
            {
                yield break;
            }

            yield return uri.GetLeftPart(UriPartial.Authority);
        }

        private static bool IsApiOrAjaxRequest(HttpRequest request)
        {
            if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
                request.Path.StartsWithSegments("/v1", StringComparison.OrdinalIgnoreCase) ||
                request.Path.StartsWithSegments(ApplicationConstants.SignalR.HubUrl, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (request.Path.StartsWithSegments("/connect", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Path.Equals("/connect/authorize", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }

            if (request.Headers.TryGetValue("X-Requested-With", out var requestedWith) &&
                string.Equals(requestedWith.ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return request.Headers.Accept.Any(h => h.Contains("application/json", StringComparison.OrdinalIgnoreCase));
        }

        private static string SelectAuthenticationScheme(HttpRequest request)
        {
            if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
                request.Path.StartsWithSegments(ApplicationConstants.SignalR.HubUrl, StringComparison.OrdinalIgnoreCase))
            {
                string? token = GetAccessToken(request);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    return token.Contains('.', StringComparison.Ordinal)
                        ? JwtBearerDefaults.AuthenticationScheme
                        : OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                }
            }

            return IdentityConstants.ApplicationScheme;
        }

        private static string? GetAccessToken(HttpRequest request)
        {
            string? authorization = null;
            if (request.Headers.TryGetValue("Authorization", out var authorizationValues))
            {
                authorization = authorizationValues.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(authorization) &&
                authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authorization["Bearer ".Length..].Trim();
            }

            string? accessToken = request.Query["access_token"].FirstOrDefault();
            return string.IsNullOrWhiteSpace(accessToken) ? null : accessToken;
        }
    }
}

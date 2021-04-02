/*
 *
 * (c) Copyright Talegen, LLC.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

namespace Bastille.Id.Api
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Bastille.Id.Api.Common.Configuration;
    using Bastille.Id.Api.Common.Notifications;
    using Bastille.Id.Api.Properties;
    using Bastille.Id.Core;
    using Bastille.Id.Core.Configuration;
    using Bastille.Id.Core.Data;
    using Bastille.Id.Core.Data.Entities;
    using Bastille.Id.Core.Security;
    using IdentityModel.AspNetCore.OAuth2Introspection;
    using IdentityServer4.AccessTokenValidation;
    using IdentityServer4.EntityFramework.DbContexts;
    using IdentityServer4.EntityFramework.Options;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Authorization;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Newtonsoft.Json;
    using Serilog;
    using Talegen.AspNetCore.Web.Bindings;
    using Talegen.AspNetCore.Web.Configuration;
    using Talegen.Common.Core.Errors;
    using Talegen.Common.Core.Extensions;
    using Talegen.Common.Messaging;
    using Vasont.AspnetCore.RedisClient;

    /// <summary>
    /// This class contains the main startup routines for the web application.
    /// </summary>
    public class Startup
    {
        #region Private Fields

        /// <summary>
        /// Defines the default cors policy name.
        /// </summary>
        private const string DefaultCorsPolicy = "default";

        /// <summary>
        /// Defines the notifications hub route.
        /// </summary>
        private const string NotificationHubRoute = "/hubs/notificationHub";

        /// <summary>
        /// Contains an instance of the application settings.
        /// </summary>
        private ApplicationSettings applicationSettings;

        /// <summary>
        /// Contains the identity server database connection string.
        /// </summary>
        private string databaseConnectionString;

        /// <summary>
        /// Contains the redis cache connection string.
        /// </summary>
        private string redisConnectionString;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup" /> class.
        /// </summary>
        /// <param name="configuration">Contains an <see cref="IConfiguration" /> implementation.</param>
        /// <param name="environment">Contains an <see cref="IWebHostEnvironment" /> implementation.</param>
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            this.Configuration = configuration;
            this.Environment = environment;
        }

        #region Public Properties

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Gets the hosting environment.
        /// </summary>
        public IWebHostEnvironment Environment { get; }

        /// <summary>
        /// Gets or sets the application settings.
        /// </summary>
        public ApplicationSettings Settings
        {
            get
            {
                if (this.applicationSettings == null)
                {
                    var settingsSection = this.Configuration.GetSection(nameof(ApplicationSettings));
                    this.applicationSettings = settingsSection?.Get<ApplicationSettings>() ?? new ApplicationSettings();
                }

                return this.applicationSettings;
            }
            set
            {
                this.applicationSettings = value;
            }
        }

        /// <summary>
        /// Gets or sets the identity server database connection string.
        /// </summary>
        public string DatabaseConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(this.databaseConnectionString))
                {
                    this.databaseConnectionString = this.Configuration.GetConnectionString("DefaultConnection");
                }

                return this.databaseConnectionString;
            }

            set
            {
                this.databaseConnectionString = value;
            }
        }

        /// <summary>
        /// Gets or sets the redis cache server connection string.
        /// </summary>
        public string RedisConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(this.redisConnectionString))
                {
                    this.redisConnectionString = this.Configuration.GetConnectionString("RedisConnection");
                }

                return this.redisConnectionString;
            }

            set
            {
                this.redisConnectionString = value;
            }
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">Contains the service collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            bool development = this.Environment.IsDevelopment();

            this.InitializeSettings(services);

            ConfigureServiceStartupSettings(this.Settings, this.Environment.ContentRootPath, this.RedisConnectionString);

            // setup service injection
            ConfigureServiceInjectionObjects(services, this.Settings, this.DatabaseConnectionString);

            // setup server
            ConfigureServiceWebServer(services, this.Settings, this.RedisConnectionString, development);

            // setup signal-R notifications
            ConfigureServiceSignalR(services, this.Settings, this.RedisConnectionString, development);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            bool development = env.IsDevelopment();

            if (development)
            {
                Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
                app.UseDeveloperExceptionPage();
            }

            // this will exclude localhost already, otherwise add HSTS header
            app.UseHsts();

            if (this.Settings.Advanced.ForceSsl)
            {
                app.UseHttpsRedirection();
            }

            app.UseCors(DefaultCorsPolicy);

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // setup default endpoint route
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                // add notification hub route endpoint
                endpoints.MapHub<NotificationHub>(NotificationHubRoute)
                    .RequireAuthorization();
            });
        }

        #endregion

        #region Private Methods

        #region Configure Service Methods

        /// <summary>
        /// Initializes the settings.
        /// </summary>
        /// <param name="services">The services.</param>
        private void InitializeSettings(IServiceCollection services)
        {
            services.Configure<ApplicationSettings>(this.Configuration.GetSection(nameof(ApplicationSettings)));

            // configure telemetry settings
            ConfigureServiceTelemetry(services, this.Settings);

            // show connection strings if diagnosing
            if (this.Settings.Advanced.ShowDiagnostics || this.Environment.IsDevelopment())
            {
                // show PII in logging - by default useful PII is stripped out
                Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
                Log.Debug("Connection String: {0}\r\nRedis String: {1}", this.DatabaseConnectionString, this.RedisConnectionString);
            }
        }

        /// <summary>
        /// Configures the telemetry for the application.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="settings">The settings.</param>
        private static void ConfigureServiceTelemetry(IServiceCollection services, ApplicationSettings settings)
        {
            services.AddSingleton<ITelemetryInitializer, InstrumentationConfigInitializer>();
            services.AddApplicationInsightsTelemetry(settings.ApplicationInsights.InstrumentationKey);
        }

        /// <summary>
        /// Configures the startup settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="contentRootPath">Content root path.</param>
        /// <param name="redisConnectionString">Contains the redis connection string.</param>
        private static void ConfigureServiceStartupSettings(ApplicationSettings settings, string contentRootPath, string redisConnectionString)
        {
            // setup working folder if none specified
            if (string.IsNullOrWhiteSpace(settings.Storage.RootPath))
            {
                // working folder will reside in the main application folder by default.
                settings.Storage.RootPath = Path.Combine(contentRootPath, settings.Advanced.AppDataSubFolderName);
            }

            // if thread settings config has a value...
            if (settings.Advanced.MinimumCompletionPortThreads > 0)
            {
                // setup threading
                ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
                ThreadPool.SetMinThreads(workerThreads * 2, completionPortThreads > settings.Advanced.MinimumCompletionPortThreads ? completionPortThreads : settings.Advanced.MinimumCompletionPortThreads);
            }

            if (!string.IsNullOrWhiteSpace(redisConnectionString))
            {
                // setup Redis
                RedisManager.Initialize(redisConnectionString);
            }
        }

        /// <summary>
        /// Configures the injection objects.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="connectionString">The connection string.</param>
        private static void ConfigureServiceInjectionObjects(IServiceCollection services, ApplicationSettings settings, string connectionString)
        {
            // define the direct inject for the Application Database Context
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            services.AddDbContext<ConfigurationDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            services.AddScoped<ConfigurationStoreOptions>();

            // add injections
            services.AddTransient<IErrorManager, ErrorManager>();

            // add User Manager related objects into DI configuration
            services.AddTransient<IUserStore<User>, UserStore<User, Role, ApplicationDbContext, Guid>>();
            services.AddTransient<IRoleStore<Role>, RoleStore<Role, ApplicationDbContext, Guid>>();
            services.AddTransient<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddTransient<ILookupNormalizer, UpperInvariantLookupNormalizer>();
            services.AddTransient<IdentityErrorDescriber>();
            var identityBuilder = new IdentityBuilder(typeof(User), typeof(Role), services);
            identityBuilder.AddTokenProvider("Default", typeof(DataProtectorTokenProvider<User>));
            services.AddTransient<UserManager<User>>();

            // add background messaging
            services.AddMessaging(settings.Messaging);

            // setup transient for application settings.
            services.AddTransient<ApplicationContext<ApplicationSettings>>();

            // implement signalR injections
            services.AddSingleton<IUserIdProvider, UserIdProvider>();
            services.AddSingleton<IConnectionManager, ConnectionManager>();
            services.AddSingleton<INotificationHelper, NotificationHelper>();
        }

        /// <summary>
        /// Configures the web server.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="redisConnectionString">The redis connection string.</param>
        /// <param name="development">Contains a value indicating whether the application is in a dev environment.</param>
        private static void ConfigureServiceWebServer(IServiceCollection services, ApplicationSettings settings, string redisConnectionString, bool development)
        {
            // setup controllers
            services.AddControllers(options =>
            {
                if (settings.Advanced.ForceSsl)
                {
                    options.Filters.Add(new RequireHttpsAttribute());
                }

                // add authorization filter to require token have a scope that includes the API resource identifier.
                var policy = ScopePolicy.Create(SecurityDefaults.IdentityApiResource);
                options.Filters.Add(new AuthorizeFilter(policy));

                // add our custom bindings overrides to fix Guid binding B.S. etc.
                options.AddBindingOverrides();
            })
            .AddNewtonsoftJson(setup =>
            {
                setup.SerializerSettings.Formatting = settings.Advanced.MinifyJsonOutput ? Formatting.None : Formatting.Indented;
                setup.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                setup.SerializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;
                setup.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                setup.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                setup.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Include & DefaultValueHandling.Populate;
                setup.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            // configure the web server security settings
            ConfigureServiceWebServerSecurity(services, settings, development);

            // choose cache mechanism
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                // use the Redis mechanism
                services.AddRedisClientCache(options =>
                {
                    options.Configuration = redisConnectionString;
                });
            }
            else
            {
                // use local memory
                services.AddMemoryCache();
                services.AddDistributedMemoryCache();
            }

            // adds services for using options
            services.AddOptions();
        }

        /// <summary>
        /// Configures the service notification services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="redisConnectionString">Contains the redis connection string.</param>
        /// <param name="development">if set to <c>true</c> [development].</param>
        private static void ConfigureServiceSignalR(IServiceCollection services, ApplicationSettings settings, string redisConnectionString, bool development)
        {
            // add SignalR
            ISignalRServerBuilder signalBuilder = services.AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = development || settings.Advanced.ShowDiagnostics;
            })
            .AddNewtonsoftJsonProtocol(setup =>
            {
                setup.PayloadSerializerSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.None,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Include & DefaultValueHandling.Populate,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
            });

            // add redis backplane connection if redis connection information specified and Redis is being used for cache.
            if (signalBuilder != null)
            {
                switch (settings.Notifications.BackingType)
                {
                    case BackplaneType.Redis:
                        string redisConnection = !string.IsNullOrWhiteSpace(settings.Notifications.AlternativeRedisConnectionConfig) ? settings.Notifications.AlternativeRedisConnectionConfig : redisConnectionString;

                        if (!string.IsNullOrWhiteSpace(redisConnection))
                        {
                            Log.Debug(Resources.SignalRConfigureLoggingText, redisConnection);
                            signalBuilder.AddStackExchangeRedis(redisConnection, options =>
                            {
                                options.Configuration.ChannelPrefix = settings.Notifications.BackplaneChannelPrefix;
                            });
                        }

                        break;

                    case BackplaneType.Azure:
                        Log.Warning(Resources.SignalRAzureUnsupportedLoggingText);
                        break;
                }
            }
        }

        /// <summary>
        /// Configures the web server security.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="development">Contains a value indicating whether the application is in a dev environment.</param>
        private static void ConfigureServiceWebServerSecurity(IServiceCollection services, ApplicationSettings settings, bool development)
        {
            // setup HSTS settings
            services.AddHsts(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = development ? TimeSpan.FromMinutes(60) : TimeSpan.FromDays(365);
            });

            // configure the authentication method as bearer, and setup API authentication options.
            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    if (settings.Advanced.JwtTimeSkewInMinutes > 0)
                    {
                        // add clock skew for JWT timezone offset issues.
                        options.JwtValidationClockSkew = TimeSpan.FromMinutes(settings.Advanced.JwtTimeSkewInMinutes);
                    }

                    // this is the URL to the Vasont Identity Server
                    options.Authority = settings.IdentityProvider.AuthorityUri.ToString();

                    options.RequireHttpsMetadata = settings.IdentityProvider.RequireHttpsMetadata;

                    // set an API secret needed for introspection of non-Jwt tokens
                    options.ApiSecret = settings.IdentityProvider.ApiSecret;
                    options.IntrospectionDiscoveryPolicy = new IdentityModel.Client.DiscoveryPolicy();
                    options.SaveToken = true;
                    options.EnableCaching = settings.IdentityProvider.CacheMinutes > 0;
                    options.CacheDuration = TimeSpan.FromMinutes(settings.IdentityProvider.CacheMinutes);

                    // this is the name of this API
                    options.ApiName = SecurityDefaults.IdentityApiResource;

                    // Handling the token from query string in due to the reason that signalR clients are handling them over it.
                    options.TokenRetriever = new Func<HttpRequest, string>(req =>
                    {
                        var fromHeader = TokenRetrieval.FromAuthorizationHeader();
                        var fromQuery = TokenRetrieval.FromQueryString();
                        var fromQueryToken = TokenRetrieval.FromQueryString("token");

                        return fromHeader(req) ?? (fromQuery(req) ?? fromQueryToken(req));
                    });

                    // capture events to support requests to hub
                    options.JwtBearerEvents = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // for SignalR, they can only send access token via query parameter
                            string accessToken = context.HttpContext.Request.Query["access_token"].ConvertToString();
                            string path = context.HttpContext.Request.Path;

                            // If the request is for our hub...
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWith(NotificationHubRoute, StringComparison.OrdinalIgnoreCase))
                            {
                                // set the context access token.
                                context.Token = accessToken;

                                Log.Information("Hub access token received from {0}.", path);
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            // Configure the Default CORS configuration.
            services.AddCors(options =>
            {
                options.AddPolicy(DefaultCorsPolicy,
                    policy =>
                    {
                        policy.AllowAnyMethod();
                        policy.AllowAnyHeader();

                        // if origins defined, restrict them.
                        if (settings.Security.AllowedOrigins.Any())
                        {
                            policy.WithOrigins(settings.Security.AllowedOrigins.ToArray())
                                .SetIsOriginAllowedToAllowWildcardSubdomains()
                                .AllowCredentials();
                        }
                        else
                        {
                            // otherwise allow any, but most browsers will not allow loading of content.
                            policy.AllowAnyOrigin();
                        }

                        // For CSV or any file download need to expose the headers, otherwise in JavaScript response.getResponseHeader('Content-Disposition')
                        // retuns undefined https://stackoverflow.com/questions/58452531/im-not-able-to-access-response-headerscontent-disposition-on-client-even-aft
                        policy.WithExposedHeaders("Content-Disposition");
                    });
            });
        }

        #endregion

        #endregion
    }
}
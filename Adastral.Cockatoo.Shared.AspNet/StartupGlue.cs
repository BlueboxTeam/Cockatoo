using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using EFCoreSecondLevelCacheInterceptor;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.ComponentModel;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;

namespace Adastral.Cockatoo.Common.AspNet;

public partial class StartupGlue
{
    public static void ResponseCompression(IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
                "image/jpeg", "image/png", "application/font-woff2", "image/svg+xml", "text/javascript", "text/css"
            ]);
            options.EnableForHttps = true;
        });
    }

    public static void ForwardedHeadersOptions(IServiceCollection services)
    {
        var cfg = AppConfig.Instance;
        if (cfg.Proxy == null) return;
        var parsedProxyAddresses = new List<IPAddress>();
        var ipAddressValueMapping = new List<(string, IPAddress)>()
        {
            ("any", IPAddress.Any),
            ("loopback", IPAddress.Loopback),
            ("localhost", IPAddress.Loopback),
            ("ipv6any", IPAddress.IPv6Any),
            ("ipv6loopback", IPAddress.IPv6Loopback),
        };
        foreach (var addr in cfg.Proxy.KnownProxies.Distinct())
        {
            var altTarget = ipAddressValueMapping
                .Where(e => e.Item1.Equals(addr, StringComparison.InvariantCultureIgnoreCase))
                .Select(e => e.Item2)
                .FirstOrDefault();
            if (altTarget != null)
            {
                parsedProxyAddresses.Add(altTarget);
            }
            else
            {
                if (!IPAddress.TryParse(addr, out var ipAddr))
                {
                    throw new InvalidOperationException($"Invalid IP Address format for Known Proxy address: \"{addr}\"");
                }
                parsedProxyAddresses.Add(ipAddr);
            }
        }
        services.Configure<ForwardedHeadersOptions>(opts =>
        {
            foreach (var a in parsedProxyAddresses) opts.KnownProxies.Add(a);
            if (cfg.Proxy.ForwardedHeaders.HasValue)
            {
                opts.ForwardedHeaders = cfg.Proxy.ForwardedHeaders.Value;
            }
        });
    }

    public static void Database(IServiceCollection services, DatabaseServicesOptions options)
    {
        // Add services to the container.
        services.AddDbContextPool<ApplicationDbContext>(
            o =>
            {
                // TODO update cockatoo config to use xml format (like Get Psyched! Partner App)
                var cfg = AppConfig.Instance;
                var connectionString = cfg.Database.ToConnectionString();
                o.UseNpgsql(connectionString);

                if (options.EnableSensitiveDataLogging)
                {
                    o.EnableSensitiveDataLogging();
                }
            })
            .AddDataAccessRepositories();
        if (options.DatabaseDeveloperPageExceptionFilter)
        {
            services.AddDatabaseDeveloperPageExceptionFilter();
        }

        services.AddDefaultIdentity<UserModel>(
                options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                    options.Password.RequireNonAlphanumeric = false;
                })
            .AddRoles<RoleModel>()
            // .AddUserManager<CustomUserManager<UserModel>>() // TODO implement CustomUserManager and CustomSignInManager
            // .AddSignInManager<CustomSignInManager<UserModel>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
    }

    public class DatabaseServicesOptions
    {
        /// <summary>
        /// Enabled in development mode.
        /// </summary>
        [DefaultValue(false)]
        public bool EnableSensitiveDataLogging { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(false)]
        public bool DatabaseDeveloperPageExceptionFilter { get; set; } = false;
    }
    
    public static void Cache(IServiceCollection services)
    {
        var logger = NLog.LogManager.GetCurrentClassLogger();
        var cfg = AppConfig.Instance;

        if (cfg.Cache.InMemory == null && cfg.Cache.Redis == null)
        {
            cfg.Cache.InMemory ??= new();
        }
        var providerName = cfg.Cache.Redis != null ? "Redis" : "InMemory";

        services.AddEFSecondLevelCache(options =>
        {
            options.UseEasyCachingCoreProvider(providerName, isHybridCache: false)
                   .ConfigureLogging(true)
                   .UseCacheKeyPrefix(cfg.Cache.CachePrefix)
                   // Fallback on db if the caching provider fails (for example, if Redis is down).
                   .UseDbCallsIfCachingProviderIsDown(TimeSpan.FromMinutes(1));
        });

        services.AddEasyCaching(options =>
        {
            cfg = AppConfig.Instance;
            var enableRedis = cfg.Cache.Redis?.Enable ?? false;
            if (enableRedis)
            {
                enableRedis = cfg.Cache.Redis!.DbConfig.Endpoints.Count >= 1;
                if (!enableRedis)
                {
                    logger.Warn("Disabling Redis Cache since no endpoints are defined.");
                }
            }
            if (enableRedis)
            {
                var redisConfig = cfg.Cache.Redis!;
                options.UseRedis(config =>
                {
                    config.DBConfig = new()
                    {
                        Database = redisConfig.DbConfig.Database,
                        AsyncTimeout = redisConfig.DbConfig.AsyncTimeout,
                        SyncTimeout = redisConfig.DbConfig.SyncTimeout,
                        KeyPrefix = cfg.Cache.CachePrefix,

                        Username = string.IsNullOrEmpty(redisConfig.DbConfig.Username) ? "" : redisConfig.DbConfig.Username,
                        Password = string.IsNullOrEmpty(redisConfig.DbConfig.Password) ? "" : redisConfig.DbConfig.Password,
                        IsSsl = redisConfig.DbConfig.SslEnabled,
                        SslHost = redisConfig.DbConfig.SslHost,
                        ConnectionTimeout = redisConfig.DbConfig.ConnectionTimeout,
                        AllowAdmin = redisConfig.DbConfig.AllowAdmin,
                        AbortOnConnectFail = redisConfig.DbConfig.AbortOnConnectFail,
                    };
                    config.DBConfig.Endpoints.Clear();
                    foreach (var endpoint in redisConfig.DbConfig.Endpoints)
                    {
                        config.DBConfig.Endpoints.Add(new(endpoint.Host, endpoint.Port));
                    }
                    config.EnableLogging = redisConfig.EnableLogging;
                    config.SerializerName = "Pack";

                }, "Redis")
                .WithMessagePack(so =>
                {
                    so.EnableCustomResolver = true;
                    var formatters = new IMessagePackFormatter[]
                    {
                        DBNullFormatter.Instance, // This is necessary for the null values
                    };
                    var formatterResolvers = new IFormatterResolver[]
                    {
                        NativeDateTimeResolver.Instance,
                        ContractlessStandardResolver.Instance,
                        StandardResolverAllowPrivate.Instance,
                    };
                    so.CustomResolvers = CompositeResolver.Create(formatters, formatterResolvers);
                }, "Pack");
            }
            else
            {
                var memoryConfig = cfg.Cache.InMemory ?? new();
                options.UseInMemory(config =>
                {
                    config.DBConfig = new EasyCaching.InMemory.InMemoryCachingOptions()
                    {
                        ExpirationScanFrequency = memoryConfig.DbConfig.ExpirationScanFrequency,
                        SizeLimit = memoryConfig.DbConfig.SizeLimit,
                        EnableReadDeepClone = memoryConfig.DbConfig.EnableReadDeepClone,
                        EnableWriteDeepClone = memoryConfig.DbConfig.EnableWriteDeepClone
                    };

                    config.MaxRdSecond = memoryConfig.MaxRandomSeconds;
                    config.EnableLogging = memoryConfig.EnableLogging;
                    config.LockMs = memoryConfig.LockMilliseconds;
                    config.SleepMs = memoryConfig.SleepMilliseconds;
                }, "InMemory");
            }
        });
    }

    public static void InitializeNLog()
    {
        System.Diagnostics.Debug.WriteLine("Initialize NLog");

        var relativeConfigurationLocation = Path.Combine(Environment.CurrentDirectory, "nlog.config");
        if (File.Exists(FeatureFlags.NLogConfigLocation))
        {
            System.Diagnostics.Debug.WriteLine("Loading configuration from " + FeatureFlags.NLogConfigLocation);
            LogManager.Setup().LoadConfigurationFromFile(FeatureFlags.NLogConfigLocation);
        }
        else if (File.Exists(relativeConfigurationLocation))
        {
            System.Diagnostics.Debug.WriteLine("Loading configuration from " + relativeConfigurationLocation);
            LogManager.Setup().LoadConfigurationFromFile(relativeConfigurationLocation);
        }
        else
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly?.GetManifestResourceNames().Any(e => e == $"{entryAssembly.GetName().Name}.nlog.config") ?? false)
            {
                LogManager.Setup().LoadConfigurationFromAssemblyResource(entryAssembly, "nlog.config");
            }
            else
            {
                LogManager.Setup().LoadConfigurationFromAssemblyResource(typeof(StartupGlue).Assembly, "nlog.config");
            }
        }

        if (!string.IsNullOrEmpty(FeatureFlags.SentryDSN))
        {
            LogManager.Configuration!.AddSentry(
                opts =>
                {
                    SetSentryOptions(opts);
                    opts.MinimumBreadcrumbLevel = NLog.LogLevel.Trace;
                    opts.MinimumEventLevel = NLog.LogLevel.Warn;
                });
        }
    }
    private static void SetSentryOptions(SentryOptions opts)
    {
        opts.Dsn = FeatureFlags.SentryDSN;
        opts.Release = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        opts.SendDefaultPii = true;
        opts.AttachStacktrace = true;
        opts.DiagnosticLevel = SentryLevel.Debug;
        opts.TracesSampleRate = 1.0;
#if DEBUG
        opts.Debug = true;
#else
        opts.Debug = false;
#endif
        if (File.Exists(FeatureFlags.ConfigLocation))
        {
            PopulateSentryOptionsFromConfig(opts);
        }
    }
    public static void PopulateSentryOptionsFromConfig(SentryOptions opts)
    {
        var cfg = AppConfig.Instance;
        if (cfg.Sentry != null)
        {
            if (cfg.Sentry.SampleRate != null && cfg.Sentry.SampleRate.HasValue)
            {
                if (cfg.Sentry.SampleRate < 0.0f)
                {
                    opts.SampleRate = null;
                }
                else if (cfg.Sentry.SampleRate <= 1.0f)
                {
                    opts.SampleRate = cfg.Sentry.SampleRate.Value;
                }
                else if (cfg.Sentry.SampleRate > 1.0f)
                {
                    opts.SampleRate = cfg.Sentry.SampleRate.Value % 1.0f;
                }
            }

            if (cfg.Sentry.ProfilesSampleRate != null && cfg.Sentry.ProfilesSampleRate.HasValue)
            {
                if (cfg.Sentry.ProfilesSampleRate < 0.0f)
                {
                    opts.ProfilesSampleRate = null;
                }
                else if (cfg.Sentry.ProfilesSampleRate <= 1.0f)
                {
                    opts.ProfilesSampleRate = cfg.Sentry.ProfilesSampleRate.Value;
                }
                else if (cfg.Sentry.ProfilesSampleRate > 1.0f)
                {
                    opts.ProfilesSampleRate = cfg.Sentry.ProfilesSampleRate.Value % 1.0f;
                }
            }

            if (cfg.Sentry.TracesSampleRate != null && cfg.Sentry.TracesSampleRate.HasValue)
            {
                if (cfg.Sentry.TracesSampleRate < 0.0f)
                {
                    opts.TracesSampleRate = null;
                }
                else if (cfg.Sentry.TracesSampleRate <= 1.0f)
                {
                    opts.TracesSampleRate = cfg.Sentry.TracesSampleRate.Value;
                }
                else if (cfg.Sentry.TracesSampleRate > 1.0f)
                {
                    opts.TracesSampleRate = cfg.Sentry.TracesSampleRate.Value % 1.0f;
                }
            }
        }
        else
        {
            opts.TracesSampleRate = 1.0;
        }
    }

    public static void ApplyWebHostBuilder(IWebHostBuilder webBuilder)
    {
        webBuilder.UseKestrel(ApplyKestrelLimits);
        if (!string.IsNullOrEmpty(FeatureFlags.SentryDSN))
        {
            webBuilder.UseSentry(opts =>
            {
                SetSentryOptions(opts);
                opts.MinimumBreadcrumbLevel = Microsoft.Extensions.Logging.LogLevel.Trace;
                opts.MinimumEventLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
                opts.MaxRequestBodySize = Sentry.Extensibility.RequestSize.Always;
            });
        }
    }

    public static void ApplyKestrelLimits(KestrelServerOptions opts)
    {
        var cfg = AppConfig.Instance;

        if (cfg.Kestrel?.Limits == null) return;

        var limits = cfg.Kestrel.Limits;
        if (limits.MaxResponseBufferSize != null)
        {
            if (limits.MaxResponseBufferSize == -1)
            {
                opts.Limits.MaxResponseBufferSize = -1;
            }
            else
            {
                opts.Limits.MaxResponseBufferSize = limits.MaxResponseBufferSize.Value;
            }
        }
        if (limits.MaxRequestBufferSize != null)
        {
            if (limits.MaxRequestBufferSize == -1)
            {
                opts.Limits.MaxRequestBufferSize = null;
            }
            else
            {
                opts.Limits.MaxRequestBufferSize = limits.MaxRequestBufferSize.Value;
            }
        }
        if (limits.MaxRequestLineSize != null && limits.MaxRequestLineSize.HasValue)
        {
            opts.Limits.MaxRequestLineSize = limits.MaxRequestLineSize.Value;
        }
        if (limits.MaxRequestHeadersTotalSize != null && limits.MaxRequestHeadersTotalSize.HasValue)
        {
            opts.Limits.MaxRequestHeadersTotalSize = limits.MaxRequestHeadersTotalSize.Value;
        }
        if (limits.MaxRequestHeaderCount != null && limits.MaxRequestHeaderCount.HasValue)
        {
            opts.Limits.MaxRequestHeaderCount = limits.MaxRequestHeaderCount.Value;
        }
        if (limits.MaxRequestBodySize != null)
        {
            if (limits.MaxRequestBodySize == -1)
            {
                opts.Limits.MaxRequestBodySize = null;
            }
            else
            {
                opts.Limits.MaxRequestBodySize = limits.MaxRequestBodySize.Value;
            }
        }
        if (limits.KeepAliveTimeout != null)
        {
            opts.Limits.KeepAliveTimeout = limits.KeepAliveTimeout.ToTimeSpan();
        }
        if (limits.RequestHeadersTimeout != null)
        {
            opts.Limits.RequestHeadersTimeout = limits.RequestHeadersTimeout.ToTimeSpan();
        }
        if (limits.MaxConcurrentConnections != null)
        {
            if (limits.MaxConcurrentConnections == -1)
            {
                opts.Limits.MaxConcurrentConnections = null;
            }
            else
            {
                opts.Limits.MaxConcurrentConnections = limits.MaxConcurrentConnections.Value;
            }
        }
        if (limits.MaxConcurrentUpgradedConnections != null)
        {
            if (limits.MaxConcurrentUpgradedConnections == -1)
            {
                opts.Limits.MaxConcurrentUpgradedConnections = null;
            }
            else
            {
                opts.Limits.MaxConcurrentUpgradedConnections = limits.MaxConcurrentUpgradedConnections.Value;
            }
        }

        if (limits.EnforceMinRequestBodyDataRate == false)
        {
            opts.Limits.MinRequestBodyDataRate = null;
        }
        else if (limits.MinRequestBodyDataRate != null)
        {
            var dataRate = limits.MinRequestBodyDataRate;
            opts.Limits.MinRequestBodyDataRate = new MinDataRate(dataRate.BytesPerSecond, dataRate.GracePeriod.ToTimeSpan());
        }

        if (limits.EnforceMinResponseDataRate == false)
        {
            opts.Limits.MinResponseDataRate = null;
        }
        else if (limits.MinResponseDataRate != null)
        {
            var dataRate = limits.MinResponseDataRate;
            opts.Limits.MinResponseDataRate = new MinDataRate(dataRate.BytesPerSecond, dataRate.GracePeriod.ToTimeSpan());
        }
    }
    public static void CheckConfiguration()
    {
        var logger = LogManager.GetLogger(nameof(CheckConfiguration));
        try
        {
            // We know for sure that the default configuration location used
            // by Cockatoo is "/config", and that this will only happen on Docker.
            //
            // This is here just to be sure that the creation of the config
            // directory will not fail.
            if (FeatureFlags.RunningInDocker && !Directory.Exists("/config"))
            {
                Directory.CreateDirectory("/config");
            }

            logger.Info($"Using File: {FeatureFlags.ConfigLocation}");
            if (string.IsNullOrEmpty(FeatureFlags.ConfigLocation))
            {
                logger.Error($"Environment Variable CONFIG_LOCATION has not been set!!!");
                Environment.Exit(1);
                return;
            }

            // Create parent directory if it doesn't exist.
            var parentDirectory = Path.GetDirectoryName(FeatureFlags.ConfigLocation);
            if (!string.IsNullOrEmpty(parentDirectory) && !Directory.Exists(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            if (!File.Exists(FeatureFlags.ConfigLocation))
            {
                var selfType = typeof(StartupGlue);
                logger.Warn("Configuration file does not exist!! Creating a blank one, PLEASE POPULATE IT !!!");
                File.WriteAllText(FeatureFlags.ConfigLocation, string.Empty);
                if (TryGetExampleConfigurationStream(out var exampleConfigStream))
                {
                    using var file = new FileStream(
                        FeatureFlags.ConfigLocation,
                        FileMode.OpenOrCreate,
                        FileAccess.Write,
                        FileShare.Read);

                    file.SetLength(0);
                    file.Seek(0, SeekOrigin.Begin);
                    exampleConfigStream!.CopyTo(file);
                }
                else
                {
                    logger.Fatal($"Couldn't find embedded resource {selfType.Namespace}.config.example.xml");
                }
                Environment.Exit(1);
                return;
            }
            else
            {
                logger.Info($"Configuration file found! ({FeatureFlags.ConfigLocation})");
            }
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Failed to check configuration!");
        }
    }

    private static bool TryGetExampleConfigurationStream(out Stream? stream)
    {
        var current = typeof(StartupGlue);
        foreach (var resource in current.Assembly.GetManifestResourceNames()
            .Where(resource => resource.ToLower().Trim().EndsWith(".config.example.xml")))
        {
            stream = current.Assembly.GetManifestResourceStream(resource);
            if (stream != null)
                return true;
        }

        stream = null;
        return false;
    }
}
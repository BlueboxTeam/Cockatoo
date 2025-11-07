using System.ComponentModel;
using System.Net;
using Adastral.Cockatoo.Common;
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

namespace Adastral.Cockatoo.Shared.AspNet;

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
            });
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
}
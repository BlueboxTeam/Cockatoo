using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Common.AspNet;
using Adastral.Cockatoo.WebApi;
using Microsoft.IdentityModel.Logging;
using NLog.Web;

if (args.FirstOrDefault()?.Trim().Equals("docker", StringComparison.OrdinalIgnoreCase) ?? false)
{
    Environment.SetEnvironmentVariable(FeatureFlags.RunningInDockerName, "true");
}
if (FeatureFlags.IsDevelopmentEnvironment)
{
    IdentityModelEventSource.ShowPII = true;
    IdentityModelEventSource.LogCompleteSecurityArtifact = true;
}

StartupGlue.InitializeNLog();
StartupGlue.CheckConfiguration();
await RunServer(args);

static async Task RunServer(string[] args)
{
    var h = Host.CreateDefaultBuilder(args)
        .UseNLog().ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
            StartupGlue.ApplyWebHostBuilder(webBuilder);
        });
    await h.RunConsoleAsync();
}
/*
namespace Adastral.Cockatoo.WebApi;

public static class Program
{
    public static void Main(string[] args)
    {
        RunServer(ref args);
    }

    private static void RunServer(ref string[] args)
    {
        var h = Host.CreateDefaultBuilder(args)
            .UseNLog().ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                StartupGlue.ApplyWebHostBuilder(webBuilder);
            });
        h.RunConsoleAsync().Wait();
    }
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    public static string Version => typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

    private static WebApplication? WebApp = null;
    private static WebApplicationBuilder? WebAppBuilder = null;

    private static void InitializeWebApplication(CoreContext core, string[] args)
    {
        _log.Debug("Creating WebApplication Builder");
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        // Add services to the container.
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options => {
            options.Cookie.IsEssential = true;
            options.Cookie.Name = ".Cockatoo.WebApi.Session";
            options.IdleTimeout = TimeSpan.FromDays(2);
        });
        var pluginAssembly = typeof(AdminGroupApiV1Controller).Assembly;
        builder.Services.AddMvc(options =>
        {
            options.EnableEndpointRouting = false;
        }).AddApplicationPart(pluginAssembly);
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        // add swagger-related stuff
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("basicAuth", new OpenApiSecurityScheme
            {
                Description = "Basic Authorization",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Scheme = "basic",
                Type = SecuritySchemeType.Http
            });
            options.AddSecurityDefinition("tokenHeader", new OpenApiSecurityScheme
            {
                Description = "Cockatoo Token Header",
                Name = core.Config.AspNET.TokenHeader,
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "basicAuth"
                        }
                    },
                    new string[] {}
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "tokenHeader"
                        }
                    },
                    new string[] {}
                }
            });
            options.OperationFilter<FileUploadFilter>();
        });
        builder.Services.AddAuthorization();
        var authBuilder = builder.Services.AddAuthentication(
                options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
            .AddCookie(
                options =>
                {
                    options.LoginPath = "/signin";
                    options.LogoutPath = "/signout";
                    options.ReturnUrlParameter = "redirect";
                });

        // initialize nlog logger
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);
        });
        builder.Services.AddSingleton<ILoggerProvider, NLogLoggerProvider>();
        if (core.Config.Redis.Enable)
        {
            if (string.IsNullOrEmpty(core.Config.Redis.ConnectionString))
            {
                throw new NoNullAllowedException($"{nameof(core.Config.Redis.ConnectionString)} is required when Redis is enabled.");
            }
            builder.Services.AddStackExchangeRedisCache((opts) =>
            {
                opts.Configuration = core.Config.Redis.ConnectionString.Replace("\"", "");
                if (!string.IsNullOrEmpty(core.Config.Redis.InstanceName))
                {
                    opts.InstanceName = core.Config.Redis.InstanceName;
                }
            });
        }
        else
        {
            builder.Services.AddDistributedMemoryCache();
        }
        if (FeatureFlags.SentryEnable)
        {
            if (string.IsNullOrEmpty(FeatureFlags.SentryDSN))
            {
                throw new Exception($"Missing FeatureFlag {nameof(FeatureFlags.SentryDSN)} when {nameof(FeatureFlags.SentryEnable)} is enabled!");
            }
            builder.WebHost.UseSentry(FeatureFlags.SentryDSN);
        }
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
            serverOptions.AllowSynchronousIO = true;
        });
        WebAppBuilder = builder;

        core.BuildServiceCollectionAction = (col) => {
            if (col == builder.Services)
            {
                var app = builder.Build();
                WebApp = app;
                // enable swagger when in development or when it's enabled
                if (app.Environment.IsDevelopment() || core.Config.AspNET.SwaggerEnable)
                {
                    Console.WriteLine($"Enabled Swagger (core.Config.AspNET.SwaggerEnable={core.Config.AspNET.SwaggerEnable})");
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseRouting();
                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                if (FeatureFlags.SentryEnable && app.Environment.IsDevelopment() == false)
                {
                    if (string.IsNullOrEmpty(FeatureFlags.SentryDSN))
                    {
                        throw new Exception($"Missing FeatureFlag {nameof(FeatureFlags.SentryDSN)} when {nameof(FeatureFlags.SentryEnable)} is enabled!");
                    }
                    app.UseSentryTracing();
                }

                app.UseAuthorization();
                app.UseCookiePolicy();
                app.UseMvc();
                app.UseSession();

                return app.Services;
            }
            else
            {
                throw new ArgumentException($"Does not equal {nameof(builder)}.{nameof(builder.Services)}!", nameof(col));
            }
        };
    }*/
}
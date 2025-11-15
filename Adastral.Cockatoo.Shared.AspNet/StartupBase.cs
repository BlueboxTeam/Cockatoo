using Adastral.Cockatoo.Common.AspNet.Jobs;
using Adastral.Cockatoo.DataAccess;
using FluentScheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Adastral.Cockatoo.Common.AspNet;

public class StartupBase
{
    protected IWebHostEnvironment WebHostEnvironment { get; }
    public IConfiguration Configuration { get; }

    public StartupBase(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        WebHostEnvironment = env;

        JobManager.Initialize();
    }

    public IServiceScope? DefaultScope { get; private set; }
    public IServiceProvider? Services => DefaultScope?.ServiceProvider;

    protected virtual ApplicationInfo GetApplicationInfo()
    {
        throw new NotImplementedException();
    }

    protected virtual bool IncludeReloadConfigurationJob() => true;

    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        DefaultScope = app.ApplicationServices.CreateScope();

        // Configure the HTTP request pipeline.
        if (env.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            var context = Services!.GetRequiredService<ApplicationDbContext>();
            var migrations = context.Database.GetPendingMigrations().ToList();
            if (migrations.Count > 0)
            {
                var logger = LogManager.GetCurrentClassLogger();
                logger.Info("Applying the following migrations:\n{Migrations}",
                    string.Join(Environment.NewLine, migrations.Select(e => "- " + e)));
                try
                {
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to apply one or more database migrations!");
                    throw new DatabaseMigrationFailureException(migrations, ex);
                }
            }
        }

        // TODO should this even be done? backported from Kasta
        /*
        using (var scope = app.ApplicationServices.CreateScope())
        {
            using (var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().CreateSession())
            {
                ctx.EnsureInitialRoles();
                var trans = ctx.Database.BeginTransaction();
                try
                {
                    var s = ctx.GetSystemSettings();
                    ctx.SaveChanges();
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to insert global preferences.\n{ex}");
                    trans.Rollback();
                }
            }
        }
        */

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpointBuilder =>
        {
            endpointBuilder.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            endpointBuilder.MapRazorPages();
        });
    }

    public virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IApplicationInfo>(GetApplicationInfo());
        services.AddHostedService<JobManagerHostedService>();
        if (IncludeReloadConfigurationJob())
        {
            services.AddHostedService<ReloadConfigurationJob>();
        }

        StartupGlue.ResponseCompression(services);
        StartupGlue.ForwardedHeadersOptions(services);
        StartupGlue.Database(services, GetDatabaseServicesOptions());
        StartupGlue.Cache(services);
        StartupGlue.Authentication(services);
        ConfigureMvc(services.AddMvc());
        ConfigureDataProtection(services.AddDataProtection(SetupDataProtectionOptions).PersistKeysToDbContext<ApplicationDbContext>());
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        /*services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new BlockUserRegisterAttribute());
        });*/
    }

    protected virtual void ConfigureMvc(IMvcBuilder builder)
    {
    }
    protected virtual void ConfigureDataProtection(IDataProtectionBuilder builder)
    {
    }
    protected virtual StartupGlue.DatabaseServicesOptions GetDatabaseServicesOptions()
    {
        return new();
    }
    protected virtual void SetupDataProtectionOptions(DataProtectionOptions options)
    {
    }
}

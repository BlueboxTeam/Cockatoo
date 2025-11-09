using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.Shared.AspNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Adastral.Cockatoo.Services;

public class StartupBase
{
    protected IWebHostEnvironment WebHostEnvironment { get; }
    public IConfiguration Configuration { get; }

    public StartupBase(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        WebHostEnvironment = env;
    }

    public IServiceScope? DefaultScope { get; private set; }
    public IServiceProvider? Services => DefaultScope?.ServiceProvider;

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
            if (migrations.Any())
            {
                var logger = LogManager.GetCurrentClassLogger();
                logger.Info("Applying the following migrations:" + Environment.NewLine + string.Join(Environment.NewLine, migrations.Select(e => "- " + e)));
                context.Database.Migrate();
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
        StartupGlue.ResponseCompression(services);
        StartupGlue.ForwardedHeadersOptions(services);
        StartupGlue.Database(services, new());
        StartupGlue.Cache(services);
        StartupGlue.Authentication(services);
        services.AddMvc();
        services.AddEndpointsApiExplorer();
        /*services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new BlockUserRegisterAttribute());
        });*/
        services.AddHttpContextAccessor();
        services.AddDataProtection().PersistKeysToDbContext<ApplicationDbContext>();
    }
}

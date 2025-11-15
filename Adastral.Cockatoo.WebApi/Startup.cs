using Adastral.Cockatoo.Common.AspNet;
using StartupBase = Adastral.Cockatoo.Common.AspNet.StartupBase;

namespace Adastral.Cockatoo.WebApi;

public class Startup : StartupBase
{
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
        : base(configuration, env)
    {
    }

    protected override ApplicationInfo GetApplicationInfo()
    {
        return new ApplicationInfo
        {
            Assembly = GetType().Assembly,
            DisplayName = "Cockatoo (WebApi)",
            AssemblyName = "Adastral.Cockatoo.WebApi" // NOTE update me when Startup namespace has changed
        };
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddControllers();
    }
}

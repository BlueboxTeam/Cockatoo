using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterCockatooServices(this IServiceCollection services) => services
        .AddScoped<ApplicationDetailService>()
        .AddScoped<BlogPostService>()
        .AddScoped<BullseyeCacheService>()
        .AddScoped<BullseyeService>()
        .AddScoped<GroupService>()
        .AddScoped<PermissionCacheService>()
        .AddScoped<PermissionService>()
        .AddScoped<S3Service>()
        .AddScoped<SouthbankService>()
        .AddScoped<SteamApiService>()
        .AddScoped<StorageService>()
        .AddScoped<TaskMutexService>()
        .AddScoped<UserProfileService>()
        .AddScoped<UserService>();
}

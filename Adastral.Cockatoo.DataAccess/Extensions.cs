using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.AutoUpdaterDotNet;
using Adastral.Cockatoo.DataAccess.Repositories.Group;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.DataAccess;

public static class Extensions
{
    public static IServiceCollection AddDataAccessRepositories<T>(this T services)
        where T : IServiceCollection
    {
        return services
            .AddScoped<AUDNRevisionRepository>()

            .AddScoped<BullseyeAppRepository>()
            .AddScoped<BullseyeRevisionRepository>()
            .AddScoped<BullseyePatchRepository>()
            .AddScoped<BullseyeV1CacheRepository>()
            .AddScoped<BullseyeV2CacheRepository>()

            .AddScoped<GroupPermissionApplicationRepository>()
            .AddScoped<GroupPermissionGlobalRepository>()
            .AddScoped<GroupRepository>()
            .AddScoped<GroupUserAssociationRepository>()

            .AddScoped<UserRepository>()
            .AddScoped<UserApplicationPermissionCacheRepository>()
            .AddScoped<UserGlobalPermissionCacheRepository>()

            .AddScoped<ApplicationColorRepository>()
            .AddScoped<ApplicationImageRepository>()
            .AddScoped<ApplicationRepository>()

            .AddScoped<BlogPostAttachmentRepository>()
            .AddScoped<BlogPostRepository>()
            .AddScoped<BlogPostTagRepository>()
            .AddScoped<BlogTagRepository>()

            .AddScoped<SouthbankCacheRepository>()
            .AddScoped<StorageFileRepository>()
            
            .AddScoped<TaskMutexRepository>();
    }
}

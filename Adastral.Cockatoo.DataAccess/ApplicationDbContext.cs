using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Models.AutoUpdaterDotNet;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adastral.Cockatoo.DataAccess;

public class ApplicationDbContext
    : IdentityDbContext<UserModel, RoleModel, Guid>
    , IDataProtectionKeyContext
{
    private readonly DbContextOptions<ApplicationDbContext> _ops;
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        _ops = options;
    }

    public ApplicationDbContext CreateSession()
    {
        return new(_ops);
    }

    #region IDataProtectionKeyContext
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    #endregion

    public DbSet<StorageFileModel> StorageFiles { get; set; }

    // TODO need to see if this is easier to do with ASP.NET Core Identity or not.
    public DbSet<ServiceAccountModel> ServiceAccounts { get; set; }
    public DbSet<ServiceAccountTokenModel> ServiceAccountTokens { get; set; }

    #region Application
    public DbSet<ApplicationModel> Applications { get; set; }
    public DbSet<ApplicationSourceModModel> ApplicationSourceMods { get; set; }
    public DbSet<ApplicationBrandAssetModel> ApplicationBrandAssets { get; set; }
    public DbSet<ApplicationBrandColorModel> ApplicationBrandColors { get; set; }

    public DbSet<ApplicationBullseyeModel> ApplicationBullseye { get; set; }
    public DbSet<BullseyePatchModel> BullseyePatches { get; set; }
    public DbSet<BullseyeRevisionModel> BullseyeRevisions { get; set; }
    public DbSet<BullseyeV1CacheModel> BullseyeCacheV1 { get; set; }
    public DbSet<BullseyeV2CacheModel> BullseyeCacheV2 { get; set; }
    public DbSet<SouthbankCacheModel> SouthbankCache { get; set; }
    public DbSet<AutoUpdaterDotNetRevisionModel> AutoUpdaterDotNetRevisions { get; set; } // TODO fluent config
    #endregion

    #region Groups
    public DbSet<GroupModel> Groups { get; set; }
    public DbSet<GroupMembershipModel> GroupMemberships { get; set; }
    public DbSet<GroupPermissionApplicationModel> GroupApplicationPermissions { get; set; }
    public DbSet<GroupPermissionGlobalModel> GroupGlobalPermissions { get; set; }
    #endregion

    #region Permission Cache
    public DbSet<UserGlobalPermissionCacheModel> UserGlobalPermissionCache { get; set; }
    public DbSet<UserApplicationPermissionCacheModel> UserApplicationPermissionCache { get; set; }
    #endregion

    #region Blog
    public DbSet<BlogPostModel> BlogPosts { get; set; }
    public DbSet<BlogPostAttachmentModel> BlogPostAttachments { get; set; }
    public DbSet<BlogPostAuthorModel> BlogPostAuthors { get; set; }
    public DbSet<BlogPostTagModel> BlogPostTags { get; set; }
    public DbSet<BlogTagModel> BlogTags { get; set; }
    #endregion

    public DbSet<TaskMutexModel> TaskMutexes { get; set; }

    // NOTE ScopedApplicationRoleModel will be used in the future to replace GroupPermissions
    public DbSet<ScopedApplicationRoleModel> ScopedApplicationRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<TaskMutexModel>(b =>
        {
            b.ToTable(TaskMutexModel.TableName).HasKey(e => e.Id);
        });
        #region Permission Cache
        builder.Entity<UserGlobalPermissionCacheModel>(b =>
        {
            b.ToTable(UserGlobalPermissionCacheModel.TableName)
            .HasKey(e => new { e.UserId, e.Permission });
        });
        builder.Entity<UserApplicationPermissionCacheModel>(b =>
        {
            b.ToTable(UserApplicationPermissionCacheModel.TableName)
            .HasKey(e => new { e.UserId, e.ApplicationId, e.Permission });
        });
        #endregion

        builder.Entity<StorageFileModel>(b =>
        {
            b.ToTable(StorageFileModel.TableName).HasKey(e => e.Id);

            b.HasIndex(e => e.CreatedAt).IsDescending().IsUnique(false);

            b.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId);
        });

        #region Application
        builder.Entity<ApplicationModel>(b =>
        {
            b.ToTable(ApplicationModel.TableName).HasKey(e => e.Id);

            b.HasMany(e => e.BrandColors)
                .WithOne()
                .HasForeignKey(e => e.ApplicationId)
                .IsRequired();
            b.HasMany(e => e.BrandAssets)
                .WithOne()
                .HasForeignKey(e => e.ApplicationId)
                .IsRequired();

            b.HasOne(e => e.SourceMod)
                .WithOne()
                .HasForeignKey<ApplicationSourceModModel>(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(true);
        });
        builder.Entity<ApplicationBrandColorModel>(b =>
        {
            b.ToTable(ApplicationBrandColorModel.TableName)
            .HasKey(e => new { e.ApplicationId, e.Type });
        });
        builder.Entity<ApplicationBrandAssetModel>(b =>
        {
            b.ToTable(ApplicationBrandAssetModel.TableName)
            .HasKey(e => new { e.ApplicationId, e.Type });
        });
        builder.Entity<ApplicationSourceModModel>(b =>
        {
            b.ToTable(ApplicationSourceModModel.TableName).HasKey(e => e.ApplicationId);

            b.ComplexProperty(e => e.RequiredAppIds, e => e.ToJson());
        });

        builder.Entity<ApplicationBullseyeModel>(b =>
        {
            b.ToTable(ApplicationBullseyeModel.TableName).HasKey(e => e.ApplicationId);

            b.HasOne(e => e.CacheV1)
                .WithOne()
                .HasForeignKey<BullseyeV1CacheModel>(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
            
            b.HasOne(e => e.CacheV2)
                .WithOne()
                .HasForeignKey<BullseyeV2CacheModel>(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        });
        builder.Entity<BullseyePatchModel>(b =>
        {
            b.ToTable(BullseyePatchModel.TableName).HasKey(e => e.Id);
        });
        builder.Entity<BullseyeRevisionModel>(b =>
        {
            b.ToTable(BullseyeRevisionModel.TableName).HasKey(e => e.Id);
        });
        builder.Entity<BullseyeV1CacheModel>(b =>
        {
            b.ToTable(BullseyeV1CacheModel.TableName).HasKey(e => e.ApplicationId);
            b.ComplexProperty(p => p.Content, d => d.ToJson());
        });
        builder.Entity<BullseyeV2CacheModel>(b =>
        {
            b.ToTable(BullseyeV2CacheModel.TableName).HasKey(e => e.ApplicationId);
            b.ComplexProperty(p => p.Content, d => d.ToJson());
        });

        builder.Entity<SouthbankCacheModel>(b =>
        {
            b.ToTable(SouthbankCacheModel.TableName).HasKey(e => e.Id);
            b.HasIndex(e => e.CreatedAt).IsUnique(false).IsDescending(true);

            b.ComplexProperty(p => p.V1, d => d.ToJson());
            b.ComplexProperty(p => p.V2, d => d.ToJson());
            b.ComplexProperty(p => p.V3, d => d.ToJson());
        });
        #endregion

        #region Groups
        builder.Entity<GroupModel>(b =>
        {
            b.ToTable(GroupModel.TableName).HasKey(e => e.Id);

            b.HasIndex(e => e.Priority).IsUnique(false).IsDescending();
        });
        builder.Entity<GroupMembershipModel>(b =>
        {
            b.ToTable(GroupMembershipModel.TableName).HasKey(e => e.Id);

            b.HasOne(e => e.Group)
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .IsRequired();

            b.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired();

            b.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId);
        });
        builder.Entity<GroupPermissionApplicationModel>(b =>
        {
            b.ToTable(GroupPermissionApplicationModel.TableName);

            b.HasOne(e => e.Application)
                .WithMany()
                .HasForeignKey(e => e.ApplicationId);
            
            PerformBasePermissionGroupModel<GroupPermissionApplicationModel, ScopedApplicationPermissionKind>(b);
        });
        builder.Entity<GroupPermissionGlobalModel>(b =>
        {
            b.ToTable(GroupPermissionGlobalModel.TableName);
            PerformBasePermissionGroupModel<GroupPermissionGlobalModel, PermissionKind>(b);
        });
        #endregion

        #region Blog
        builder.Entity<BlogPostModel>(b =>
        {
            b.ToTable(BlogPostModel.TableName).HasKey(e => e.Id);
            
            b.HasMany(e => e.Authors)
                .WithOne()
                .HasForeignKey(e => e.BlogPostId)
                .IsRequired();
        });
        builder.Entity<BlogPostAttachmentModel>(b =>
        {
            b.ToTable(BlogPostAttachmentModel.TableName).HasKey(e => new { e.BlogPostId, e.StorageFileId });

            b.HasOne(e => e.BlogPost)
                .WithMany()
                .HasForeignKey(e => e.BlogPostId)
                .IsRequired();
            
            b.HasOne(e => e.StorageFile)
                .WithMany()
                .HasForeignKey(e => e.StorageFileId)
                .IsRequired();
        });
        builder.Entity<BlogPostAuthorModel>(b =>
        {
            b.ToTable(BlogPostAuthorModel.TableName).HasKey(e => new { e.BlogPostId, e.UserId });
            
            b.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired();
        });
        builder.Entity<BlogPostTagModel>(b =>
        {
            b.ToTable(BlogPostTagModel.TableName).HasKey(e => new { e.BlogPostId, e.BlogTagId });

            b.HasOne(e => e.BlogPost)
                .WithMany()
                .HasForeignKey(e => e.BlogPostId)
                .IsRequired();

            b.HasOne(e => e.BlogTag)
                .WithMany()
                .HasForeignKey(e => e.BlogTagId)
                .IsRequired();

            b.Navigation(e => e.BlogTag).IsRequired();
        });
        builder.Entity<BlogTagModel>(b =>
        {
            b.ToTable(BlogTagModel.TableName).HasKey(e => e.Id);
        });
        #endregion

        // will be used in the future to replace GroupPermissions
        builder.Entity<ScopedApplicationRoleModel>(b =>
        {
            b.ToTable(ScopedApplicationRoleModel.TableName).HasKey(e => e.Id);

            b.HasIndex(r => new { r.NormalizedName, r.ApplicationId })
            .HasDatabaseName("ScopedApplicationRole_Name__ApplicationId_Index").IsUnique();
            b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();
            b.Property(u => u.Name).HasMaxLength(256);
            b.Property(u => u.NormalizedName).HasMaxLength(256);

            b.HasOne(e => e.Application)
                .WithMany()
                .HasForeignKey(e => e.ApplicationId)
                .IsRequired();
        });
    }
    private static void PerformBasePermissionGroupModel<TModel, TKind>(EntityTypeBuilder<TModel> b)
        where TKind : struct, Enum
        where TModel : BasePermissionGroupModel<TKind>
    {
        b.HasKey(e => e.Id);

        b.HasOne(e => e.Group)
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .IsRequired();
    }
}
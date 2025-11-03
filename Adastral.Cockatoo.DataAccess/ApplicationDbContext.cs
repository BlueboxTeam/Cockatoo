using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

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

    public DbSet<ServiceAccountModel> ServiceAccounts { get; set; }
    public DbSet<ServiceAccountTokenModel> ServiceAccountTokens { get; set; }

    public DbSet<ApplicationModel> Applications { get; set; }
    public DbSet<ApplicationSourceModModel> ApplicationSourceMods { get; set; }
    public DbSet<ApplicationBrandingModel> ApplicationBrands { get; set; }

    public DbSet<ApplicationBullseyeModel> ApplicationBullseye { get; set; }
    public DbSet<BullseyePatchModel> BullseyePatches { get; set; }
    public DbSet<BullseyeRevisionModel> BullseyeRevisions { get; set; }
    public DbSet<BullseyeV1CacheModel> BullseyeCacheV1 { get; set; }
    public DbSet<BullseyeV2CacheModel> BullseyeCacheV2 { get; set; }
    public DbSet<SouthbankCacheModel> SouthbankCache { get; set; }

    public DbSet<GroupModel> Groups { get; set; }
    public DbSet<GroupMembershipModel> GroupMemberships { get; set; }
    public DbSet<GroupPermissionApplicationModel> GroupApplicationPermissions { get; set; }
    public DbSet<GroupPermissionGlobalModel> GroupGlobalPermissions { get; set; }

    public DbSet<PermissionGroupModel> PermissionGroups { get; set; }
    public DbSet<PermissionGroupMembershipModel> PermissionGroupMemberships { get; set; }
    public DbSet<PermissionRoleModel> PermissionRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        #region Application
        builder.Entity<ApplicationModel>(b =>
        {
            b.ToTable(ApplicationModel.TableName).HasKey(e => e.Id);

            b.HasOne(e => e.Branding)
                .WithOne()
                .HasForeignKey<ApplicationBrandingModel>(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(true);

            b.HasOne(e => e.SourceMod)
                .WithOne()
                .HasForeignKey<ApplicationSourceModModel>(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(true);

            b.HasOne(e => e.SouthbankCache)
                .WithOne()
                .HasForeignKey<SouthbankCacheModel>(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });
        builder.Entity<ApplicationBrandingModel>(b =>
        {
            b.ToTable(ApplicationBrandingModel.TableName).HasKey(e => e.ApplicationId);
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
            b.ToTable(SouthbankCacheModel.TableName).HasKey(e => e.ApplicationId);
            b.HasIndex(e => e.CreatedAt).IsUnique(false).IsDescending(true);

            b.ComplexProperty(p => p.V1, d => d.ToJson());
            b.ComplexProperty(p => p.V2, d => d.ToJson());
            b.ComplexProperty(p => p.V3, d => d.ToJson());
        });
        #endregion
    }
}
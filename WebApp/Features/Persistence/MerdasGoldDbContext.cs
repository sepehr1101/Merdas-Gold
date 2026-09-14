using MerdasGold.Features.Authentication.Entities;
using MerdasGold.Features.Content.Entities;
using MerdasGold.Features.Catalog.Entities;
using MerdasGold.Features.OperationLogs.Entities;
using MerdasGold.Features.Settings.Entities;
using MerdasGold.Features.StoreInformation.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Persistence;

public sealed class MerdasGoldDbContext(DbContextOptions<MerdasGoldDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<OpLog> OpLogs => Set<OpLog>();
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();
    public DbSet<StorePolicy> StorePolicies => Set<StorePolicy>();
    public DbSet<PromotionBanner> PromotionBanners => Set<PromotionBanner>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductTag> ProductTags => Set<ProductTag>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<ProductAttributeDefinition> ProductAttributes => Set<ProductAttributeDefinition>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductPiece> ProductPieces => Set<ProductPiece>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<SecuritySettings> SecuritySettings => Set<SecuritySettings>();
    public DbSet<StoreProfile> StoreProfiles => Set<StoreProfile>();
    public DbSet<StoreBankAccount> StoreBankAccounts => Set<StoreBankAccount>();
    public DbSet<StoreWorkingHour> StoreWorkingHours => Set<StoreWorkingHour>();
    public DbSet<StoreLocation> StoreLocations => Set<StoreLocation>();
    public DbSet<StoreSocialNetwork> StoreSocialNetworks => Set<StoreSocialNetwork>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MerdasGoldDbContext).Assembly);
    }
}

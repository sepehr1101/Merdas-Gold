using MerdasGold.Features.Authentication.Entities;
using MerdasGold.Features.Customers.Entities;
using MerdasGold.Features.Orders.Entities;
using MerdasGold.Features.Catalog.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerdasGold.Features.Orders.Data;

public sealed class AddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> b)
    {
        b.ToTable("CustomerAddresses"); b.HasKey(x => x.Id);
        b.Property(x => x.CustomerId).HasMaxLength(450);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Mobile).HasMaxLength(11); b.Property(x => x.PostalCode).HasMaxLength(10);
        b.Property(x => x.RowVersion).IsRowVersion(); b.Ignore(x => x.FullAddress);
        b.HasIndex(x => x.CustomerId).IsUnique().HasFilter("[IsDefault] = 1");
    }
}
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("CustomerOrders"); b.HasKey(x => x.Id);
        b.Property(x => x.Number).HasMaxLength(40); b.HasIndex(x => x.Number).IsUnique();
        b.Property(x => x.CustomerId).HasMaxLength(450);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CustomerId, x.RequestId }).IsUnique();
        b.HasIndex(x => new { x.Status, x.ReservationExpiresUtc });
        b.Property(x => x.Recipient).HasMaxLength(150); b.Property(x => x.Mobile).HasMaxLength(11);
        b.Property(x => x.Address).HasMaxLength(800); b.Property(x => x.CustomerNote).HasMaxLength(500);
        b.Property(x => x.TrackingCode).HasMaxLength(100); b.Property(x => x.Total).HasPrecision(28, 4);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> b)
    {
        b.ToTable("CustomerOrderLines", t => t.HasCheckConstraint("CK_OrderLine_Quantity", "[Quantity] > 0")); b.HasKey(x => x.Id);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.ProductTitle).HasMaxLength(300); b.Property(x => x.ProductCode).HasMaxLength(100);
        b.Property(x => x.VariantTitle).HasMaxLength(300); b.Property(x => x.Size).HasMaxLength(100);
        b.Property(x => x.Weight).HasPrecision(18, 4); b.Property(x => x.GoldRateToman).HasPrecision(28, 4);
        b.Property(x => x.UnitPrice).HasPrecision(28, 4);
    }
}
public sealed class OrderEventConfiguration : IEntityTypeConfiguration<OrderEvent>
{
    public void Configure(EntityTypeBuilder<OrderEvent> b)
    {
        b.ToTable("CustomerOrderEvents"); b.HasKey(x => x.Id);
        b.Property(x => x.Actor).HasMaxLength(450); b.Property(x => x.Note).HasMaxLength(500);
    }
}

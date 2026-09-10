using MerdasGold.Features.OperationLogs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerdasGold.Features.OperationLogs.Data;

public sealed class OpLogConfiguration : IEntityTypeConfiguration<OpLog>
{
    public void Configure(EntityTypeBuilder<OpLog> builder)
    {
        builder.ToTable("OpLog");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.UserName).HasMaxLength(256).IsRequired();
        builder.Property(log => log.Description).HasMaxLength(2000).IsRequired();
        builder.Property(log => log.UserId).HasMaxLength(450).IsRequired();
        builder.Property(log => log.OperationDateTime).HasColumnType("datetime2").IsRequired();
        builder.Property(log => log.IpAddress).HasMaxLength(64).IsRequired();

        builder.HasIndex(log => log.OperationDateTime);
        builder.HasIndex(log => log.UserId);
    }
}

using MerdasGold.Features.Diagnostics.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerdasGold.Features.Diagnostics.Data;

public sealed class ApplicationErrorConfiguration : IEntityTypeConfiguration<ApplicationError>
{
    public void Configure(EntityTypeBuilder<ApplicationError> b)
    {
        b.ToTable("ApplicationErrors"); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Source).HasMaxLength(40); b.Property(x => x.Message).HasMaxLength(4000);
        b.Property(x => x.ExceptionType).HasMaxLength(300); b.Property(x => x.StackTrace).HasMaxLength(24000);
        b.Property(x => x.Path).HasMaxLength(1000); b.Property(x => x.Method).HasMaxLength(20);
        b.Property(x => x.TraceId).HasMaxLength(150); b.Property(x => x.UserId).HasMaxLength(450); b.Property(x => x.Environment).HasMaxLength(100);
        b.HasIndex(x => x.OccurredUtc); b.HasIndex(x => x.TraceId); b.HasIndex(x => new { x.Source, x.StatusCode, x.OccurredUtc });
    }
}

using MerdasGold.Features.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerdasGold.Features.Content.Data;
public sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> b)
    {
        b.ToTable("BlogPost");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(180).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.IsPublished, x.PublishedAtUtc });
        b.Property(x => x.Summary).HasMaxLength(600).IsRequired();
        b.Property(x => x.BodyJson).IsRequired();
        b.Property(x => x.ImageContentType).HasMaxLength(40);
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

using MerdasGold.Features.StoreInformation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerdasGold.Features.StoreInformation.Data;

public sealed class StoreProfileConfiguration : IEntityTypeConfiguration<StoreProfile>
{
    public void Configure(EntityTypeBuilder<StoreProfile> builder)
    {
        builder.ToTable("StoreProfile", table => table.HasCheckConstraint("CK_StoreProfile_SingleRow", "[Id] = 1"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Name).HasMaxLength(200).IsRequired();
        builder.Property(item => item.EnglishName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Tagline).HasMaxLength(300);
        builder.Property(item => item.ShortDescription).HasMaxLength(1000);
        builder.Property(item => item.BusinessCategory).HasMaxLength(150).IsRequired();
        builder.Property(item => item.ActivityStartDate).HasColumnType("date");
        builder.Property(item => item.LogoContentType).HasMaxLength(100);
        builder.Property(item => item.LogoFileName).HasMaxLength(260);
        builder.Property(item => item.FaviconContentType).HasMaxLength(100);
        builder.Property(item => item.FaviconFileName).HasMaxLength(260);
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasData(new { Id = 1, Name = "مرداس گلد", EnglishName = "Merdas Gold", Tagline = "", ShortDescription = "", BusinessCategory = "طلا و جواهر", IsActive = true });
    }
}

public sealed class StoreBankAccountConfiguration : IEntityTypeConfiguration<StoreBankAccount>
{
    public void Configure(EntityTypeBuilder<StoreBankAccount> builder)
    {
        builder.ToTable("StoreBankAccount");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.BankName).HasMaxLength(100).IsRequired();
        builder.Property(item => item.AccountHolderName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.AccountNumber).HasMaxLength(50).IsRequired();
        builder.Property(item => item.CardNumber).HasMaxLength(32).IsRequired();
        builder.Property(item => item.Iban).HasMaxLength(32).IsRequired();
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasIndex(item => item.IsDefault).HasFilter("[IsDefault] = 1").IsUnique();
    }
}

public sealed class StoreWorkingHourConfiguration : IEntityTypeConfiguration<StoreWorkingHour>
{
    public void Configure(EntityTypeBuilder<StoreWorkingHour> builder)
    {
        builder.ToTable("StoreWorkingHour", table => table.HasCheckConstraint("CK_StoreWorkingHour_DayOrder", "[DayOrder] BETWEEN 0 AND 6"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.DayName).HasMaxLength(20).IsRequired();
        builder.Property(item => item.StartTime).HasColumnType("time(0)");
        builder.Property(item => item.EndTime).HasColumnType("time(0)");
        builder.HasIndex(item => item.DayOrder).IsUnique();
        builder.HasData(
            Day(1, 0, "شنبه", true, 9, 21), Day(2, 1, "یکشنبه", true, 9, 21),
            Day(3, 2, "دوشنبه", true, 9, 21), Day(4, 3, "سه‌شنبه", true, 9, 21),
            Day(5, 4, "چهارشنبه", true, 9, 21), Day(6, 5, "پنجشنبه", true, 9, 18),
            Day(7, 6, "جمعه", false, null, null));
    }

    private static object Day(int id, int order, string name, bool open, int? start, int? end) => new
    {
        Id = id, DayOrder = order, DayName = name, IsOpen = open,
        StartTime = start.HasValue ? new TimeOnly(start.Value, 0) : (TimeOnly?)null,
        EndTime = end.HasValue ? new TimeOnly(end.Value, 0) : (TimeOnly?)null
    };
}

public sealed class StoreLocationConfiguration : IEntityTypeConfiguration<StoreLocation>
{
    public void Configure(EntityTypeBuilder<StoreLocation> builder)
    {
        builder.ToTable("StoreLocation", table =>
        {
            table.HasCheckConstraint("CK_StoreLocation_SingleRow", "[Id] = 1");
            table.HasCheckConstraint("CK_StoreLocation_Latitude", "[Latitude] BETWEEN -90 AND 90");
            table.HasCheckConstraint("CK_StoreLocation_Longitude", "[Longitude] BETWEEN -180 AND 180");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Address).HasMaxLength(1000);
        builder.Property(item => item.Latitude).HasPrecision(9, 6);
        builder.Property(item => item.Longitude).HasPrecision(9, 6);
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasData(new { Id = 1, Address = "", Latitude = 35.689200m, Longitude = 51.389000m, ZoomLevel = 13 });
    }
}

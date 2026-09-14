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
        builder.Property(item => item.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(item => item.Email).HasMaxLength(254).IsRequired();
        builder.Property(item => item.ActivityStartDate).HasColumnType("date");
        builder.Property(item => item.LogoContentType).HasMaxLength(100);
        builder.Property(item => item.LogoFileName).HasMaxLength(260);
        builder.Property(item => item.FaviconContentType).HasMaxLength(100);
        builder.Property(item => item.FaviconFileName).HasMaxLength(260);
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasData(new
        {
            Id = 1,
            Name = "مرداس گلد",
            EnglishName = "Merdas Gold",
            Tagline = "",
            ShortDescription = "طلای مرداس با هدف خلق تجربه‌ای متفاوت، مطمئن و شایسته در خرید طلای نو شکل گرفته است. تلاش ما بر این است که زیبایی، کیفیت و ارزش واقعی طلا را در کنار شفافیت در قیمت و اطلاعات محصول، به تجربه‌ای دلنشین و قابل اعتماد برای مشتریان تبدیل کنیم.\nمجموعه محصولات طلای مرداس با دقت انتخاب و عرضه می‌شوند تا پاسخگوی سلیقه‌های متفاوت باشند. شما می‌توانید محصولات را به‌صورت آنلاین از طریق وب‌سایت merdasgold.ir مشاهده و سفارش دهید یا برای خرید حضوری به فروشگاه طلای مرداس در بازار طلا و جواهر خورشید اصفهان مراجعه کنید.\nدر طلای مرداس، اعتماد شما ارزشمندترین سرمایه ماست. از انتخاب محصول تا خرید، پرداخت و دریافت سفارش، تلاش می‌کنیم تجربه‌ای شفاف، مطمئن و درخور یک انتخاب ماندگار برای شما فراهم کنیم.",
            BusinessCategory = "طلا و جواهر",
            PhoneNumber = "۰۹۱۳۳۳۸۸۸۱۹",
            Email = "info@merdasgold.ir",
            IsActive = true
        });
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
        builder.HasData(new { Id = 1, Address = "اصفهان، میدان امام علی، بازار طلا و جواهر خورشید، طبقه همکف، واحد ۱۲۰", Latitude = 32.667300m, Longitude = 51.688100m, ZoomLevel = 17 });
    }
}

public sealed class StoreSocialNetworkConfiguration : IEntityTypeConfiguration<StoreSocialNetwork>
{
    public void Configure(EntityTypeBuilder<StoreSocialNetwork> builder)
    {
        builder.ToTable("StoreSocialNetwork");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Key).HasMaxLength(30).IsRequired();
        builder.Property(item => item.DisplayName).HasMaxLength(50).IsRequired();
        builder.Property(item => item.Username).HasMaxLength(100).IsRequired();
        builder.Property(item => item.BaseUrl).HasMaxLength(300).IsRequired();
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasIndex(item => item.Key).IsUnique();
        builder.HasIndex(item => item.DisplayOrder).IsUnique();
        builder.HasData(
            Social(1, "instagram", "اینستاگرام", "merdasgold", "https://www.instagram.com/", 1),
            Social(2, "telegram", "تلگرام", "merdasgold2", "https://t.me/", 2),
            Social(3, "bale", "بله", "merdasgold", "https://ble.ir/", 3),
            Social(4, "eitaa", "ایتا", "merdasgoldd", "https://eitaa.com/", 4),
            Social(5, "rubika", "روبیکا", "merdasgold", "https://rubika.ir/", 5));
    }

    private static object Social(int id, string key, string name, string username, string baseUrl, int order) => new
    {
        Id = id, Key = key, DisplayName = name, Username = username, BaseUrl = baseUrl,
        DisplayOrder = order, IsActive = true
    };
}

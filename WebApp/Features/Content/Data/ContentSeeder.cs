using MerdasGold.Features.Content.Entities;
using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Content.Data;

public sealed class ContentSeeder(MerdasGoldDbContext dbContext, IWebHostEnvironment environment)
{
    private static readonly SeedSlide[] InitialSlides =
    [
        new("فـروشگاه مرداس گلد", "کالکشنی از بهترین جواهراتـــ", "6786867-min.png", 1),
        new("درخشش یک انتخاب", "طراحی‌های چشم‌نواز برای لحظه‌های خاص", "image-56-min.png", 2),
        new("ظرافتی از جنس طلا", "جواهری هماهنگ با سبک منحصربه‌فرد شما", "ertertrei-min.png", 3)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.PromotionBanners.AnyAsync(x => x.Placement == "home-slider", cancellationToken))
        {
            return;
        }

        foreach (var slide in InitialSlides)
        {
            var path = Path.Combine(environment.WebRootPath, "assets", "storefront", "images", slide.FileName);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"تصویر seed اسلایدر پیدا نشد: {path}");
            }

            dbContext.PromotionBanners.Add(new PromotionBanner
            {
                Title = slide.Title,
                Subtitle = slide.Subtitle,
                Placement = "home-slider",
                DisplayOrder = slide.DisplayOrder,
                IsActive = true,
                ImageData = await File.ReadAllBytesAsync(path, cancellationToken),
                ImageContentType = "image/png",
                ImageFileName = slide.FileName
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record SeedSlide(string Title, string Subtitle, string FileName, int DisplayOrder);
}

using MerdasGold.Features.Persistence;
using MerdasGold.Features.StoreInformation.Entities;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.StoreInformation.Data;

public sealed class StoreInformationSeeder(MerdasGoldDbContext dbContext, IWebHostEnvironment environment)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.Set<StoreProfile>()
            .SingleAsync(x => x.Id == StoreProfile.SingletonId, cancellationToken);

        if (profile.LogoData is not null)
            return;

        var logoPath = Path.Combine(environment.WebRootPath, "assets", "storefront", "brand", "merdas-logo.png");
        if (!File.Exists(logoPath))
            return;

        profile.LogoData = await File.ReadAllBytesAsync(logoPath, cancellationToken);
        profile.LogoContentType = "image/png";
        profile.LogoFileName = "merdas-logo.png";
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

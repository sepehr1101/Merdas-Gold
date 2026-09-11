using MudBlazor;

namespace MerdasGold.Features.Layout.Storefront;

public static class StorefrontTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#594ae2",
            Secondary = "#cdb181",
            Background = "#fdfcf9",
            Surface = "#ffffff",
            TextPrimary = "#21172d",
            TextSecondary = "#746f78",
            Divider = "#e8e2da",
            Error = "#a3314b"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["IRYekan", "Tahoma", "sans-serif"]
            }
        }
    };
}

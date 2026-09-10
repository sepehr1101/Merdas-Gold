using MudBlazor;

namespace MerdasGold.Features.Layout;

public static class MerdasTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#594ae2",
            Secondary = "#777482",
            Background = "#f3f2f7",
            Surface = "#ffffff",
            TextPrimary = "#252333",
            TextSecondary = "#777482",
            Divider = "#e8e6ee",
            Error = "#b42338"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Vazirmatn", "Tahoma", "sans-serif"]
            }
        }
    };
}

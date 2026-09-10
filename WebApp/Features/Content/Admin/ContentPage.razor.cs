using MerdasGold.Features.Content.Models;
using MerdasGold.Features.Content.Services;
using Microsoft.AspNetCore.Components;

namespace MerdasGold.Features.Content.Admin;

public partial class ContentPage
{
    [Inject] private ContentManagementService Service { get; set; } = default!;
    private ContentOverviewModel? _overview;
    private bool _loading = true;
    protected override async Task OnInitializedAsync()
    {
        try { _overview = await Service.GetOverviewAsync(); }
        finally { _loading = false; }
    }
}

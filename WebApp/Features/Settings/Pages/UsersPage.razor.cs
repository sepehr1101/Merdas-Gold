using MerdasGold.Features.Settings.Components;
using MerdasGold.Features.Settings.Models;
using MerdasGold.Features.Settings.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MiniExcelLibs;
using MudBlazor;

namespace MerdasGold.Features.Settings.Pages;

public partial class UsersPage
{
    [Inject] private UserAdministrationService UserAdministrationService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    private readonly Func<UserListItem, bool> _matchAll = _ => true;
    private readonly int[] _pageSizeOptions = [10, 25, 50, 100];
    private MudDataGrid<UserListItem>? _grid;
    private IReadOnlyList<UserListItem> _users = [];
    private IJSObjectReference? _downloadModule;
    private string? _searchText;
    private bool _isLoading = true;

    private Func<UserListItem, bool> QuickFilter => string.IsNullOrWhiteSpace(_searchText)
        ? _matchAll
        : user => Contains(user.UserName, _searchText)
                  || Contains(user.FullName, _searchText)
                  || Contains(user.PhoneNumber, _searchText)
                  || Contains(user.Email, _searchText)
                  || Contains(user.Roles, _searchText)
                  || Contains(user.Status, _searchText);

    private int FilteredCount => _grid?.FilteredItems.Count() ?? _users.Count;

    protected override async Task OnInitializedAsync() => await LoadUsersAsync();

    private async Task LoadUsersAsync()
    {
        _isLoading = true;

        try
        {
            _users = await UserAdministrationService.GetUsersAsync();
        }
        catch
        {
            Snackbar.Add("دریافت فهرست کاربران انجام نشد. دوباره تلاش کنید.", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void OnSearchChanged(string? value)
    {
        _searchText = value;
        if (_grid is not null)
        {
            _grid.NavigateTo(Page.First);
        }
    }

    private void OnFilterChanged(IReadOnlyCollection<IFilterDefinition<UserListItem>> _) => StateHasChanged();

    private void OpenFilters(MouseEventArgs _) => _grid?.OpenFilters();

    private void OpenColumnsPanel(MouseEventArgs args) => _grid?.ShowColumnsPanel(args);

    private async Task RefreshAsync(MouseEventArgs _)
    {
        await LoadUsersAsync();
        Snackbar.Add("اطلاعات جدول به‌روز شد.", Severity.Success);
    }

    private async Task OpenCreateUserDialog(MouseEventArgs _)
    {
        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small,
            CloseOnEscapeKey = true
        };
        var dialog = await DialogService.ShowAsync<CreateUserDialog>("افزودن کاربر", options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            await LoadUsersAsync();
            Snackbar.Add("کاربر جدید با موفقیت ساخته شد.", Severity.Success);
        }
    }

    private Task ExportCurrentPageAsync(MouseEventArgs _) => ExportAsync(currentPageOnly: true);

    private Task ExportFilteredAsync(MouseEventArgs _) => ExportAsync(currentPageOnly: false);

    private async Task ExportAsync(bool currentPageOnly)
    {
        if (_grid is null)
        {
            return;
        }

        _isLoading = true;

        try
        {
            var filteredUsers = _grid.FilteredItems
                .AsQueryable()
                .OrderBy(_grid.SortDefinitions.Values)
                .ToList();
            var exportUsers = currentPageOnly
                ? filteredUsers.Skip(_grid.CurrentPage * _grid.RowsPerPage).Take(_grid.RowsPerPage)
                : filteredUsers;

            var rows = exportUsers.Select(ToExcelRow).ToList();
            if (rows.Count == 0)
            {
                Snackbar.Add("داده‌ای برای خروجی گرفتن وجود ندارد.", Severity.Warning);
                return;
            }

            await using var stream = new MemoryStream();
            await MiniExcel.SaveAsAsync(stream, rows, sheetName: "کاربران");
            stream.Position = 0;

            _downloadModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./scripts/file-download.js");
            using var streamReference = new DotNetStreamReference(stream);
            var scope = currentPageOnly ? "صفحه" : "فیلترشده";
            var fileName = $"کاربران-{scope}-{DateTime.Now:yyyy-MM-dd-HHmm}.xlsx";
            await _downloadModule.InvokeVoidAsync("downloadFileFromStream", fileName, streamReference);
            Snackbar.Add("فایل Excel آماده شد.", Severity.Success);
        }
        catch
        {
            Snackbar.Add("ساخت فایل Excel انجام نشد. دوباره تلاش کنید.", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static Dictionary<string, object?> ToExcelRow(UserListItem user) => new()
    {
        ["نام کاربری"] = user.UserName,
        ["نام و نام خانوادگی"] = user.FullName,
        ["شماره موبایل"] = user.PhoneNumber,
        ["ایمیل"] = user.Email,
        ["نقش‌ها"] = user.Roles,
        ["وضعیت"] = user.Status
    };

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase);

    public async ValueTask DisposeAsync()
    {
        if (_downloadModule is null)
        {
            return;
        }

        try
        {
            await _downloadModule.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}

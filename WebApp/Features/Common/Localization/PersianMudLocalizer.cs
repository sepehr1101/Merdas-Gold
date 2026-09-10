using Microsoft.Extensions.Localization;
using MudBlazor;

namespace MerdasGold.Features.Common.Localization;

public sealed class PersianMudLocalizer : MudLocalizer
{
    private static readonly IReadOnlyDictionary<string, string> Translations =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MudDataGrid_Apply"] = "اعمال",
            ["MudDataGrid_Clear"] = "پاک کردن",
            ["MudDataGrid_AddFilter"] = "افزودن فیلتر",
            ["MudDataGrid_Cancel"] = "انصراف",
            ["MudDataGrid_Loading"] = "در حال بارگذاری…",
            ["MudDataGrid_Save"] = "ذخیره",
            ["MudDataGrid_HideAll"] = "پنهان کردن همه",
            ["MudDataGrid_ShowAll"] = "نمایش همه",
            ["MudDataGrid_Columns"] = "ستون‌ها",
            ["MudDataGrid_ExpandAllGroups"] = "باز کردن همه گروه‌ها",
            ["MudDataGrid_CollapseAllGroups"] = "بستن همه گروه‌ها",
            ["MudDataGrid_RefreshData"] = "بارگذاری مجدد",
            ["MudDataGrid_True"] = "بله",
            ["MudDataGrid_False"] = "خیر",
            ["MudDataGrid_Unsort"] = "حذف مرتب‌سازی",
            ["MudDataGrid_Filter"] = "فیلتر",
            ["MudDataGrid_Hide"] = "پنهان کردن",
            ["MudDataGrid_Ungroup"] = "حذف گروه‌بندی",
            ["MudDataGrid_CollapseGroup"] = "بستن گروه",
            ["MudDataGrid_ExpandGroup"] = "باز کردن گروه",
            ["MudDataGrid_Group"] = "گروه‌بندی",
            ["MudDataGrid_FilterValue"] = "مقدار فیلتر",
            ["MudDataGrid_Contains"] = "شامل باشد",
            ["MudDataGrid_NotContains"] = "شامل نباشد",
            ["MudDataGrid_Equals"] = "برابر باشد",
            ["MudDataGrid_NotEquals"] = "برابر نباشد",
            ["MudDataGrid_StartsWith"] = "شروع شود با",
            ["MudDataGrid_EndsWith"] = "پایان یابد با",
            ["MudDataGrid_IsEmpty"] = "خالی باشد",
            ["MudDataGrid_IsNotEmpty"] = "خالی نباشد",
            ["MudDataGrid_EqualSign"] = "برابر با",
            ["MudDataGrid_NotEqualSign"] = "نابرابر با",
            ["MudDataGrid_GreaterThanSign"] = "بزرگ‌تر از",
            ["MudDataGrid_GreaterThanOrEqualSign"] = "بزرگ‌تر یا برابر با",
            ["MudDataGrid_LessThanSign"] = "کوچک‌تر از",
            ["MudDataGrid_LessThanOrEqualSign"] = "کوچک‌تر یا برابر با",
            ["MudDataGrid_Is"] = "باشد",
            ["MudDataGrid_IsNot"] = "نباشد",
            ["MudDataGrid_IsAfter"] = "بعد از",
            ["MudDataGrid_IsOnOrAfter"] = "در یا بعد از",
            ["MudDataGrid_IsBefore"] = "قبل از",
            ["MudDataGrid_IsOnOrBefore"] = "در یا قبل از",
            ["MudDataGrid_Column"] = "ستون",
            ["MudDataGrid_Operator"] = "عملگر",
            ["MudDataGrid_Value"] = "مقدار",
            ["MudDataGrid_MoveDown"] = "انتقال به پایین",
            ["MudDataGrid_MoveUp"] = "انتقال به بالا",
            ["MudDataGrid_Sort"] = "مرتب‌سازی",
            ["MudDataGrid_ClearFilter"] = "پاک کردن فیلتر",
            ["MudDataGrid_OpenFilters"] = "باز کردن فیلترها",
            ["MudDataGrid_ToggleGroupExpansion"] = "باز یا بسته کردن گروه",
            ["MudDataGrid_RemoveFilter"] = "حذف فیلتر",
            ["MudDataGrid_SelectRow"] = "انتخاب ردیف",
            ["MudDataGrid_SelectAllRows"] = "انتخاب همه ردیف‌ها",
            ["MudDataGrid_ShowColumnOptions"] = "تنظیمات ستون",
            ["MudDataGridPager_RowsPerPage"] = "تعداد رکورد در صفحه:",
            ["MudDataGridPager_AllItems"] = "همه",
            ["MudDataGridPager_InfoFormat"] = "{0} تا {1} از {2}",
            ["MudDataGridPager_FirstPage"] = "صفحه اول",
            ["MudDataGridPager_PreviousPage"] = "صفحه قبل",
            ["MudDataGridPager_NextPage"] = "صفحه بعد",
            ["MudDataGridPager_LastPage"] = "صفحه آخر"
        };

    public override LocalizedString this[string key] =>
        Translations.TryGetValue(key, out var value)
            ? new LocalizedString(key, value, resourceNotFound: false)
            : new LocalizedString(key, key, resourceNotFound: true);

    public override LocalizedString this[string key, params object[] arguments]
    {
        get
        {
            var translation = this[key];
            return translation.ResourceNotFound
                ? translation
                : new LocalizedString(
                    key,
                    string.Format(translation.Value, arguments),
                    resourceNotFound: false);
        }
    }
}

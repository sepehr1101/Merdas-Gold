# معماری پروژه مرداس گلد

این سند مرجع تصمیم‌های معماری پروژه است و هم‌زمان با رشد پروژه به‌روزرسانی می‌شود.

## اهداف

- حفظ سادگی و خوانایی پروژه
- توسعه تدریجی قابلیت‌ها
- نگهداری تمام کدهای برنامه در یک پروژه و یک DLL
- دسته‌بندی کدها بر اساس قابلیت کسب‌وکار (Feature-based)
- جلوگیری از abstraction، الگو و وابستگی‌ای که مسئله واقعی پروژه را حل نمی‌کند

## تصمیم اصلی

پروژه به‌صورت یک **Feature-based Monolith** پیاده‌سازی می‌شود:

```text
یک Blazor Web App
└── یک فایل MerdasGold.csproj
    └── یک DLL خروجی
        └── چند Feature مستقل در سطح پوشه و namespace
```

این ساختار از تجربه پروژه [DNTips](https://github.com/VahidN/DntSite) الهام می‌گیرد، اما قرار نیست ساختار یا کد آن مو‌به‌مو کپی شود. قواعد رسمی Blazor و EF Core و نیازهای واقعی مرداس گلد نیز در تصمیم‌ها لحاظ می‌شوند.

## فناوری و مدل اجرا

- ASP.NET Core Blazor Web App
- Interactive Server برای تعاملات Blazor
- EF Core با یک `MerdasGoldDbContext`
- یک دیتابیس برای کل برنامه
- استفاده از `IDbContextFactory<MerdasGoldDbContext>` برای ساخت DbContext کوتاه‌عمر
- عدم ایجاد پروژه Client، Razor Class Library یا Class Library تا زمانی که نیاز واقعی وجود نداشته باشد

وابستگی‌های فعلی:

- `MudBlazor` برای کامپوننت‌های UI
- `DNTPersianUtils.Core` برای تبدیل، نمایش و اعتبارسنجی تاریخ و داده‌های فارسی
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` برای مدیریت کاربران، نقش‌ها و ورود محلی
- `Microsoft.EntityFrameworkCore.SqlServer` به‌عنوان provider دیتابیس SQL Server
- `Microsoft.EntityFrameworkCore.Design` فقط برای ابزارهای migration و design-time
- `MiniExcel` برای ساخت خروجی Excel جداول به‌صورت کم‌حافظه و بدون وابستگی به Microsoft Office
- `Leaflet 1.9.4` برای انتخاب و نمایش موقعیت فروشگاه روی نقشه OpenStreetMap؛ فایل‌های ثابت کتابخانه داخل `wwwroot/lib/leaflet` نگهداری می‌شوند تا رابط نقشه به CDN وابسته نباشد.

## تم و رابط کاربری

- رنگ اصلی برند `#594ae2` است و توکن‌های مشترک در `wwwroot/theme.css` نگهداری می‌شوند.
- تم MudBlazor در `Features/Layout/MerdasTheme.cs` با همین رنگ‌ها هماهنگ می‌شود.
- فونت پیش‌فرض برنامه `Vazirmatn` است و فایل آن به‌صورت محلی از `wwwroot/assets/fonts` ارائه می‌شود.
- صفحه ورود در Feature مستقل `Features/Authentication` قرار دارد و از layout اختصاصی و بدون پوسته پنل استفاده می‌کند.
- صفحه ورود به ASP.NET Core Identity متصل است و Cookie احراز هویت را از endpoint سمت سرور دریافت می‌کند.
- مسیرهای پنل مدیریت به نقش `Administrator` محدود شده‌اند.

## ساختار کلی

```text
MerdasGold/
├── MerdasGold.csproj
├── Program.cs
├── appsettings.json
│
├── Features/
│   ├── App.razor
│   ├── App.razor.cs
│   ├── Routes.razor
│   ├── Routes.razor.cs
│   ├── _Imports.razor
│   │
│   ├── Layout/
│   ├── Common/
│   ├── Persistence/
│   ├── ServicesConfigs/
│   │
│   ├── Home/
│   ├── UserProfiles/
│   ├── Catalog/
│   ├── Pricing/
│   ├── ShoppingCart/
│   ├── Orders/
│   ├── Wishlists/
│   ├── Comparisons/
│   └── Blog/
│
└── wwwroot/
    ├── css/
    ├── fonts/
    ├── images/
    └── scripts/
```

Featureها به‌تدریج و در زمان پیاده‌سازی نیاز مربوطه به پروژه اضافه می‌شوند.

## ساختار ثابت هر Feature

هر Feature از ابتدای ایجاد، ساختار دسته‌بندی‌شده زیر را خواهد داشت:

```text
Features/
└── Catalog/
    ├── Components/
    ├── Pages/
    ├── Admin/
    ├── Entities/
    ├── Models/
    ├── Data/
    ├── Services/
    ├── Mappings/
    └── Routing/
```

کاربرد پوشه‌ها:

- `Components`: کامپوننت‌های Blazor قابل استفاده مجدد در همان Feature
- `Pages`: صفحات عمومی و route‌دار Feature
- `Admin`: صفحات و کامپوننت‌های مدیریتی همان Feature
- `Entities`: موجودیت‌های EF Core و منطق ذاتی آن‌ها
- `Models`: مدل‌های فرم، نمایش و ورودی/خروجی
- `Data`: تنظیمات EF Core و کدهای persistence مخصوص Feature
- `Services`: عملیات و منطق کاربردی Feature
- `Mappings`: تبدیل Entityها و Modelها به یکدیگر
- `Routing`: ثابت‌ها و الگوهای مسیریابی Feature

خالی بودن موقت بعضی پوشه‌ها قابل قبول است. هدف، داشتن یک قرارداد ثابت و قابل پیش‌بینی برای تمام Featureها است.

## نمونه Feature کاتالوگ

```text
Features/
└── Catalog/
    ├── Components/
    │   ├── ProductCard.razor
    │   └── ProductFilters.razor
    │
    ├── Pages/
    │   ├── ProductsPage.razor
    │   ├── ProductsPage.razor.cs
    │   ├── ProductDetailsPage.razor
    │   └── ProductDetailsPage.razor.cs
    │
    ├── Admin/
    │   ├── ManageProductsPage.razor
    │   ├── ManageProductsPage.razor.cs
    │   └── ProductForm.razor
    │
    ├── Entities/
    │   ├── Product.cs
    │   ├── ProductCategory.cs
    │   └── ProductImage.cs
    │
    ├── Models/
    │   ├── ProductListItem.cs
    │   └── ProductEditModel.cs
    │
    ├── Data/
    │   ├── ProductConfiguration.cs
    │   └── ProductCategoryConfiguration.cs
    │
    ├── Services/
    │   └── ProductService.cs
    │
    ├── Mappings/
    │   └── ProductMappings.cs
    │
    └── Routing/
        └── CatalogRoutes.cs
```

## قواعد فایل‌های Blazor

- رابط کاربری هر کامپوننت در فایل `.razor` نوشته می‌شود.
- منطق قابل توجه کامپوننت در فایل code-behind با پسوند `.razor.cs` قرار می‌گیرد.
- CSS اختصاصی کامپوننت در فایل `.razor.css` کنار همان کامپوننت قرار می‌گیرد.
- کامپوننت ساده می‌تواند فقط یک فایل `.razor` داشته باشد.
- Entity، Model، Service، EF Configuration و سایر کدهای غیر UI فایل معمولی `.cs` هستند.

نمونه:

```text
ProductDetailsPage.razor
ProductDetailsPage.razor.cs
ProductDetailsPage.razor.css
```

## پنل مدیریت

پنل مدیریت به‌عنوان یک Feature افقی و جدا از قابلیت‌های کسب‌وکار ساخته نمی‌شود. بخش مدیریتی هر قابلیت در پوشه `Admin` همان Feature نگهداری می‌شود:

```text
Catalog/Admin/ManageProductsPage.razor
Orders/Admin/ManageOrdersPage.razor
UserProfiles/Admin/ManageUsersPage.razor
Blog/Admin/ManageArticlesPage.razor
```

موارد مشترک پوسته پنل مدیریت، مانند layout و navigation، در `Features/Layout` قرار می‌گیرند.

## دسترسی به داده

- کل برنامه یک `MerdasGoldDbContext` دارد.
- دیتابیس اصلی برنامه SQL Server است.
- migrationها در `Features/Persistence/Migrations` قرار می‌گیرند.
- تنظیم EF Core هر Entity در پوشه `Data` همان Feature قرار می‌گیرد.
- تنظیمات Entityها با `ApplyConfigurationsFromAssembly` در DbContext اعمال می‌شوند.
- سرویس‌ها از `IDbContextFactory<MerdasGoldDbContext>` استفاده می‌کنند و برای هر عملیات یک DbContext کوتاه‌عمر می‌سازند.
- برای EF Core، Generic Repository یا Unit of Work سفارشی ایجاد نمی‌شود.

## کاتالوگ و موجودی کالا

- هر نصب مرداس متعلق به یک فروشگاه و یک دیتابیس مستقل است؛ بنابراین در جداول کاتالوگ `TenantId` نگهداری نمی‌شود.
- `Product` صفحه و طرح مشترک کالا را نگه می‌دارد؛ دسته اصلی، برچسب‌ها، محتوا و تصاویر در این سطح هستند.
- `ProductVariant` انتخاب قابل مشاهده برای مشتری، مانند رنگ یا سایز، است.
- `ProductPiece` قطعه فیزیکی موجود در مغازه است و کد رهگیری، وزن دقیق طلای آن قطعه، وزن اختیاری سنگ و وضعیت موجودی را نگه می‌دارد. چند قطعه هم‌شکل با وزن متفاوت زیر یک محصول و تنوع قرار می‌گیرند.
- `ProductType` الگوی قابل استفاده مجدد هر نوع کالا است. اتصال الگو به `ProductAttributeDefinition` تعیین می‌کند هر ویژگی در سطح محصول یا تنوع نمایش داده شود.
- مقدار ویژگی‌ها در MVP به‌صورت رشته نگهداری می‌شود، اما نوع ورودی و گزینه‌های مجاز از تعریف ویژگی می‌آیند؛ این انتخاب افزودن ویژگی‌های تازه را بدون تغییر schema ممکن می‌کند.
- دسته‌بندی برای ساخت مسیر مرور فروشگاه و برچسب برای مجموعه‌های منعطف و موقت استفاده می‌شود؛ این دو مفهوم جای یکدیگر را نمی‌گیرند.
- قیمت در موجودیت‌های کاتالوگ ذخیره نمی‌شود. فرمول وزن، نرخ طلا، اجرت، سنگ، مالیات و تخفیف در Feature مستقل «فروش و قیمت‌گذاری» پیاده‌سازی خواهد شد.
- در MVP فقط یک موجودی فروشگاهی وجود دارد؛ انبار چندمکانه، ورود گروهی، تاریخچه گردش موجودی و قیمت‌گذاری پویا به فازهای بعد موکول شده‌اند.

## تست‌های Integration و دیتابیس تست

- تست‌های Integration روی SQL Server واقعی اجرا می‌شوند، نه provider جایگزین.
- SQL Server تست برای هر اجرای تست داخل Docker بالا می‌آید و پس از پایان تست‌ها حذف می‌شود.
- استفاده از SQLite، از جمله SQLite in-memory، در تست‌های Integration مجاز نیست؛ زیرا رفتار آن با SQL Server یکسان نیست.
- هنگام ایجاد پروژه تست، از `Testcontainers.MsSql` برای مدیریت چرخه عمر container استفاده می‌شود.
- migrationهای واقعی برنامه روی دیتابیس تست اجرا می‌شوند تا سازگاری schema نیز بررسی شود.
- Docker باید روی سیستم توسعه و محیط CI در دسترس باشد.

## تنظیمات امنیتی و لاگ عملیات

- تنظیمات قابل ویرایش سیاست رمز عبور، مسدودی حساب و مدت نشست در جدول تک‌رکوردی `SecuritySettings` نگهداری می‌شوند.
- شناسه رکورد تنظیمات امنیتی همیشه `1` است و ایجاد یا حذف آن از رابط کاربری مجاز نیست.
- تغییرات `SecuritySettings` و رکورد متناظر آن در جدول append-only با نام `OpLog` داخل یک transaction ذخیره می‌شوند.
- `OpLog` نام کاربر، شناسه Identity کاربر، شرح دقیق تغییر، زمان UTC عملیات و IP درخواست را به‌صورت snapshot نگهداری می‌کند.
- برای حفظ سابقه در صورت حذف احتمالی کاربر در آینده، روی `OpLog.UserId` کلید خارجی حذف‌شونده به `AspNetUsers` تعریف نمی‌شود.
- مقادیر `SecuritySettings` هنگام شروع برنامه و پس از هر ویرایش معتبر روی سیاست‌های Identity اعمال می‌شوند؛ مدت نشست جدید برای ورودها و تمدیدهای بعدی مؤثر است.
- هیچ رمز، token یا کلید محرمانه‌ای در `SecuritySettings` یا `OpLog` ذخیره نمی‌شود و در این مرحله Key Vault نیز به پروژه متصل نیست.
- صفحه `/admin/settings/audit-logs` در `Features/OperationLogs/Admin` فقط برای مدیر سیستم و به‌صورت خواندنی ارائه می‌شود. این صفحه سوابق موجود را نمایش می‌دهد و امکان افزودن، ویرایش یا حذف لاگ ندارد.
- فیلتر تاریخ، ورودی شمسی با ارقام فارسی/عربی/انگلیسی می‌پذیرد؛ بازه پیش‌فرض ۳۰ روز اخیر است و خالی بودن هر تاریخ به معنی نبودن آن مرز است. مرزهای روز بر اساس منطقه زمانی `Asia/Tehran` به UTC تبدیل و مستقیماً در SQL اعمال می‌شوند؛ انتهای بازه، ابتدای روز بعد به‌صورت exclusive است.
- جدول از `DataGridToolbar` مشترک برای جستجو، فیلتر ستون‌ها، مرتب‌سازی چندستونی، نمایش/ترتیب ستون‌ها، صفحه‌بندی، تازه‌سازی و خروجی Excel استفاده می‌کند. خروجی صفحه فعلی و تمام داده‌های فیلترشده، ترتیب فعلی جدول را حفظ می‌کنند. تعداد کل مربوط به بازه تاریخی انتخاب‌شده است.
- داده‌های بازه انتخابی با `AsNoTracking` خوانده می‌شوند و امکانات جدول مانند جدول کاربران روی همان مجموعه اعمال می‌شوند؛ برای حجم‌های بسیار بزرگ در آینده باید صفحه‌بندی و فیلترهای ستون نیز به سرور منتقل شوند.
- تست‌های واحد تبدیل تقویم و مرز زمانی در `Tests/MerdasGold.Tests` قرار دارند و به دیتابیس نیاز ندارند.

## اطلاعات فروشگاه

- اطلاعات پایه، حساب‌های بانکی، ساعت کاری و موقعیت جغرافیایی به‌ترتیب در جدول‌های `StoreProfile`، `StoreBankAccount`، `StoreWorkingHour` و `StoreLocation` نگهداری می‌شوند.
- `StoreProfile` و `StoreLocation` تک‌رکوردی هستند؛ حساب بانکی چندرکوردی است و فقط یک حساب می‌تواند پیش‌فرض باشد.
- لوگو و Favicon پس از کنترل حجم، MIME type و امضای واقعی فایل در SQL Server ذخیره می‌شوند؛ SVG برای جلوگیری از اجرای محتوای فعال پذیرفته نمی‌شود.
- افزودن حساب بانکی و تمام ویرایش‌های اطلاعات فروشگاه همراه با `OpLog` در یک transaction ثبت می‌شوند.
- نقشه با Leaflet و tileهای استاندارد OpenStreetMap نمایش داده می‌شود و attribution همیشه روی نقشه باقی می‌ماند؛ قابلیت دانلود انبوه یا offline tile وجود ندارد.

## Dependency Injection

- تا زمانی که تعداد ثبت‌ها کم است، سرویس‌ها به‌صورت صریح ثبت می‌شوند.
- برای هر کلاس به‌صورت خودکار interface ساخته نمی‌شود.
- concrete service در صورت داشتن تنها یک پیاده‌سازی مستقیماً تزریق می‌شود.
- اگر ثبت سرویس‌های یک Feature زیاد شد، یک extension method مانند `AddCatalog` برای همان Feature ایجاد می‌شود.
- فعلاً از assembly scanning و marker interface برای ثبت جادویی سرویس‌ها استفاده نمی‌شود.

## زمان مناسب ایجاد Interface

Interface زمانی ایجاد می‌شود که حداقل یکی از شرایط زیر برقرار باشد:

- بیش از یک پیاده‌سازی واقعی وجود دارد.
- کد با یک سرویس خارجی یا مرز قابل تعویض ارتباط دارد.
- برای تست یک مرز مهم کسب‌وکار به جایگزین واقعی نیاز داریم.

نمونه مرزهای مناسب:

```text
IPaymentGateway
IGoldPriceProvider
ISmsSender
IFileStorage
```

وجود `IProductService` در کنار `ProductService` بدون دلیل مشخص، جزو قرارداد معماری پروژه نیست.

## مواردی که به‌صورت پیش‌فرض استفاده نمی‌شوند

- Generic Repository
- Unit of Work سفارشی روی EF Core
- MediatR
- CQRS برای عملیات ساده
- AutoMapper
- interface برای هر Service
- BaseService یا BaseRepository عمومی
- DTO جداگانه برای هر لایه بدون تفاوت واقعی
- پروژه‌های جداگانه Domain، Application و Infrastructure
- Microservice

هرکدام از این موارد فقط با یک نیاز واقعی و ثبت دلیل آن در همین سند وارد پروژه خواهند شد.

## قانون وابستگی Featureها

- هر Feature مالک UI، مدل‌ها، داده و منطق مربوط به خودش است.
- Featureها نباید مستقیماً وارد جزئیات داخلی یکدیگر شوند.
- کد واقعاً مشترک در `Features/Common` قرار می‌گیرد.
- `Common` نباید به محل انتقال فایل‌های بدون مالک مشخص تبدیل شود.
- اگر ارتباط بین دو Feature ساده و داخلی است، استفاده مستقیم از سرویس عمومی آن Feature قابل قبول است.
- Event، interface یا abstraction واسط فقط زمانی اضافه می‌شود که coupling موجود واقعاً مشکل ایجاد کرده باشد.

## اصل راهنما

برای اضافه کردن هر abstraction باید پاسخ روشنی برای این سؤال وجود داشته باشد:

> این abstraction اکنون چه مسئله‌ای را حل می‌کند؟

اگر پاسخ فقط «ممکن است در آینده لازم شود» باشد، آن abstraction فعلاً اضافه نمی‌شود.

## مدیریت محتوا

- Feature محتوا در `Features/Content` مالک سؤالات پرتکرار، سیاست‌های فروشگاه و اسلایدر/بنر است.
- پنج سند ثابت «قوانین فروشگاه»، «شیوه ارسال»، «روش‌های پرداخت»، «شرایط مرجوعی» و «حریم خصوصی و تعهدات حقوقی» به‌صورت رکوردهای اولیه ایجاد می‌شوند و حذف یا تغییر عنوان آن‌ها از پنل مجاز نیست.
- متن قوانین و پاسخ‌ها فعلاً به‌صورت متن ساده ذخیره می‌شود تا در فاز بعد ویرایشگر غنی مانند TinyMCE روی همین مدل اضافه شود.
- تصاویر بنر پس از بررسی حجم، MIME type و امضای واقعی PNG/JPEG/WebP در SQL Server ذخیره می‌شوند؛ حداکثر حجم هر تصویر ۵ مگابایت است.
- افزودن، ویرایش و حذف محتوای مدیریتی در `OpLog` ثبت می‌شود و ویرایش‌ها با `rowversion` در برابر تغییر هم‌زمان محافظت می‌شوند.

خلاصه تصمیم پروژه:

```text
ساختار پوشه‌ها از ابتدا منظم و ثابت است؛
اما کلاس‌ها، interfaceها و الگوها فقط هنگام نیاز واقعی اضافه می‌شوند.
```

## صفحه درباره نرم‌افزار

- مسیر اختصاصی `/admin/settings/about` در `Features/Settings/Pages/AboutPage.razor` قرار دارد و مانند سایر تنظیمات به نقش `Administrator` محدود است.
- نام موقت محصول «مرداس» و سطوح نمایشی لایسنس «زمرد، یاقوت، الماس» هستند؛ مقدار فعلی «الماس» است. این مقادیر فعلاً معرفی محصول هستند و اعتبارسنجی، فعال‌سازی یا محدودسازی قابلیت‌های تجاری را پیاده‌سازی نمی‌کنند.
- نسخه از Assembly برنامه خوانده می‌شود. انتخاب کارت‌ها فقط جزئیات همان سطح را نمایش می‌دهد و لایسنس فعلی را تغییر نمی‌دهد.
- تصویر الماس SVG داخلی است؛ رنگ‌ها از توکن‌های مشترک تم، انیمیشن‌ها از CSS ایزوله و چیدمان از breakpointهای واکنش‌گرا استفاده می‌کنند. حالت `prefers-reduced-motion`، کنترل با صفحه‌کلید و اعلام تغییر جزئیات به صفحه‌خوان پشتیبانی می‌شوند.

## منابع مرجع

- [DNTips source code](https://github.com/VahidN/DntSite)
- [ASP.NET Core Blazor project structure](https://learn.microsoft.com/aspnet/core/blazor/project-structure)
- [ASP.NET Core feature organization](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/develop-asp-net-core-mvc-apps)
- [Blazor with EF Core](https://learn.microsoft.com/aspnet/core/blazor/blazor-ef-core)
- [Common web application architectures](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

## فروش و قیمت‌گذاری و خطاهای برنامه

- Featureهای `Pricing` و `Diagnostics` به همان پروژه و دیتابیس اضافه شده‌اند؛ جزئیات پیاده‌سازی، راه‌اندازی و حدود MVP در [PRICING-AND-DIAGNOSTICS.md](PRICING-AND-DIAGNOSTICS.md) آمده است.
- قیمت‌گذاری نسخه اول مخصوص طلای ۱۸ عیار است. نرخ از Provider ثابت Navasan با HttpClient و BackgroundService گرفته می‌شود؛ نتیجه هر درخواست در GoldRates و آخرین نرخ معتبر در IMemoryCache نگهداری می‌شود. RateSettings کلید رمز‌شده با Data Protection، زمان‌بندی و نرخ دستی موقت را نگه می‌دارد.
- PricingRules و PriceDiscounts تنظیمات قابل تغییر توسط مدیر را نگه می‌دارند. قواعد، تخفیف و تنظیمات با rowversion و OpLog ذخیره می‌شوند. محاسبات مالی از decimal استفاده می‌کنند.
- صورتحساب فعلی نمونه قابل چاپ است؛ سفارش و پرداخت هنوز وجود ندارد و صدور فاکتور واقعی یا تثبیت قیمت پرداخت در این فاز انجام نمی‌شود.
- خطاهای HTTP با middleware و خطاهای تعاملی با ErrorBoundary/ILoggerProvider محدود دریافت می‌شوند. خطا ابتدا در فایل خصوصی خارج از wwwroot نوشته می‌شود و worker آن را به ApplicationErrors منتقل می‌کند؛ قطع دیتابیس موجب حذف فایل صف نمی‌شود.
- صفحه `/admin/settings/errors` فقط برای Administrator است و فیلتر و صفحه‌بندی را در SQL انجام می‌دهد. صفحات عمومی خطا جزئیات فنی را نمایش نمی‌دهند. مسیر ناموجود کد HTTP 404 را حفظ می‌کند.

## مدیریت تصاویر محصول

- در مرحله تصاویر ویرایش محصول، حذف تصویر با تأیید کاربر و انتخاب تصویر شاخص از میان تصاویر موجود پشتیبانی می‌شود.
- حذف، انتخاب شاخص و بارگذاری روی رکورد والد Product قفل تراکنشی می‌گیرند تا تغییرات هم‌زمان تصاویر همان محصول مرتب اجرا شوند. انتخاب شاخص قبلی پیش از شاخص جدید برداشته می‌شود تا index یکتای تصویر شاخص رعایت شود.
- درخواست حذف/انتخاب با ProductId و ImageId بررسی می‌شود؛ تصویر محصول دیگر قابل تغییر نیست. rowversion محصول، تب قدیمی را از بازنویسی تغییر تازه بازمی‌دارد.
- حذف تصویر شاخص، نخستین تصویر باقی‌مانده با ترتیب DisplayOrder و سپس Id را جایگزین می‌کند؛ محصول بدون تصویر نیز مجاز است.
- تغییر تصاویر، UpdatedAtUtc و OpLog را در همان تراکنش به‌روز می‌کند. endpointها به Administrator محدود و با antiforgery محافظت می‌شوند.

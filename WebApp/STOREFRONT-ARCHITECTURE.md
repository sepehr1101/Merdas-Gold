# معماری Storefront مرداس گلد

این سند مرجع تصمیم‌های فعلی برای پیاده‌سازی تدریجی صفحات مشتریان با Blazor است. تصمیم‌های عمومی پروژه همچنان در [ARCHITECTURE.md](ARCHITECTURE.md) نگهداری می‌شوند.

## وضعیت و مرجع بصری

- Storefront در همان Blazor Web App فعلی و با مدل اجرای Interactive Server پیاده‌سازی می‌شود.
- نمونه تأییدشده فعلی برای شروع صفحه اصلی، `Medalion1/index-fifth.html` است.
- فایل HTML فقط مرجع بصری و رفتاری است؛ قرار نیست به‌صورت یک فایل بزرگ داخل Razor کپی و نگهداری شود.
- ظاهر ابتدا به Razor استاتیک با خروجی نزدیک به نمونه تبدیل می‌شود و سپس هر بخش جداگانه به داده واقعی متصل خواهد شد.
- جهت طراحی Storefront آرام، ممتاز، معاصر و محصول‌محور است. رابط نباید شبیه داشبورد یا Landing Page پرزرق‌وبرق شود.
## تفکیک Design System ادمین و Storefront

پنل ادمین و Storefront دو سطح نمایش مستقل با مخاطب، هدف و تراکم متفاوت هستند. لازم نیست typography، spacing، padding، اندازه کنترل‌ها، radius، shadow، palette یا breakpointهای آن‌ها یکسان باشد.

- Admin رابطی حرفه‌ای، قابل پیش‌بینی و نسبتاً متراکم برای انجام عملیات روزمره است.
- Storefront رابطی آرام‌تر، تصویرمحور، لمسی‌تر و مناسب تصمیم‌گیری مشتری است.
- Storefront می‌تواند theme و توکن‌های مستقل خود را در فایل یا محدوده‌ای مانند `storefront-theme.css` و `.storefront-shell` داشته باشد.
- توکن‌های Storefront نباید مقادیر Admin را بازنویسی کنند و استایل‌های Admin نیز نباید به صفحات عمومی نشت کنند.
- اشتراک‌گذاری فقط برای موارد واقعاً مشترک مانند هویت پایه برند، قواعد دسترس‌پذیری یا reset عمومی انجام می‌شود؛ حتی فونت و رنگ اصلی نیز در صورت تصمیم طراحی می‌توانند بین دو سطح متفاوت باشند.
- استقلال دو Design System به معنی ایجاد چند سیستم موازی داخل خود Storefront نیست؛ تمام صفحات عمومی باید از یک مجموعه توکن و primitive منسجم Storefront استفاده کنند.
- اگر MudBlazor در هر دو سطح استفاده شود، themeهای جدا مانند `AdminTheme` و `StorefrontTheme` مجاز و ترجیحی هستند و هر Layout theme مربوط به خودش را اعمال می‌کند.

## مرز Featureها

مرز اصلی پوشه‌ها قابلیت کسب‌وکار است، نه نوع کاربر یا محل نمایش. `Storefront` یک Feature کسب‌وکاری سراسری نیست؛ بنابراین ساختاری مانند `Features/Storefront/Catalog` ایجاد نمی‌شود.

هر Feature در صورت نیاز می‌تواند دو سطح نمایش داشته باشد:

```text
Features/
└── Catalog/
    ├── Admin/
    ├── Storefront/
    │   ├── Pages/
    │   └── Components/
    ├── Models/
    ├── Services/
    ├── Entities/
    ├── Data/
    └── Routing/
```

- `Admin`: صفحات و کامپوننت‌های مدیریتی همان Feature
- `Storefront`: صفحات و کامپوننت‌های مشتری‌محور همان Feature
- `Pages`: صفحات routeدار
- `Components`: اجزای قابل استفاده مجدد همان Feature
- `Models`: مدل‌های فرم یا نمایش؛ مدل ادمین و Storefront در صورت تفاوت جدا می‌شوند
- `Services`: عملیات و queryهای متعلق به همان Feature
- `Routing`: فقط زمانی که چند مسیر یا منطق مسیریابی واقعی وجود دارد

همه Featureها الزاماً هر دو سطح را ندارند. برای مثال `ShoppingCart` فقط Storefront و `Diagnostics` فقط Admin دارد؛ `Catalog` و `Orders` هر دو را خواهند داشت.

## Home به‌عنوان Composition Feature

`Home` مالک داده Catalog، Content، Pricing یا StoreInformation نیست؛ فقط خروجی عمومی آن‌ها را در صفحه اصلی کنار هم قرار می‌دهد.

```text
Features/
└── Home/
    ├── Pages/
    │   ├── HomePage.razor
    │   ├── HomePage.razor.cs
    │   └── HomePage.razor.css
    ├── Components/
    │   ├── HeroSlider.razor
    │   ├── FeaturedCategoriesSection.razor
    │   ├── StoreIntroductionSection.razor
    │   ├── FeaturedProductsSection.razor
    │   ├── BrandStorySection.razor
    │   └── LatestArticlesSection.razor
    ├── Models/
    │   └── HomePageModel.cs
    └── Services/
        └── HomePageService.cs
```

`HomePage.razor` در `Pages` قرار می‌گیرد چون routeدار است. اجزای سکشن‌های صفحه داخل `Components` قرار می‌گیرند.

کامپوننتی که فقط چینش یک سکشن صفحه اصلی را مشخص می‌کند متعلق به Home است، اما جزء کسب‌وکاری قابل استفاده مجدد در Feature مالک باقی می‌ماند:

```text
Home/Components/FeaturedProductsSection.razor
Catalog/Storefront/Components/ProductCard.razor

Home/Components/FeaturedCategoriesSection.razor
Catalog/Storefront/Components/CategoryCard.razor
```

Home تعیین می‌کند سکشن با چه عنوان، ترتیب و تعداد آیتم نمایش داده شود؛ Catalog تعیین می‌کند کارت محصول یا دسته‌بندی چه داده و رفتاری داشته باشد.

## Layout و پوسته عمومی

موارد مشترک تمام صفحات عمومی داخل Layout قرار می‌گیرند:

```text
Features/
└── Layout/
    └── Storefront/
        ├── StorefrontLayout.razor
        ├── StorefrontHeader.razor
        ├── StorefrontFooter.razor
        ├── MobileNavigation.razor
        ├── StorefrontShellModel.cs
        └── StorefrontShellService.cs
```

هدر و فوتر می‌توانند هم‌زمان خروجی چند Feature را نمایش دهند. این موضوع مرز مالکیت Featureها را تغییر نمی‌دهد؛ ترکیب فقط در لایه نمایش انجام می‌شود.

نمونه منابع Footer:

```text
StorefrontFooter
├── StoreInformation: نام، تلفن، آدرس، ساعات کاری و موقعیت
├── Content: معرفی کوتاه، قوانین و FAQ
└── SocialMedia: لینک شبکه‌های اجتماعی
```

نمونه منابع Header:

```text
StorefrontHeader
├── StoreInformation: نام و لوگوی فروشگاه
├── Catalog: دسته‌بندی‌ها یا محصولات پرفروش
├── Pricing: نرخ لحظه‌ای طلا
├── ShoppingCart: تعداد اقلام سبد
└── Authentication: وضعیت ورود مشتری
```

## Composition Serviceها

برای جلوگیری از تزریق چندین سرویس در Header، Footer یا HomePage، یک سرویس ترکیب‌کننده مدل آماده UI را می‌سازد:

```text
HomePage
    ↓
HomePageService
    ├── StorefrontCatalogService
    ├── StorefrontContentService
    └── StorefrontStoreInformationService

StorefrontLayout
    ↓
StorefrontShellService
    ├── StorefrontStoreInformationService
    ├── StorefrontContentService
    ├── StorefrontSocialMediaService
    └── StorefrontCatalogService
```

Composition Service فقط داده‌ها را کنار هم قرار می‌دهد:

- Business Logic جدید ایجاد نمی‌کند.
- Entityهای Featureهای دیگر را تغییر نمی‌دهد.
- عملیات ادمین انجام نمی‌دهد.
- مدل آماده Header، Footer یا HomePage را برمی‌گرداند.

وابستگی همیشه یک‌طرفه است:

```text
Home یا Layout
      ↓
سرویس خواندنی عمومی Feature
      ↓
Entity و داده همان Feature
```

Featureهایی مانند Catalog و Pricing نباید به Home، Header یا Footer وابسته شوند و نباید بدانند خروجی آن‌ها در کدام بخش UI نمایش داده می‌شود.

## داده‌های پویا و مستقل

همه اطلاعات Layout چرخه تغییر یکسان ندارند. لوگو، آدرس و متن معرفی کم‌تغییر هستند، ولی نرخ طلا و تعداد سبد خرید پویا هستند.

- اطلاعات کم‌تغییر Header و Footer توسط `StorefrontShellService` بارگذاری می‌شوند.
- نرخ طلا در کامپوننت مستقل `LiveGoldRate.razor` از Pricing خوانده و مستقل به‌روزرسانی می‌شود.
- تعداد سبد خرید state مستقل ShoppingCart است.
- محصولات پرفروش داخل Mega Menu فقط هنگام بازشدن منو یا با cache کوتاه‌مدت خوانده می‌شوند تا بار تمام صفحات افزایش پیدا نکند.

## سرویس‌ها و مدل‌های عمومی

سرویس‌های دارای عملیات مدیریتی مستقیماً در Storefront مصرف نمی‌شوند. در صورت تفاوت مسئولیت، سرویس خواندنی عمومی جدا ساخته می‌شود:

```text
CatalogService
└── عملیات ادمین، ذخیره، حذف، concurrency و OpLog

StorefrontCatalogService
└── خواندن محصولات منتشرشده، فعال و قابل فروش
```

مدل‌های ویرایش ادمین یا Entityهای EF مستقیماً به Razor عمومی داده نمی‌شوند. مدل‌های عمومی فقط داده موردنیاز مشتری را دارند، مانند:

```text
ProductCardModel
CategoryCardModel
BannerModel
StoreIdentityModel
AboutSummaryModel
SocialLinkModel
```

Storefront نباید اطلاعات داخلی زیر را ناخواسته نمایش دهد:

- محصولات Draft یا غیرفعال
- قطعات فروخته‌شده یا غیرفعال
- RowVersion
- بارکد و کد رهگیری داخلی
- تنظیمات یا توضیحات مدیریتی
- قیمت منقضی یا نرخ نامعتبر

## مالکیت بخش‌های صفحه اصلی

| بخش صفحه | Feature مالک داده | وضعیت فعلی |
|---|---|---|
| هدر و فوتر | Layout به‌عنوان محل ترکیب | باید استخراج شود |
| لوگو، نام، آدرس، ساعت و نقشه | StoreInformation | زیرساخت موجود است |
| اسلایدر و بنرها | Content | زیرساخت موجود است |
| دسته‌بندی‌ها | Catalog | زیرساخت موجود است |
| محصولات جدید و ویژه | Catalog | زیرساخت موجود است |
| قیمت محصولات و نرخ طلا | Pricing | زیرساخت اولیه موجود است |
| معرفی و داستان مرداس | Content یا StoreInformation بر اساس نوع محتوا | بخشی موجود و بخشی نیازمند تصمیم است |
| شبکه‌های اجتماعی | StoreInformation یا Feature مستقل SocialMedia در صورت رشد | هنوز باید اضافه شود |
| مطالب مجله | Blog | هنوز پیاده‌سازی نشده است |
| سبد و علاقه‌مندی | ShoppingCart و Wishlists | فاز بعدی |

## Render Mode و تعاملات

- محتوای عمومی صفحه برای سرعت اولیه و SEO سمت سرور رندر می‌شود.
- فقط بخش‌های واقعاً تعاملی مانند اسلایدر، منوی موبایل، جستجو، علاقه‌مندی و سبد خرید Interactive Server می‌شوند.
- JavaScript نمونه موجود در `Medalion1` عیناً وارد برنامه نمی‌شود؛ تعاملات تا جای ممکن با Blazor پیاده‌سازی می‌شوند و JS interop فقط برای نیاز واقعی باقی می‌ماند.
- تصاویر محصول از endpoint مناسب خوانده می‌شوند و به‌صورت Base64 داخل markup قرار نمی‌گیرند.

## مسیر اجرای تدریجی

1. استخراج توکن‌ها و دارایی‌های لازم از طرح پنجم و ایجاد Design System مستقل و scoped برای Storefront، بدون تغییر ناخواسته Design System ادمین.
2. ساخت `StorefrontLayout`، `StorefrontHeader` و `StorefrontFooter`.
3. تبدیل طرح پنجم به `HomePage.razor` استاتیک و نزدیک‌کردن خروجی Blazor به نمونه HTML.
4. شکستن صفحه به سکشن‌های Home و اجزای قابل استفاده مجدد Featureهای مالک.
5. اتصال نام، لوگو، آدرس، ساعات کاری و اطلاعات تماس به StoreInformation.
6. اتصال HeroSlider به PromotionBannerهای فعال Content.
7. اتصال دسته‌بندی‌ها به Catalog.
8. اتصال محصولات جدید، ویژه و پرفروش به Catalog و Pricing.
9. افزودن SocialMedia و Blog فقط هنگام رسیدن به نیاز واقعی آن‌ها.
10. پیاده‌سازی ShoppingCart، Wishlists، حساب مشتری، سفارش و پرداخت در فازهای بعد.

در هر مرحله، ظاهر فارسی RTL در دسکتاپ، موبایل و یک عرض میانی بررسی می‌شود و build و تست‌های مرتبط باید سبز بمانند.

## اولین کار پیشنهادی در گفت‌وگوی بعدی

اولین تغییر بهتر است فقط این محدوده را پوشش دهد:

1. ساخت ساختار پوشه‌های Home و `Layout/Storefront`.
2. تبدیل Header، Footer و HomePage طرح پنجم به Razor استاتیک.
3. انتقال حداقلی CSS و assets موردنیاز و تعریف theme مستقل Storefront با scope روشن.
4. حفظ رفتار فعلی Admin و مسیرهای احراز هویت.
5. اجرای build و بررسی ظاهری دسکتاپ و موبایل.

در این مرحله هنوز داده‌های صفحه اصلی به دیتابیس متصل نمی‌شوند. پس از تأیید برابری بصری، اتصال هر سکشن جداگانه انجام خواهد شد.

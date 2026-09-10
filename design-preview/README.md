# نمونه صفحه ورود مرداس گلد

فایل `login.html` را در مرورگر باز کنید. فونت و تصویر همراه نمونه هستند و اجرای صفحه به نصب ابزار یا اینترنت نیاز ندارد. فایل `login-classic.html` نسخه قبلی است.

## تغییر تم

در `theme.css` فقط مقدار زیر را عوض کنید:

```css
--primary: #594ae2;
```

دکمه‌ها، آیکون‌ها، فوکوس، لینک‌ها، سایه‌های رنگی و سطوح ملایم از این مقدار مشتق می‌شوند. رنگ‌های خنثی، طلایی و وضعیت‌ها نیز در همین فایل متمرکزند. رنگ طلای داخل تصویر، بخشی از عکس است و با CSS تغییر نمی‌کند. برای رنگ‌های بسیار روشن باید `--on-primary` و کنتراست متن نیز بررسی شود.

`login.css` فقط ظاهر و چیدمان صفحه را تعریف می‌کند. `login.js` تعاملات نمونه را انجام می‌دهد. هیچ رمز یا نام کاربری ارسال، ذخیره یا در گزارش‌ها ثبت نمی‌شود. بازیابی رمز صرفاً راهنمای تماس با مدیر سیستم است. دکمه ورود، پس از اعتبارسنجی، پیام نمونه بودن صفحه را نمایش می‌دهد.

در مرحله Blazor این متغیرها مبنای تم مشترک و MudTheme خواهند بود؛ صفحه فعلی هنوز به سامانه احراز هویت وصل نیست.

## تصویر اختصاصی

فایل: `assets/jewelry-editorial.png`، ابعاد 1024 × 1536، تولید با ابزار داخلی ImageGen. تصویر، دارایی نمایشی طراحی است و عکس محصول واقعی فروشگاه نیست.

پرامپت: Portrait luxury editorial studio photograph. One polished 18k yellow gold solitaire diamond ring standing upright at a slightly diagonal angle, centered in the middle 55% of a 2:3 frame. Near-black charcoal matte stone with restrained muted violet reflections. Dramatic controlled softbox light, rich warm gold, realistic faceted diamond. Quiet negative space in the top and bottom 20% for HTML text. No text, logos, watermark, extra rings, people or UI.

فونت وزیرمتن با مجوز OFL؛ متن مجوز در `assets/Vazirmatn-LICENSE.txt` قرار دارد.

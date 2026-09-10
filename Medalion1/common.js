(() => {
  const D = window.MEDALION_DATA;
  const fa = n => new Intl.NumberFormat('fa-IR').format(n);
  const read = key => { try { return JSON.parse(localStorage.getItem(key) || '[]'); } catch { return []; } };
  const write = (key, value) => localStorage.setItem(key, JSON.stringify(value));
  const headerHost = document.getElementById('siteHeader');
  const footerHost = document.getElementById('siteFooter');
  const current = document.body.dataset.page || '';
  const nav = (href, label, key) => `<a class="${current === key ? 'active' : ''}" href="${href}">${label}</a>`;

  if (headerHost) headerHost.innerHTML = `
    <header class="site-header">
      <div class="header-main shell">
        <a class="brand" href="index.html"><img src="assets/images/Untitled-1-3.png" alt="فروشگاه مدالیون"></a>
        <form class="search" id="innerSearch"><svg aria-hidden="true" viewBox="0 0 24 24"><circle cx="11" cy="11" r="7"></circle><path d="m20 20-4-4"></path></svg><input type="search" placeholder="جستجوی محصولات" aria-label="جستجوی محصولات"></form>
        <div class="header-tools">
          <a class="round-tool" href="compare.html" aria-label="مقایسه محصولات"><span class="count" data-count="compare">۰</span><svg viewBox="0 0 24 24"><path d="M8 7h10l-3-3m3 3-3 3M16 17H6l3 3m-3-3 3-3"></path></svg></a>
          <a class="round-tool" href="wishlist.html" aria-label="علاقه‌مندی‌ها"><span class="count" data-count="wishlist">۰</span><svg viewBox="0 0 24 24"><path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8l1.1 1.1L12 21l7.8-7.5 1.1-1.1a5.5 5.5 0 0 0-.1-7.8Z"></path></svg></a>
          <a class="round-tool" href="cart.html" aria-label="سبد خرید"><span class="count" data-count="cart">۰</span><svg viewBox="0 0 24 24"><circle cx="9" cy="20" r="1"></circle><circle cx="19" cy="20" r="1"></circle><path d="M3 4h2l2.4 10.4a2 2 0 0 0 2 1.6h7.7a2 2 0 0 0 2-1.6L21 7H6"></path></svg></a>
          <a class="login" href="account.html">ورود / ثبت نام</a>
        </div>
        <button class="mobile-toggle" type="button" aria-label="نمایش فهرست" aria-expanded="false" id="innerMobile"><span></span><span></span><span></span></button>
      </div>
      <nav class="main-nav shell" id="innerNav" aria-label="منوی اصلی">
        <a class="category-link" href="shop.html"><svg viewBox="0 0 24 24"><path d="M4 6h16M4 12h16M4 18h16"></path></svg>دسته‌بندی کالاها</a>
        <div class="nav-links">${nav('index.html','صفحه اصلی','home')}${nav('about.html','درباره ما','about')}${nav('shop.html','فروشگاه','shop')}${nav('blog.html','بلاگ','blog')}${nav('contact.html','ارتباط با ما','contact')}</div>
      </nav>
    </header>`;

  if (footerHost) footerHost.innerHTML = `
    <footer><div class="footer-wave"></div><div class="footer-grid shell">
      <section class="footer-about"><img src="assets/images/Untitled-1-3-1.png" alt="مدالیون"><p>از سال ۱۳۸۸ در عرصه طراحی و تولید طلا و جواهرات فعالیت داشتیم و اکنون تولیدات خود را با کیفیت بالا و قیمت مناسب به دست مصرف‌کننده می‌رسانیم.</p></section>
      <section><h3>دسترسی سریع</h3><a href="index.html">صفحه اصلی</a><a href="about.html">درباره ما</a><a href="blog.html">بلاگ</a><a href="contact.html">ارتباط با ما</a><a href="rules.html">قوانین فروشگاه</a></section>
      <section><h3>خدمات مشتریان</h3><a href="contact.html#faq">پرسش‌های متداول</a><a href="shop.html">راهنمای خرید</a><a href="rules.html">شیوه ارسال</a><a href="article.html?post=ring-size">راهنمای اندازه‌ها</a><a href="rules.html">بازگشت کالا</a></section>
      <section class="trust"><h3>نماد اعتماد</h3><img src="assets/images/1005129_orig-100x100x0x0x100x100x1675713258.png" alt="نماد اعتماد نمایشی"><p>این نماد فاقد اعتبار است<br>و صرفاً جنبه نمایشی دارد</p></section>
    </div><div class="copyright">نسخه آفلاین فروشگاه مدالیون</div></footer>`;

  const toast = document.createElement('div');
  toast.className = 'inner-toast';
  document.body.append(toast);
  let toastTimer;
  const notify = message => { toast.textContent = message; toast.classList.add('show'); clearTimeout(toastTimer); toastTimer = setTimeout(() => toast.classList.remove('show'), 2000); };
  const updateCounts = () => {
    document.querySelectorAll('[data-count="cart"]').forEach(e => e.textContent = fa(read('medalionCart').length));
    document.querySelectorAll('[data-count="wishlist"]').forEach(e => e.textContent = fa(read('medalionWishlist').length));
    document.querySelectorAll('[data-count="compare"]').forEach(e => e.textContent = fa(read('medalionCompare').length));
  };
  updateCounts();

  const mobile = document.getElementById('innerMobile');
  mobile?.addEventListener('click', () => { const menu = document.getElementById('innerNav'); const open = menu.classList.toggle('open'); mobile.setAttribute('aria-expanded', String(open)); });
  document.getElementById('innerSearch')?.addEventListener('submit', event => { event.preventDefault(); const q = event.currentTarget.querySelector('input').value.trim(); location.href = `shop.html${q ? `?q=${encodeURIComponent(q)}` : ''}`; });

  const productCard = p => `<article class="shop-product" data-product="${p.id}"><a href="product.html?id=${p.id}"><img src="assets/images/${p.image}" alt="${p.title}"><h3>${p.title}</h3></a><p class="price">${p.price}</p><div class="shop-actions"><button class="mini-button" data-action="add-cart" data-id="${p.id}">افزودن به سبد</button><button class="mini-button alt" data-action="wishlist" data-id="${p.id}" aria-label="علاقه‌مندی">♡</button></div></article>`;
  const articleCard = a => `<article><a href="article.html?post=${a.id}"><img src="assets/images/${a.image}" alt="${a.title}"></a><div class="post-body"><span class="post-meta">${a.date} · بدون دیدگاه</span><h2><a href="article.html?post=${a.id}">${a.title}</a></h2><p>${a.excerpt}</p><a class="gold-button" href="article.html?post=${a.id}">ادامه مطلب</a></div></article>`;

  const shopGrid = document.getElementById('shopGrid');
  if (shopGrid) {
    const params = new URLSearchParams(location.search);
    let category = params.get('category') || 'all';
    const query = (params.get('q') || '').trim().toLowerCase();
    const sort = document.getElementById('shopSort');
    const max = document.getElementById('priceFilter');
    const renderShop = () => {
      let items = D.products.filter(p => (category === 'all' || p.category === category) && p.priceValue <= Number(max?.value || 3000000) && (!query || p.title.toLowerCase().includes(query)));
      if (sort?.value === 'low') items.sort((a,b)=>a.priceValue-b.priceValue);
      if (sort?.value === 'high') items.sort((a,b)=>b.priceValue-a.priceValue);
      shopGrid.innerHTML = items.length ? items.map(productCard).join('') : '<div class="page-card empty-state" style="grid-column:1/-1"><h2>محصولی پیدا نشد</h2><p class="muted">فیلترها را تغییر دهید.</p></div>';
      document.getElementById('resultsCount').textContent = `${fa(items.length)} محصول`;
    };
    document.querySelectorAll('[data-category]').forEach(button => { if (button.dataset.category === category) button.classList.add('active'); button.addEventListener('click', () => { document.querySelectorAll('[data-category]').forEach(b=>b.classList.remove('active')); button.classList.add('active'); category=button.dataset.category; renderShop(); }); });
    sort?.addEventListener('change', renderShop); max?.addEventListener('input', () => { document.getElementById('priceLabel').textContent = `${fa(Number(max.value))} تومان`; renderShop(); });
    renderShop();
  }

  const blogList = document.getElementById('blogList');
  if (blogList) blogList.innerHTML = D.articles.map(articleCard).join('');

  const productHost = document.getElementById('productHost');
  if (productHost) {
    const product = D.products.find(p => p.id === new URLSearchParams(location.search).get('id')) || D.products[0];
    document.title = `${product.title} | مدالیون`;
    document.getElementById('pageTitle').textContent = product.title;
    productHost.innerHTML = `<div class="product-detail"><div class="product-gallery"><img src="assets/images/${product.large}" alt="${product.title}"></div><div class="product-summary"><span class="muted">خانه / محصولات</span><h1>${product.title}</h1><p class="price">${product.price}</p><p>توضیحات محصول در این قسمت قرار می‌گیرد. این محصول با طراحی ظریف و ساختار زیبا، برای استفاده روزمره و هدیه دادن انتخاب مناسبی است.</p><div class="qty-row"><input type="number" min="1" value="1" aria-label="تعداد"><button class="gold-button" data-action="add-cart" data-id="${product.id}">افزودن به سبد خرید</button></div><div class="product-extra"><button class="mini-button alt" data-action="compare" data-id="${product.id}">افزودن به مقایسه</button><button class="mini-button alt" data-action="wishlist" data-id="${product.id}">افزودن به علاقه‌مندی</button></div></div></div><div class="product-tabs page-card"><h2>توضیحات</h2><p>توضیحات محصول در این قسمت قرار می‌گیرد. مدالیون تلاش می‌کند محصولات خود را با کیفیت بالا، طرح‌های متنوع و قیمت مناسب در اختیار مصرف‌کنندگان قرار دهد.</p><h3>خرید و ارسال</h3><p>زمان ارسال کالا برای تهران و شهرستان‌ها بین ۵ تا ۷ روز کاری است. جزئیات سفارش در نسخه آفلاین صرفاً نمایشی است.</p></div><section class="related-products"><h2>محصولات مشابه</h2><div class="shop-grid">${D.products.filter(p=>p.id!==product.id).slice(0,4).map(productCard).join('')}</div></section>`;
  }

  const articleHost = document.getElementById('articleHost');
  if (articleHost) {
    const article = D.articles.find(a => a.id === new URLSearchParams(location.search).get('post')) || D.articles[0];
    document.title = `${article.title} | مدالیون`;
    document.getElementById('pageTitle').textContent = article.title;
    articleHost.innerHTML = `<div class="article-layout"><article class="article-main"><img src="assets/images/${article.image}" alt="${article.title}"><h1>${article.title}</h1><div class="post-meta">ارسال توسط مدالیون · ${article.date} · بدون دیدگاه</div>${article.sections.map(([h,p])=>`${h?`<h2>${h}</h2>`:''}<p>${p}</p>`).join('')}</article><aside class="article-sidebar"><h3>آخرین مطالب</h3>${D.articles.filter(a=>a.id!==article.id).map(a=>`<a class="side-post" href="article.html?post=${a.id}"><img src="assets/images/${a.image}" alt=""><span>${a.title}</span></a>`).join('')}</aside></div>`;
  }

  const listHost = document.getElementById('listHost');
  if (listHost) {
    const type = listHost.dataset.type;
    const key = type === 'cart' ? 'medalionCart' : type === 'wishlist' ? 'medalionWishlist' : 'medalionCompare';
    const renderList = () => {
      const ids = read(key); const items = ids.map(id=>D.products.find(p=>p.id===id)).filter(Boolean);
      if (!items.length) { listHost.innerHTML = `<div class="page-card empty-state"><h2>${type==='cart'?'سبد خرید':type==='wishlist'?'لیست علاقه‌مندی':'فهرست مقایسه'} خالی است</h2><p class="muted">هنوز محصولی به این بخش اضافه نکرده‌اید.</p><a class="gold-button" href="shop.html">بازگشت به فروشگاه</a></div>`; return; }
      listHost.innerHTML = `<div class="list-page">${items.map((p,i)=>`<article class="list-item"><a href="product.html?id=${p.id}"><img src="assets/images/${p.image}" alt="${p.title}"></a><div><h3><a href="product.html?id=${p.id}">${p.title}</a></h3><p>${p.price}</p>${type!=='cart'?`<button class="mini-button" data-action="add-cart" data-id="${p.id}">افزودن به سبد</button>`:''}</div><button class="remove-item" data-remove-index="${i}" aria-label="حذف">×</button></article>`).join('')}</div>`;
      listHost.querySelectorAll('[data-remove-index]').forEach(button=>button.addEventListener('click',()=>{const data=read(key);data.splice(Number(button.dataset.removeIndex),1);write(key,data);updateCounts();renderList();}));
    };
    renderList();
  }

  document.addEventListener('click', event => {
    const button = event.target.closest('[data-action]'); if (!button) return;
    const id = button.dataset.id; const action = button.dataset.action;
    const key = action === 'add-cart' ? 'medalionCart' : action === 'wishlist' ? 'medalionWishlist' : 'medalionCompare';
    const data = read(key); if (action === 'add-cart' || !data.includes(id)) data.push(id); write(key,data); updateCounts();
    notify(action === 'add-cart' ? 'محصول به سبد خرید اضافه شد' : action === 'wishlist' ? 'به علاقه‌مندی‌ها اضافه شد' : 'به فهرست مقایسه اضافه شد');
  });

  document.querySelectorAll('form[data-offline-form]').forEach(form => form.addEventListener('submit', event => { event.preventDefault(); form.reset(); notify('اطلاعات در نسخه آفلاین ثبت نمایشی شد'); }));
  document.querySelectorAll('[data-tab]').forEach(button => button.addEventListener('click', () => { document.querySelectorAll('[data-tab]').forEach(b=>b.classList.remove('active')); document.querySelectorAll('[data-tab-panel]').forEach(p=>p.hidden=true); button.classList.add('active'); document.querySelector(`[data-tab-panel="${button.dataset.tab}"]`).hidden=false; }));
})();

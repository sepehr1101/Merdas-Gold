(() => {
  const fa = n => new Intl.NumberFormat('fa-IR').format(n);
  const toast = document.getElementById('toast');
  let toastTimer;
  const notify = message => {
    toast.textContent = message;
    toast.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => toast.classList.remove('show'), 2200);
  };

  const header = document.querySelector('.site-header');
  const topButton = document.querySelector('.back-top');
  addEventListener('scroll', () => {
    header.classList.toggle('scrolled', scrollY > 80);
    topButton.classList.toggle('show', scrollY > 650);
  }, { passive: true });
  topButton.addEventListener('click', () => scrollTo({ top: 0, behavior: 'smooth' }));

  const mobileToggle = document.getElementById('mobileToggle');
  const mainNav = document.getElementById('mainNav');
  mobileToggle.addEventListener('click', () => {
    const open = mainNav.classList.toggle('open');
    mobileToggle.setAttribute('aria-expanded', String(open));
  });
  mainNav.querySelectorAll('a').forEach(a => a.addEventListener('click', () => mainNav.classList.remove('open')));

  const slides = [...document.querySelectorAll('[data-slide]')];
  const dotsHost = document.querySelector('.slider-dots');
  let activeSlide = 0;
  let slideTimer;
  const selectSlide = index => {
    activeSlide = (index + slides.length) % slides.length;
    slides.forEach((slide, i) => slide.classList.toggle('active', i === activeSlide));
    [...dotsHost.children].forEach((dot, i) => dot.classList.toggle('active', i === activeSlide));
  };
  slides.forEach((_, i) => {
    const dot = document.createElement('button');
    dot.type = 'button';
    dot.setAttribute('aria-label', `اسلاید ${fa(i + 1)}`);
    dot.addEventListener('click', () => { selectSlide(i); restartSlider(); });
    dotsHost.append(dot);
  });
  const restartSlider = () => {
    clearInterval(slideTimer);
    slideTimer = setInterval(() => selectSlide(activeSlide + 1), 5200);
  };
  document.querySelector('.slider-arrow.prev').addEventListener('click', () => { selectSlide(activeSlide - 1); restartSlider(); });
  document.querySelector('.slider-arrow.next').addEventListener('click', () => { selectSlide(activeSlide + 1); restartSlider(); });
  selectSlide(0);
  restartSlider();

  let cart = [];
  let wishes = 0;
  const cartCount = document.getElementById('cartCount');
  const wishCount = document.getElementById('wishCount');
  const cartDrawer = document.getElementById('cartDrawer');
  const backdrop = document.getElementById('drawerBackdrop');
  const cartItems = document.getElementById('cartItems');
  const cartTotal = document.getElementById('cartTotal');
  const renderCart = () => {
    cartCount.textContent = fa(cart.length);
    cartTotal.textContent = fa(cart.length);
    if (!cart.length) {
      cartItems.innerHTML = '<p class="empty-cart">سبد خرید شما خالی است.</p>';
      return;
    }
    cartItems.innerHTML = cart.map((item, index) => `<div class="cart-item"><div><b>${item.name}</b><br><span>${item.price}</span></div><button type="button" data-remove="${index}" aria-label="حذف ${item.name}">×</button></div>`).join('');
  };
  const setDrawer = open => {
    cartDrawer.classList.toggle('open', open);
    backdrop.classList.toggle('open', open);
    cartDrawer.setAttribute('aria-hidden', String(!open));
  };
  document.querySelectorAll('[data-open-cart]').forEach(b => b.addEventListener('click', () => setDrawer(true)));
  document.querySelector('.drawer-close').addEventListener('click', () => setDrawer(false));
  backdrop.addEventListener('click', () => setDrawer(false));
  cartItems.addEventListener('click', event => {
    const button = event.target.closest('[data-remove]');
    if (!button) return;
    cart.splice(Number(button.dataset.remove), 1);
    renderCart();
    notify('محصول از سبد خرید حذف شد');
  });
  document.querySelectorAll('.product').forEach(product => {
    product.querySelector('.add-cart').addEventListener('click', () => {
      cart.push({ name: product.querySelector('h3').textContent, price: product.querySelector('.price').textContent });
      renderCart();
      notify('محصول به سبد خرید اضافه شد');
    });
    product.querySelector('.heart').addEventListener('click', event => {
      const active = event.currentTarget.classList.toggle('active');
      event.currentTarget.textContent = active ? '♥' : '♡';
      wishes += active ? 1 : -1;
      wishCount.textContent = fa(wishes);
      notify(active ? 'به علاقه‌مندی‌ها اضافه شد' : 'از علاقه‌مندی‌ها حذف شد');
    });
  });
  renderCart();

  const searchForm = document.getElementById('searchForm');
  const searchInput = document.getElementById('searchInput');
  const noResults = document.getElementById('noResults');
  searchForm.addEventListener('submit', event => event.preventDefault());
  searchInput.addEventListener('input', () => {
    const term = searchInput.value.trim().toLocaleLowerCase('fa');
    let found = 0;
    document.querySelectorAll('.product').forEach(product => {
      const match = product.dataset.name.toLocaleLowerCase('fa').includes(term);
      product.style.display = match ? '' : 'none';
      if (match) found++;
    });
    noResults.classList.toggle('show', found === 0);
    if (term) document.getElementById('products').scrollIntoView({ behavior: 'smooth', block: 'start' });
  });

  const dialog = document.getElementById('infoDialog');
  const openDialog = () => typeof dialog.showModal === 'function' ? dialog.showModal() : dialog.setAttribute('open', '');
  document.getElementById('loginButton').addEventListener('click', openDialog);
  document.querySelector('.compare-button').addEventListener('click', openDialog);
  document.querySelectorAll('.read-more').forEach(button => button.addEventListener('click', openDialog));
  document.querySelector('.dialog-close').addEventListener('click', () => dialog.close());
  document.querySelector('.dialog-ok').addEventListener('click', () => dialog.close());
})();

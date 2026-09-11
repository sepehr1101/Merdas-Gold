(() => {
  const fa = number => new Intl.NumberFormat('fa-IR').format(number);
  const toast = document.getElementById('toast');
  let toastTimer;
  const notify = message => {
    toast.textContent = message;
    toast.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => toast.classList.remove('show'), 2200);
  };

  const menuButton = document.querySelector('.menu-toggle');
  const navigation = document.getElementById('mainNav');
  menuButton.addEventListener('click', () => {
    const open = navigation.classList.toggle('open');
    menuButton.setAttribute('aria-expanded', String(open));
  });
  navigation.querySelectorAll('a').forEach(link => link.addEventListener('click', () => {
    navigation.classList.remove('open');
    menuButton.setAttribute('aria-expanded', 'false');
  }));

  const searchButton = document.querySelector('[data-search-toggle]');
  const searchPanel = document.querySelector('.search-panel');
  searchButton.addEventListener('click', () => {
    searchPanel.hidden = !searchPanel.hidden;
    if (!searchPanel.hidden) document.getElementById('aiSearch').focus();
  });

  const slides = [...document.querySelectorAll('[data-slide]')];
  const dotsHost = document.querySelector('.slider-dots');
  let activeSlide = 0;
  let slideTimer;
  const selectSlide = index => {
    activeSlide = (index + slides.length) % slides.length;
    slides.forEach((slide, i) => slide.classList.toggle('active', i === activeSlide));
    [...dotsHost.children].forEach((dot, i) => dot.classList.toggle('active', i === activeSlide));
  };
  const restartSlider = () => {
    clearInterval(slideTimer);
    slideTimer = setInterval(() => selectSlide(activeSlide + 1), 5200);
  };
  slides.forEach((_, index) => {
    const dot = document.createElement('button');
    dot.type = 'button';
    dot.setAttribute('aria-label', `اسلاید ${fa(index + 1)}`);
    dot.addEventListener('click', () => { selectSlide(index); restartSlider(); });
    dotsHost.append(dot);
  });
  document.querySelector('.slider-arrow.prev').addEventListener('click', () => { selectSlide(activeSlide - 1); restartSlider(); });
  document.querySelector('.slider-arrow.next').addEventListener('click', () => { selectSlide(activeSlide + 1); restartSlider(); });
  selectSlide(0);
  restartSlider();

  let cartCount = 0;
  const bagCount = document.querySelector('.bag span');
  document.querySelectorAll('.product').forEach(product => {
    product.querySelector('.add-cart').addEventListener('click', () => {
      cartCount++;
      bagCount.textContent = fa(cartCount);
      document.querySelector('.bag').setAttribute('aria-label', `سبد خرید، ${fa(cartCount)} محصول`);
      notify('محصول به سبد خرید اضافه شد');
    });
    product.querySelector('.heart').addEventListener('click', event => {
      const active = event.currentTarget.classList.toggle('active');
      event.currentTarget.textContent = active ? '♥' : '♡';
      event.currentTarget.setAttribute('aria-pressed', String(active));
      notify(active ? 'به علاقه‌مندی‌ها اضافه شد' : 'از علاقه‌مندی‌ها حذف شد');
    });
  });

  document.querySelectorAll('.read-more').forEach(button => button.addEventListener('click', () => location.href = 'article.html'));
  const topButton = document.querySelector('.back-top');
  addEventListener('scroll', () => topButton.classList.toggle('show', scrollY > 650), { passive: true });
  topButton.addEventListener('click', () => scrollTo({ top: 0, behavior: 'smooth' }));
})();

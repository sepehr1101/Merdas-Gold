// Presentation-only prototype: no credentials are sent, stored, or logged.
(() => {
  'use strict';
  const form = document.querySelector('#login-form');
  const password = document.querySelector('#password');
  const visibility = document.querySelector('.visibility');
  const fields = [...form.querySelectorAll('input')];
  const notice = document.querySelector('#demo-notice');
  const caps = document.querySelector('#caps-lock');
  const recovery = document.querySelector('#recovery');

  visibility.addEventListener('click', () => {
    const showing = password.type === 'password';
    password.type = showing ? 'text' : 'password';
    visibility.setAttribute('aria-pressed', String(showing));
    visibility.setAttribute('aria-label', showing ? 'پنهان کردن رمز عبور' : 'نمایش رمز عبور');
    document.querySelector('#eye-slash').toggleAttribute('hidden', !showing);
  });

  function setInvalid(input, invalid) {
    input.setAttribute('aria-invalid', String(invalid));
    document.querySelector('#' + input.id + '-error').hidden = !invalid;
  }
  fields.forEach(input => input.addEventListener('input', () => {
    setInvalid(input, false);
    notice.hidden = true;
  }));
  function checkCaps(event) {
    if (event.getModifierState) caps.hidden = !event.getModifierState('CapsLock');
  }
  password.addEventListener('keydown', checkCaps);
  password.addEventListener('keyup', checkCaps);
  password.addEventListener('blur', () => { caps.hidden = true; });
  recovery.addEventListener('click', () => {
    const expanded = recovery.getAttribute('aria-expanded') !== 'true';
    recovery.setAttribute('aria-expanded', String(expanded));
    document.querySelector('#recovery-note').hidden = !expanded;
  });
  form.addEventListener('submit', event => {
    event.preventDefault();
    let firstInvalid;
    fields.forEach(input => {
      const invalid = input.id === 'username' ? !input.value.trim() : !input.value;
      setInvalid(input, invalid);
      if (invalid && !firstInvalid) firstInvalid = input;
    });
    notice.hidden = Boolean(firstInvalid);
    if (firstInvalid) firstInvalid.focus();
  });
})();

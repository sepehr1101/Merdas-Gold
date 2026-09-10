(() => {
    const persianNumber = value => new Intl.NumberFormat("fa-IR", {
        useGrouping: false
    }).format(value);

    const getLabel = control =>
        control.dataset.validationLabel ||
        control.getAttribute("aria-label") ||
        "این فیلد";

    const getMatchMessage = control => {
        const targetName = control.dataset.match;
        if (!targetName || !control.form || !control.value) return null;

        const target = control.form.elements.namedItem(targetName);
        if (!target || control.value === target.value) return null;

        return control.dataset.matchMessage || `${getLabel(control)} با مقدار موردنظر یکسان نیست.`;
    };

    const setPersianValidationMessage = control => {
        if (!(control instanceof HTMLInputElement ||
              control instanceof HTMLTextAreaElement ||
              control instanceof HTMLSelectElement)) return;

        const matchMessage = getMatchMessage(control);
        control.setCustomValidity("");

        if (matchMessage) {
            control.setCustomValidity(matchMessage);
            return;
        }

        const validity = control.validity;
        const label = getLabel(control);
        let message = "";

        if (validity.valueMissing) {
            message = `لطفاً ${label} را وارد کنید.`;
        } else if (validity.typeMismatch && control.type === "email") {
            message = "لطفاً یک نشانی ایمیل معتبر وارد کنید.";
        } else if (validity.typeMismatch && control.type === "url") {
            message = "لطفاً یک نشانی اینترنتی معتبر وارد کنید.";
        } else if (validity.tooShort) {
            message = `${label} باید حداقل ${persianNumber(control.minLength)} نویسه باشد.`;
        } else if (validity.tooLong) {
            message = `${label} باید حداکثر ${persianNumber(control.maxLength)} نویسه باشد.`;
        } else if (validity.patternMismatch) {
            message = control.dataset.patternMessage || `قالب ${label} صحیح نیست.`;
        } else if (validity.rangeUnderflow) {
            message = `${label} نباید کمتر از ${control.min} باشد.`;
        } else if (validity.rangeOverflow) {
            message = `${label} نباید بیشتر از ${control.max} باشد.`;
        } else if (validity.stepMismatch) {
            message = `مقدار واردشده برای ${label} معتبر نیست.`;
        } else if (validity.badInput) {
            message = `لطفاً مقدار معتبری برای ${label} وارد کنید.`;
        }

        control.setCustomValidity(message);
    };

    document.addEventListener("invalid", event => {
        setPersianValidationMessage(event.target);
    }, true);

    document.addEventListener("input", event => {
        const control = event.target;
        if (!(control instanceof HTMLInputElement ||
              control instanceof HTMLTextAreaElement ||
              control instanceof HTMLSelectElement)) return;

        control.setCustomValidity("");

        if (control.dataset.match) {
            setPersianValidationMessage(control);
        }

        if (control.form && control.name) {
            for (const dependent of control.form.querySelectorAll(`[data-match="${CSS.escape(control.name)}"]`)) {
                setPersianValidationMessage(dependent);
            }
        }
    }, true);

    document.addEventListener("change", event => {
        setPersianValidationMessage(event.target);
    }, true);
})();

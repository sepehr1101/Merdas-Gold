const editors = new WeakMap();
export function init(host) {
    const body = host.querySelector('.editor-body');
    const controller = new AbortController();
    const options = {signal: controller.signal};
    let range;
    const remember = () => {
        const selection = window.getSelection();
        if (selection.rangeCount && body.contains(selection.anchorNode)) range = selection.getRangeAt(0).cloneRange();
    };
    const restore = () => {
        body.focus();
        if (range && body.contains(range.commonAncestorContainer)) {
            const selection = window.getSelection(); selection.removeAllRanges(); selection.addRange(range);
        }
    };
    document.addEventListener('selectionchange', remember, options);
    host.querySelectorAll('button').forEach(button => {
        button.addEventListener('mousedown', e => e.preventDefault(), options);
        button.addEventListener('click', () => {
            restore();
            if (button.hasAttribute('data-link')) {
                const input = host.querySelector('[data-link-url]');
                const href = input.value.trim();
                if (!/^https?:\/\//i.test(href) || !input.checkValidity()) { input.setCustomValidity('نشانی کامل با https:// وارد کنید.'); input.reportValidity(); return; }
                if (!window.getSelection().toString()) { input.setCustomValidity('ابتدا بخشی از متن را انتخاب کنید.'); input.reportValidity(); return; }
                input.setCustomValidity(''); document.execCommand('createLink', false, href);
            } else if (button.dataset.block) document.execCommand('formatBlock', false, button.dataset.block);
            else document.execCommand(button.dataset.command, false);
            remember();
        }, options);
    });
    host.querySelector('[data-link-url]').addEventListener('input', e => e.target.setCustomValidity(''), options);
    body.addEventListener('paste', e => { e.preventDefault(); document.execCommand('insertText', false, e.clipboardData.getData('text/plain')); }, options);
    body.addEventListener('drop', e => e.preventDefault(), options);
    editors.set(host, controller);
}
export function read(host) {
    function nodes(parent) {
        return [...parent.childNodes].flatMap(node => {
            if (node.nodeType === Node.TEXT_NODE) return [{type:'text', text:node.textContent}];
            if (node.nodeType !== Node.ELEMENT_NODE) return [];
            let type = node.tagName.toLowerCase();
            if (['script','style','img','iframe','object'].includes(type)) return [];
            if (type === 'div') type = 'p';
            if (type === 'b') type = 'strong';
            const children = nodes(node);
            if (!['p','h2','h3','strong','ul','ol','li','a','br'].includes(type)) return children;
            if (type === 'a') {
                const href = node.getAttribute('href') || '';
                if (!/^https?:\/\//i.test(href) && !/^\/(?!\/)/.test(href)) return children;
                return [{type, href, children}];
            }
            return [{type, children}];
        });
    }
    const json = JSON.stringify(nodes(host.querySelector('.editor-body')));
    if (new TextEncoder().encode(json).length > 100000) throw new Error('متن بیش از حد طولانی است.');
    return json;
}
export function dispose(host) { editors.get(host)?.abort(); editors.delete(host); }


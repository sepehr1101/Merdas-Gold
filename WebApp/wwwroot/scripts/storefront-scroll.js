window.storefrontScroll = (() => {
    const handlers = new Map();

    function dispose(id) {
        const handler = handlers.get(id);
        if (!handler) return;
        window.removeEventListener("scroll", handler);
        handlers.delete(id);
    }

    return {
        initialize(id) {
            dispose(id);
            const button = document.getElementById(id);
            if (!button) return;

            const update = () => button.classList.toggle("back-top--visible", window.scrollY > 650);
            handlers.set(id, update);
            window.addEventListener("scroll", update, { passive: true });
            update();
        },
        toTop() {
            window.scrollTo({ top: 0, behavior: "smooth" });
        },
        dispose
    };
})();

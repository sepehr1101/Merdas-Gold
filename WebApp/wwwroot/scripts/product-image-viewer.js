const viewers = new WeakMap();
const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

export function openViewer(dialog, images, index, title) {
    if (!images?.length) return;
    let viewer = viewers.get(dialog);
    if (!viewer) {
        viewer = createViewer(dialog);
        viewers.set(dialog, viewer);
    }
    viewer.open(images, index, title);
}

export function disposeViewer(dialog) {
    const viewer = viewers.get(dialog);
    if (!viewer) return;
    viewer.dispose();
    viewers.delete(dialog);
}

function createViewer(dialog) {
    const controller = new AbortController();
    const signal = controller.signal;
    const find = name => dialog.querySelector(`[data-viewer-${name}]`);
    const stage = find("stage");
    const image = find("image");
    const count = find("count");
    const zoomText = find("zoom");
    const pointers = new Map();
    let images = [];
    let index = 0;
    let title = "";
    let zoom = 1;
    let rotation = 0;
    let offsetX = 0;
    let offsetY = 0;
    let pinchDistance = 0;
    let pinchZoom = 1;
    let dragX = 0;
    let dragY = 0;

    function render() {
        image.style.transform = `translate(${offsetX}px, ${offsetY}px) rotate(${rotation}deg) scale(${zoom})`;
        zoomText.textContent = `${Math.round(zoom * 100).toLocaleString("fa-IR")}٪`;
        find("zoom-out").disabled = zoom <= 1;
        find("zoom-in").disabled = zoom >= 4;
    }

    function reset() {
        zoom = 1;
        rotation = 0;
        offsetX = offsetY = 0;
        pointers.clear();
        stage.classList.remove("is-dragging");
        render();
    }

    function select(next) {
        index = (next + images.length) % images.length;
        reset();
        image.src = images[index];
        image.alt = `${title}، تصویر ${index + 1} از ${images.length}`;
        count.textContent = `${(index + 1).toLocaleString("fa-IR")} / ${images.length.toLocaleString("fa-IR")}`;
        find("prev").hidden = images.length < 2;
        find("next").hidden = images.length < 2;
    }

    function setZoom(next) {
        zoom = clamp(Math.round(next * 10) / 10, 1, 4);
        if (zoom === 1) offsetX = offsetY = 0;
        render();
    }

    find("close").addEventListener("click", () => dialog.close(), { signal });
    find("prev").addEventListener("click", () => select(index - 1), { signal });
    find("next").addEventListener("click", () => select(index + 1), { signal });
    find("zoom-in").addEventListener("click", () => setZoom(zoom + .5), { signal });
    find("zoom-out").addEventListener("click", () => setZoom(zoom - .5), { signal });
    find("rotate").addEventListener("click", () => { rotation = (rotation + 90) % 360; render(); }, { signal });
    find("reset").addEventListener("click", reset, { signal });
    dialog.addEventListener("click", event => {
        if (event.target === dialog) dialog.close();
    }, { signal });
    dialog.addEventListener("keydown", event => {
        if (event.altKey || event.ctrlKey || event.metaKey) return;
        const actions = {
            ArrowRight: () => select(index - 1),
            ArrowLeft: () => select(index + 1),
            "+": () => setZoom(zoom + .5),
            "=": () => setZoom(zoom + .5),
            "-": () => setZoom(zoom - .5),
            "0": reset,
            r: () => { rotation = (rotation + 90) % 360; render(); }
        };
        const action = actions[event.key];
        if (action) { event.preventDefault(); action(); }
    }, { signal });
    stage.addEventListener("wheel", event => {
        event.preventDefault();
        setZoom(zoom + (event.deltaY < 0 ? .2 : -.2));
    }, { signal, passive: false });
    stage.addEventListener("dblclick", event => {
        if (event.target.closest("button")) return;
        setZoom(zoom === 1 ? 2 : 1);
    }, { signal });
    stage.addEventListener("pointerdown", event => {
        if (event.target.closest("button")) return;
        stage.setPointerCapture(event.pointerId);
        pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
        dragX = event.clientX - offsetX;
        dragY = event.clientY - offsetY;
        stage.classList.add("is-dragging");
        if (pointers.size === 2) {
            const [a, b] = [...pointers.values()];
            pinchDistance = Math.hypot(a.x - b.x, a.y - b.y);
            pinchZoom = zoom;
        }
    }, { signal });
    stage.addEventListener("pointermove", event => {
        if (!pointers.has(event.pointerId)) return;
        pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
        if (pointers.size === 2) {
            const [a, b] = [...pointers.values()];
            if (pinchDistance) setZoom(pinchZoom * Math.hypot(a.x - b.x, a.y - b.y) / pinchDistance);
        } else if (zoom > 1) {
            offsetX = event.clientX - dragX;
            offsetY = event.clientY - dragY;
            render();
        }
    }, { signal });
    const endPointer = event => {
        pointers.delete(event.pointerId);
        if (pointers.size === 1) {
            const remaining = [...pointers.values()][0];
            dragX = remaining.x - offsetX;
            dragY = remaining.y - offsetY;
        }
        if (!pointers.size) stage.classList.remove("is-dragging");
    };
    stage.addEventListener("pointerup", endPointer, { signal });
    stage.addEventListener("pointercancel", endPointer, { signal });
    dialog.addEventListener("close", () => { pointers.clear(); stage.classList.remove("is-dragging"); }, { signal });

    return {
        open(nextImages, nextIndex, nextTitle) {
            images = nextImages;
            title = nextTitle;
            select(nextIndex);
            if (!dialog.open) dialog.showModal();
            find("close").focus();
        },
        dispose() {
            if (dialog.open) dialog.close();
            controller.abort();
        }
    };
}

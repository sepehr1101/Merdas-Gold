let storeMap;

async function waitForMapContainer(mapId) {
    for (let attempt = 0; attempt < 30; attempt++) {
        const element = document.getElementById(mapId);
        if (element && element.clientWidth > 0 && element.clientHeight > 0) {
            return element;
        }

        await new Promise(resolve => requestAnimationFrame(resolve));
    }

    throw new Error("Map container is not visible.");
}

export async function initializeStoreMap(mapId, latitudeId, longitudeId, zoomId, latitude, longitude, zoom) {
    if (!window.L) {
        throw new Error("Leaflet is not available.");
    }

    const mapElement = await waitForMapContainer(mapId);

    if (storeMap) {
        storeMap.remove();
    }

    storeMap = L.map(mapElement).setView([latitude, longitude], zoom);
    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(storeMap);

    const marker = L.marker([latitude, longitude], { draggable: true }).addTo(storeMap);
    const updateInputs = (lat, lng) => {
        document.getElementById(latitudeId).value = lat.toFixed(6);
        document.getElementById(longitudeId).value = lng.toFixed(6);
    };

    storeMap.on("click", event => {
        marker.setLatLng(event.latlng);
        updateInputs(event.latlng.lat, event.latlng.lng);
    });
    marker.on("dragend", event => {
        const point = event.target.getLatLng();
        updateInputs(point.lat, point.lng);
    });
    storeMap.on("zoomend", () => {
        document.getElementById(zoomId).value = storeMap.getZoom();
    });

    requestAnimationFrame(() => storeMap?.invalidateSize());
}

export function disposeStoreMap() {
    if (storeMap) {
        storeMap.remove();
        storeMap = undefined;
    }
}

(() => {
    const target = document.querySelector("#workspaceMap");
    if (!target || !window.L) return;

    const map = L.map(target).setView([50.4501, 30.5234], 6);
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: "&copy; OpenStreetMap contributors"
    }).addTo(map);
    const layer = L.layerGroup().addTo(map);

    async function load() {
        const plan = document.querySelector("#mapPlan").value;
        const response = await fetch(`/api/map/workspaces?plan=${encodeURIComponent(plan)}`);
        const workspaces = await response.json();
        layer.clearLayers();
        const bounds = [];
        for (const workspace of workspaces) {
            const point = [workspace.latitude, workspace.longitude];
            bounds.push(point);
            L.marker(point).bindPopup(`<strong>${escapeHtml(workspace.name)}</strong><br>${escapeHtml(workspace.description)}<br><small>${workspace.plan} · Документів: ${workspace.documentCount}</small><br><a href="${workspace.url}">Відкрити простір</a>`).addTo(layer);
        }
        if (bounds.length) map.fitBounds(bounds, { padding: [40, 40], maxZoom: 13 });
    }

    function escapeHtml(value) {
        const node = document.createElement("span");
        node.textContent = value ?? "";
        return node.innerHTML;
    }

    document.querySelector("#mapPlan").addEventListener("change", load);
    load();
})();

(() => {
    const form = document.querySelector("#assistantForm");
    if (!form) return;
    const messages = document.querySelector("#assistantMessages");
    const steps = document.querySelector("#assistantSteps");

    form.addEventListener("submit", event => {
        event.preventDefault();
        const prompt = document.querySelector("#assistantPrompt");
        const question = prompt.value.trim();
        if (question.length < 3) return;
        messages.insertAdjacentHTML("beforeend", `<div class="assistant-message user">${escapeHtml(question)}</div>`);
        const answer = document.createElement("div");
        answer.className = "assistant-message";
        messages.append(answer);
        steps.innerHTML = "";
        const url = `/api/assistant/stream?workspaceId=${form.dataset.workspaceId}&message=${encodeURIComponent(question)}`;
        const source = new EventSource(url);
        source.onmessage = event => {
            const data = JSON.parse(event.data);
            if (data.type === "token") answer.textContent += data.value;
            if (data.type === "step" || data.type === "tool") steps.insertAdjacentHTML("beforeend", `<li>${escapeHtml(data.value)}</li>`);
            if (data.type === "done" || data.type === "error") source.close();
        };
        source.onerror = () => source.close();
        prompt.value = "";
    });

    function escapeHtml(value) { const span = document.createElement("span"); span.textContent = value; return span.innerHTML; }
})();

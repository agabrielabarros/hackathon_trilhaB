const showToast = (message, kind = "success") => {
    const node = document.querySelector("#toast");
    if (!node) return;
    node.textContent = message;
    node.className = `toast visible ${kind}`;
    window.setTimeout(() => node.className = "toast", 3200);
};

const formJson = form => Object.fromEntries(new FormData(form).entries());
const errorText = data => data.detail || Object.values(data.errors || {})[0]?.[0] || "A operacao falhou.";

document.addEventListener("submit", async event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement)) return;

    if (form.id === "squad-form") {
        event.preventDefault();
        const button = form.querySelector("button[type='submit']");
        const label = button.querySelector(".button-label");
        button.disabled = true;
        label.textContent = "PO organizando o backlog...";
        try {
            const response = await fetch("/api/squad/run", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(formJson(form))
            });
            const data = await response.json();
            if (!response.ok) throw new Error(errorText(data));
            showToast(`Release #${data.id} liberada pelo QA.`);
            window.setTimeout(() => window.location.reload(), 450);
        } catch (error) {
            showToast(error.message, "error");
            button.disabled = false;
            label.textContent = "Iniciar squad autonomo";
        }
    }

    if (form.id === "nonconformity-form") {
        event.preventDefault();
        const button = form.querySelector("button[type='submit']");
        button.disabled = true;
        try {
            const response = await fetch("/api/nonconformities", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(formJson(form))
            });
            const data = await response.json();
            if (!response.ok) throw new Error(errorText(data));
            showToast(`Nao conformidade #${data.id} registrada com evidencia.`);
            window.setTimeout(() => window.location.reload(), 450);
        } catch (error) {
            showToast(error.message, "error");
            button.disabled = false;
        }
    }
});

document.addEventListener("click", async event => {
    const button = event.target.closest("[data-root-cause]");
    if (!button) return;
    button.disabled = true;
    button.textContent = "Analisando historico...";
    try {
        const response = await fetch(`/api/nonconformities/${button.dataset.rootCause}/root-cause`, { method: "POST" });
        const data = await response.json();
        if (!response.ok) throw new Error(errorText(data));
        showToast(`Analise concluida com ${data.confidence}% de confianca.`);
        window.setTimeout(() => window.location.reload(), 450);
    } catch (error) {
        showToast(error.message, "error");
        button.disabled = false;
        button.textContent = "Gerar analise assistida";
    }
});

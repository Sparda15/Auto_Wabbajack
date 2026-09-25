// Selectors are based on Nexus markup supplied and inspected on 2026-09-25.
// Never read download-url, change attributes, or invoke page internals.
(request, commitDocument) => {
    "use strict";
    try {
        const url = new URL(location.href);
        if (url.protocol !== "https:" ||
            !["www.nexusmods.com", "nexusmods.com"].includes(url.hostname) ||
            url.port || url.username || url.password ||
            url.pathname.replace(/\/$/, "") !== request.path ||
            url.searchParams.getAll("tab").length !== 1 ||
            url.searchParams.get("tab") !== "files" ||
            url.searchParams.getAll("file_id").length !== 1 ||
            url.searchParams.get("file_id") !== request.fileId)
            return { status: "unavailable" };

        const visible = element => {
            if (!element?.isConnected || !element.getClientRects().length) return false;
            for (let node = element; node; node = node.parentElement || node.getRootNode().host) {
                const style = getComputedStyle(node);
                if (node.hidden || node.inert || node.getAttribute("aria-hidden") === "true" ||
                    style.display === "none" || style.visibility !== "visible" ||
                    Number(style.opacity) === 0) return false;
            }
            return true;
        };
        const usable = button => visible(button) && !button.disabled &&
            !button.matches(":disabled") && button.getAttribute("aria-disabled") !== "true" &&
            !button.closest('a, [inert], [aria-disabled="true"]') &&
            getComputedStyle(button).pointerEvents !== "none";
        const label = element => element?.textContent.replace(/\s+/g, " ").trim();
        const single = elements => elements.length === 1 ? elements[0] : null;

        const challenge = scope => [...scope.querySelectorAll(
            'iframe[src*="challenges.cloudflare.com"], iframe[src*="recaptcha"], ' +
            'iframe[src*="hcaptcha"], #challenge-running, #challenge-stage'
        )].some(visible);
        const login = scope => [...scope.querySelectorAll('input[type="password"]')].some(visible);
        if (challenge(document)) return { status: "unavailable", reason: "captcha" };
        if (login(document)) return { status: "unavailable", reason: "login" };

        const components = [...document.querySelectorAll("mod-file-download")].filter(visible);
        if (components.length !== 1) return { status: "unavailable" };
        const component = components[0];
        if (component.getAttribute("user-is-logged-in") === "false")
            return { status: "unavailable", reason: "login" };
        if (component.getAttribute("file-id") !== request.fileId ||
            component.getAttribute("game-domain") !== request.game ||
            component.getAttribute("user-is-logged-in") !== "true" ||
            component.getAttribute("is-nmm-download") !== "false")
            return { status: "unavailable" };

        const root = component.shadowRoot;
        if (!root) return { status: "unavailable" };
        if (challenge(root)) return { status: "unavailable", reason: "captcha" };
        if (login(root)) return { status: "unavailable", reason: "login" };

        // Only this component's known large-file modal is eligible. Do not search
        // arbitrary document portals for buttons with the same text.
        const modal = single([...root.querySelectorAll(".nxm-modal-content")].filter(visible));
        let standard = null;
        if (modal && Number(component.getAttribute("filesize-bytes")) > 500000000 &&
            label(modal.querySelector(".nxm-modal-header h2.text-heading-xs")) ===
                "Download this large file without losing progress" &&
            [...modal.querySelectorAll(".nxm-modal-body p")].some(p =>
                label(p)?.startsWith("This file is over 500MB.")) &&
            modal.querySelector('.nxm-modal-body a[href="https://help.nexusmods.com/article/170-resumable-downloads"]')) {
            const buttons = [...modal.querySelectorAll('.nxm-modal-footer > button[type="button"]')];
            const normal = single(buttons.filter(button =>
                button.matches(".nxm-button.nxm-button-secondary") &&
                label(button) === "Standard download" && usable(button)));
            const resumable = single(buttons.filter(button =>
                button.matches(".nxm-button.nxm-button-secondary-filled-strong") &&
                label(button.querySelector("span.flex > span")) === "Resumable download" &&
                label(button.querySelector(".nxm-pill-label")) === "Beta" && visible(button)));
            if (buttons.length === 2 && normal && resumable) standard = normal;
        }

        const blocked = (scope, allowedModal = null) => [...scope.querySelectorAll(
            'dialog[open], [role="dialog"], [aria-modal="true"], .nxm-modal-content, input[type="password"], ' +
            'iframe[src*="challenges.cloudflare.com"], iframe[src*="recaptcha"], ' +
            'iframe[src*="hcaptcha"], iframe[title="SP Consent Message"], #challenge-running, #challenge-stage'
        )].some(element => visible(element) &&
            !(allowedModal && (element === allowedModal ||
                (element.matches('dialog[open], [role="dialog"], [aria-modal="true"]') &&
                 element.contains(allowedModal)))));
        if (blocked(document) || blocked(root, standard ? modal : null))
            return { status: "unavailable", reason: "dialog" };

        const key = "__wabbajackNexusAutoDownload";
        let state = window[key];
        if (!commitDocument && (!state || state.session !== request.session)) {
            state = { session: request.session, document: crypto.randomUUID(),
                choiceClicked: false, standardClicked: false };
            window[key] = state;
        }
        if (!state || state.session !== request.session) return { status: "unavailable" };
        if (state.standardClicked) return { status: "handled" };

        let button = standard;
        let action = "Standard";
        if (!button) {
            if (state.choiceClicked) return { status: "waiting" };
            const section = root.querySelector("#download-section");
            if (!section) return { status: "unavailable" };

            // Exact label, real button, component identity and explicit Premium state.
            // Free-trial/upgrade promotions do not satisfy this predicate.
            const fast = component.getAttribute("user-is-premium") === "true"
                ? single([...section.querySelectorAll('button.nxm-button[type="button"]')]
                    .filter(candidate => usable(candidate) && label(candidate) === "Fast download"))
                : null;
            const slow = single([...section.querySelectorAll(
                '#upsell-cards button.nxm-button.nxm-button-secondary-filled-weak[type="button"]'
            )].filter(candidate => usable(candidate) && label(candidate) === "Slow download"));
            button = fast || slow;
            action = fast ? "Fast" : "Slow";
        }
        if (!button) return { status: "unavailable" };

        // Bind the confirmation to both document and action: a newly opened modal
        // must not consume a pending Slow/Fast confirmation (or vice versa).
        const ticket = state.document + ":" + action;
        if (!commitDocument) return { status: "ready", action, document: ticket };
        if (commitDocument !== ticket) return { status: "unavailable" };
        state.choiceClicked = true;
        if (action === "Standard") state.standardClicked = true;
        button.click();
        return { status: "clicked", action };
    } catch {
        return { status: "error", reason: "error" };
    }
}

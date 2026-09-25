const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const { JSDOM } = require("jsdom");
const script = fs.readFileSync(path.join(__dirname, "../Wabbajack.App.Wpf/UserIntervention/NexusAutoDownload.js"), "utf8");
const url = "https://www.nexusmods.com/vampirebloodlines/mods/176?tab=files&file_id=1290";
const request = { session: "test", path: "/vampirebloodlines/mods/176", game: "/vampirebloodlines", fileId: "1290" };

function fixture({ premium = false, fast = "Fast download - free trial" } = {}) {
    const dom = new JSDOM("<mod-file-download></mod-file-download>", { url, runScripts: "outside-only" });
    const win = dom.window;
    // jsdom has no layout engine. Only geometry is supplied by the fixture.
    win.HTMLElement.prototype.getClientRects = function () { return this.hidden ? [] : [{ width: 100, height: 30 }]; };
    const computed = win.getComputedStyle.bind(win);
    win.getComputedStyle = element => {
        const style = computed(element);
        return { display: style.display, visibility: style.visibility || "visible",
            opacity: style.opacity || "1", pointerEvents: style.pointerEvents };
    };
    const component = win.document.querySelector("mod-file-download");
    for (const [name, value] of Object.entries({
        "file-id": "1290", "game-domain": "/vampirebloodlines",
        "user-is-logged-in": "true", "user-is-premium": String(premium),
        "is-nmm-download": "false", "timeout-seconds": "5", "download-url": "#ERROR-download-location-not-found"
    })) component.setAttribute(name, value);
    const root = component.attachShadow({ mode: "open" });
    root.innerHTML = '<div id="download-section"><div id="upsell-cards">' +
        '<button id="slow" class="nxm-button nxm-button-secondary-filled-weak" type="button"><span>Slow download</span></button>' +
        '<button id="fast" class="nxm-button" type="button"><span>' + fast + '</span></button></div></div>';
    const clicks = [];
    root.querySelector("#slow").addEventListener("click", () => clicks.push("Slow"));
    root.querySelector("#fast").addEventListener("click", () => clicks.push("Fast"));
    const run = win.eval("(" + script + ")");
    const attempt = () => {
        const probe = run(request, null);
        return probe.status === "ready" ? run(request, probe.document) : probe;
    };
    return { dom, win, component, root, clicks, run, attempt };
}
function check(name, action, options) {
    test(name, () => { const f = fixture(options); try { action(f); } finally { f.dom.window.close(); } });
}

check("free account selects Slow, never the trial promotion", f => {
    assert.equal(f.attempt().status, "clicked");
    assert.deepEqual(f.clicks, ["Slow"]);
    assert.equal(f.component.getAttribute("timeout-seconds"), "5");
    assert.equal(f.component.getAttribute("download-url"), "#ERROR-download-location-not-found");
});
check("Premium with an exact usable Fast button selects Fast", f => {
    f.attempt(); assert.deepEqual(f.clicks, ["Fast"]);
}, { premium: true, fast: "Fast download" });
check("even Premium never selects the free-trial promotion", f => {
    f.attempt(); assert.deepEqual(f.clicks, ["Slow"]);
}, { premium: true });
check("a free account cannot select a button merely labelled Fast", f => {
    f.attempt(); assert.deepEqual(f.clicks, ["Slow"]);
}, { fast: "Fast download" });
check("duplicate probes and commits click at most once", f => {
    const probe = f.run(request, null);
    for (let n = 0; n < 20; n++) f.run(request, probe.document);
    f.attempt(); assert.deepEqual(f.clicks, ["Slow"]);
});
check("delayed Shadow DOM controls can be found by later polling", f => {
    const section = f.root.querySelector("#download-section");
    section.remove();
    assert.equal(f.attempt().status, "unavailable");
    f.root.append(section);
    f.attempt(); assert.deepEqual(f.clicks, ["Slow"]);
});
check("a probe alone schedules no click when Auto is switched off", f => {
    assert.equal(f.run(request, null).status, "ready");
    f.root.querySelector("#slow").textContent = "Slow download";
    assert.deepEqual(f.clicks, []);
});
check("stale document/session nonce cannot click", f => {
    const probe = f.run(request, null);
    f.run({ ...request, session: "next" }, null);
    assert.equal(f.run(request, probe.document).status, "unavailable");
    assert.deepEqual(f.clicks, []);
});
check("logged out component remains manual", f => {
    f.component.setAttribute("user-is-logged-in", "false");
    assert.equal(f.attempt().status, "unavailable"); assert.deepEqual(f.clicks, []);
});
check("wrong file component remains manual", f => {
    f.component.setAttribute("file-id", "1335");
    assert.equal(f.attempt().status, "unavailable");
});
check("unrelated navigation between probe and commit cannot click", f => {
    const probe = f.run(request, null);
    f.dom.reconfigure({ url: "https://example.org/" });
    assert.equal(f.run(request, probe.document).status, "unavailable");
    assert.deepEqual(f.clicks, []);
});
check("buttons outside the component are ignored", f => {
    f.root.querySelector("#slow").remove();
    f.win.document.body.insertAdjacentHTML("beforeend", '<button class="nxm-button">Slow download</button>');
    assert.equal(f.attempt().status, "unavailable");
});
check("duplicate matching controls fail open", f => {
    f.root.querySelector("#upsell-cards").append(f.root.querySelector("#slow").cloneNode(true));
    assert.equal(f.attempt().status, "unavailable");
});
check("disabled or hidden buttons are not clicked", f => {
    f.root.querySelector("#slow").disabled = true;
    assert.equal(f.attempt().status, "unavailable");
    f.root.querySelector("#slow").disabled = false;
    f.root.querySelector("#slow").hidden = true;
    assert.equal(f.attempt().status, "unavailable");
});
check("a visible security or consent dialog prevents action", f => {
    f.win.document.body.insertAdjacentHTML("beforeend", '<dialog open>Consent or security check</dialog>');
    assert.equal(f.attempt().status, "unavailable");
});
check("missing selectors leave the page interactive", f => {
    f.root.querySelector("#slow").className = "changed-by-site";
    assert.equal(f.attempt().status, "unavailable");
    f.root.querySelector("#slow").click();
    assert.deepEqual(f.clicks, ["Slow"]);
});
check("script exception returns error rather than escaping", f => {
    f.win.getComputedStyle = () => { throw new Error("test"); };
    assert.equal(f.attempt().status, "error");
});
check("a new request/session can act on the next file", f => {
    f.attempt();
    f.dom.reconfigure({ url: url.replace("1290", "1291") });
    f.component.setAttribute("file-id", "1291");
    const next = { ...request, session: "next", fileId: "1291" };
    const probe = f.run(next, null);
    assert.equal(f.run(next, probe.document).status, "clicked");
    assert.deepEqual(f.clicks, ["Slow", "Slow"]);
});
check("the consent iframe is not clicked through", f => {
    f.win.document.body.insertAdjacentHTML("beforeend", '<iframe title="SP Consent Message"></iframe>');
    assert.equal(f.attempt().status, "unavailable");
    assert.deepEqual(f.clicks, []);
});
check("a button disabled after the probe is not clicked", f => {
    const probe = f.run(request, null);
    f.root.querySelector("#slow").disabled = true;
    assert.equal(f.run(request, probe.document).status, "unavailable");
    assert.deepEqual(f.clicks, []);
});

// Fixture transcribed from the user's Nexus large-file dialog. The modal belongs
// to the requested component's open Shadow DOM, as do its download controls.
function largeFileDialog(f, parent = f.root) {
    f.component.setAttribute("filesize-bytes", "863700000");
    const dialog = f.win.document.createElement("div");
    dialog.setAttribute("role", "dialog");
    dialog.setAttribute("aria-modal", "true");
    dialog.innerHTML = `
      <div class="nxm-modal-content scrollbar">
        <div class="nxm-modal-header">
          <h2 class="text-heading-xs text-neutral-strong">Download this large file without losing progress</h2>
          <button aria-label="Close" class="nxm-modal-close-button">Close</button>
        </div>
        <div class="nxm-modal-body pt-0!">
          <div class="text-body-md text-neutral-moderate space-y-4">
            <p>This file is over 500MB. Use resumable downloads so you can pause, track progress, and recover if your connection drops.</p>
            <p><a href="https://help.nexusmods.com/article/170-resumable-downloads" rel="noreferrer" target="_blank">Learn more about resumable downloads</a></p>
          </div>
        </div>
        <div class="nxm-modal-footer flex items-center justify-end gap-4">
          <button class="nxm-button nxm-button-secondary" type="button"><span>Standard download</span></button>
          <button class="nxm-button nxm-button-secondary-filled-strong" type="button"><span class="flex items-center gap-x-1.5"><span>Resumable download</span><span class="nxm-pill nxm-pill-info"><span class="nxm-pill-label">Beta</span></span></span></button>
        </div>
      </div>`;
    dialog.querySelector(".nxm-button-secondary").addEventListener("click", () => f.clicks.push("Standard"));
    dialog.querySelector(".nxm-button-secondary-filled-strong").addEventListener("click", () => f.clicks.push("Resumable"));
    dialog.querySelector(".nxm-modal-close-button").addEventListener("click", () => f.clicks.push("Close"));
    parent.append(dialog);
    return dialog;
}

check("Slow then a delayed large-file modal selects Standard once", f => {
    f.attempt();
    for (let i = 0; i < 10; i++) assert.equal(f.attempt().status, "waiting");
    const modal = largeFileDialog(f);
    const probe = f.run(request, null);
    assert.equal(probe.action, "Standard");
    for (let i = 0; i < 10; i++) f.run(request, probe.document);
    assert.deepEqual(f.clicks, ["Slow", "Standard"]);
    modal.remove();
    assert.equal(f.attempt().status, "handled");
});
check("ON with the large-file dialog already open only clicks Standard", f => {
    largeFileDialog(f);
    f.attempt();
    assert.deepEqual(f.clicks, ["Standard"]);
});
check("Fast can also be followed by Standard without another primary click", f => {
    f.attempt();
    largeFileDialog(f);
    f.attempt();
    assert.deepEqual(f.clicks, ["Fast", "Standard"]);
}, { premium: true, fast: "Fast download" });
check("the background section may be aria-hidden while the owned dialog is open", f => {
    f.attempt();
    f.root.querySelector("#download-section").setAttribute("aria-hidden", "true");
    largeFileDialog(f);
    f.attempt();
    assert.deepEqual(f.clicks, ["Slow", "Standard"]);
});
check("a modal cannot consume a pending Slow confirmation", f => {
    const slow = f.run(request, null);
    largeFileDialog(f);
    assert.equal(f.run(request, slow.document).status, "unavailable");
    assert.deepEqual(f.clicks, []);
});
check("a closed modal cannot turn a pending Standard into a Slow click", f => {
    const dialog = largeFileDialog(f);
    const standard = f.run(request, null);
    dialog.remove();
    assert.equal(f.run(request, standard.document).status, "unavailable");
    assert.deepEqual(f.clicks, []);
});
check("a second unrelated dialog still blocks the recognized modal", f => {
    largeFileDialog(f);
    f.root.append(f.win.document.createElement("dialog"));
    f.root.querySelector("dialog").setAttribute("open", "");
    assert.equal(f.attempt().status, "unavailable");
    assert.deepEqual(f.clicks, []);
});
check("a document-level consent dialog still blocks the recognized modal", f => {
    largeFileDialog(f);
    f.win.document.body.insertAdjacentHTML("beforeend", '<div role="dialog">Consent</div>');
    assert.equal(f.attempt().status, "unavailable");
});
check("a challenge inside the recognized modal is not exempted", f => {
    const dialog = largeFileDialog(f);
    dialog.insertAdjacentHTML("beforeend", '<iframe src="https://challenges.cloudflare.com/"></iframe>');
    assert.equal(f.attempt().status, "unavailable");
});
check("a lookalike document portal outside the component is not clicked", f => {
    largeFileDialog(f, f.win.document.body);
    assert.equal(f.attempt().status, "unavailable");
    assert.deepEqual(f.clicks, []);
});
check("two large-file dialogs are ambiguous and remain manual", f => {
    largeFileDialog(f); largeFileDialog(f);
    assert.equal(f.attempt().status, "unavailable");
});
check("a different modal title remains manual", f => {
    const dialog = largeFileDialog(f);
    dialog.querySelector("h2").textContent = "Upgrade your account";
    assert.equal(f.attempt().status, "unavailable");
});
check("a changed help link does not qualify as the known modal", f => {
    const dialog = largeFileDialog(f);
    dialog.querySelector("a").href = "https://example.org/";
    assert.equal(f.attempt().status, "unavailable");
});
check("Standard alone is not enough to identify the large-file modal", f => {
    const dialog = largeFileDialog(f);
    dialog.querySelector(".nxm-button-secondary-filled-strong").remove();
    assert.equal(f.attempt().status, "unavailable");
});
check("a disabled Standard does not fall back to Resumable", f => {
    const dialog = largeFileDialog(f);
    dialog.querySelector(".nxm-button-secondary").disabled = true;
    assert.equal(f.attempt().status, "unavailable");
    assert.deepEqual(f.clicks, []);
});
check("a small file with a large-file lookalike dialog remains manual", f => {
    largeFileDialog(f);
    f.component.setAttribute("filesize-bytes", "437087232");
    assert.equal(f.attempt().status, "unavailable");
});
check("turning Auto off after a modal probe schedules no confirmation", f => {
    largeFileDialog(f);
    assert.equal(f.run(request, null).action, "Standard");
    f.root.querySelector(".nxm-modal-body p").textContent += " ";
    assert.deepEqual(f.clicks, []);
});
check("a pending Standard cannot click for another file", f => {
    largeFileDialog(f);
    const probe = f.run(request, null);
    f.component.setAttribute("file-id", "1335");
    assert.equal(f.run(request, probe.document).status, "unavailable");
    assert.deepEqual(f.clicks, []);
});

check("document CAPTCHA is diagnosed even before component loads", f => {
    f.component.remove();
    f.win.document.body.innerHTML = '<div id="challenge-stage">Verify</div>';
    assert.equal(f.attempt().reason, "captcha");
    assert.deepEqual(f.clicks, []);
});
check("logged-out page reports login without a click", f => {
    f.component.setAttribute("user-is-logged-in", "false");
    assert.equal(f.attempt().reason, "login");
    assert.deepEqual(f.clicks, []);
});
check("shadow CAPTCHA reports its cause", f => {
    f.root.innerHTML += '<iframe src="https://challenges.cloudflare.com/test"></iframe>';
    assert.equal(f.attempt().reason, "captcha");
    assert.deepEqual(f.clicks, []);
});
check("hidden CAPTCHA does not block a normal download", f => {
    f.root.innerHTML += '<div id="challenge-stage" hidden></div>';
    assert.equal(f.attempt().status, "clicked");
});
check("unknown visible dialog reports manual intervention", f => {
    f.root.innerHTML += '<div role="dialog">Consent</div>';
    assert.equal(f.attempt().reason, "dialog");
    assert.deepEqual(f.clicks, []);
});

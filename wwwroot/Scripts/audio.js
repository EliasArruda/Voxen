let callback, audio, version = 0, level = .7, hls, loadingTimer;
export function initialize(reference, volume) { callback = reference; level = volume; }
function detach() {
    clearTimeout(loadingTimer);
    hls?.destroy(); hls = undefined;
    if (!audio) return;
    const old = audio; audio = undefined;
    old.pause(); old.removeAttribute('src'); old.load();
}
export function stop(nextVersion) { version = nextVersion; detach(); }
export async function load(url, nextVersion, isHls = false) {
    detach(); version = nextVersion;
    const current = new Audio(); audio = current;
    current.preload = 'metadata'; current.volume = level;
    let lastUpdate = 0;
    const report = type => {
        if (audio !== current || version !== nextVersion) return;
        if (type === 'playing' || type === 'error') clearTimeout(loadingTimer);
        callback.invokeMethodAsync('OnAudioEvent', nextVersion, type, current.currentTime || 0,
            Number.isFinite(current.duration) ? current.duration : 0).catch(() => {});
    };
    loadingTimer = setTimeout(() => report('error'), 20000);
    for (const event of ['playing', 'pause', 'ended', 'error', 'loadedmetadata', 'seeked'])
        current.addEventListener(event, () => report(event));
    current.addEventListener('timeupdate', () => {
        if (performance.now() - lastUpdate < 250) return;
        lastUpdate = performance.now(); report('time');
    });
    if (isHls && !current.canPlayType('application/vnd.apple.mpegurl')) {
        try {
            if (!window.Hls) await new Promise((resolve, reject) => {
                const script = document.createElement('script');
                script.src = new URL('./hls.light.min.js', import.meta.url).href;
                script.onload = resolve; script.onerror = reject; document.head.appendChild(script);
            });
            const Hls = window.Hls;
            if (audio !== current || version !== nextVersion) return;
            if (!Hls.isSupported()) { report('error'); return; }
            hls = new Hls({maxBufferLength: 30, maxMaxBufferLength: 60});
            hls.on(Hls.Events.ERROR, (_, data) => { if (data.fatal) report('error'); });
            hls.on(Hls.Events.MANIFEST_PARSED, () => current.play().catch(() => report('error')));
            hls.loadSource(url); hls.attachMedia(current);
        } catch { report('error'); }
    } else {
        current.src = url;
        current.play().catch(() => report('error'));
    }
}
export function pause() { audio?.pause(); }
export function resume() {
    const current = audio, currentVersion = version;
    current?.play().catch(() => {
        if (audio !== current || version !== currentVersion) return;
        callback.invokeMethodAsync('OnAudioEvent', currentVersion, 'error', current.currentTime || 0,
            Number.isFinite(current.duration) ? current.duration : 0).catch(() => {});
    });
}
export function seek(seconds) { if (audio && Number.isFinite(audio.duration)) audio.currentTime = seconds; }
export function volume(value) { level = value; if (audio) audio.volume = value; }
export function dispose() { version++; detach(); callback = undefined; }

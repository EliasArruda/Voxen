export function initializeVolume(input, output, reference, initial, initialRevision = 0) {
    let dragging = false, pending, sending = false, disposed = false, revision = initialRevision;
    const paint = value => { input.style.setProperty('--volume', `${value}%`); output.textContent = `${Math.round(value)}%`; };
    const sync = (value, currentRevision = revision) => {
        if (currentRevision !== revision) {
            revision = currentRevision; pending = undefined;
            input.value = value * 100; paint(value * 100); return;
        }
        if (!dragging && !sending && pending === undefined) { input.value = value * 100; paint(value * 100); }
    };
    const send = async () => {
        if (sending) return;
        sending = true;
        try {
            while (pending !== undefined && !disposed) {
                const value = pending; pending = undefined;
                await reference.invokeMethodAsync('SetVolume', value.value / 100, value.revision);
            }
        } catch { pending = undefined; }
        finally { sending = false; }
    };
    const update = () => { paint(Number(input.value)); pending = { value: Number(input.value), revision }; void send(); };
    const begin = () => { dragging = true; };
    const end = () => { dragging = false; };
    input.addEventListener('input', update); input.addEventListener('pointerdown', begin);
    window.addEventListener('pointerup', end); window.addEventListener('pointercancel', end);
    sync(initial);
    return { sync, dispose() { disposed = true; input.removeEventListener('input', update); input.removeEventListener('pointerdown', begin); window.removeEventListener('pointerup', end); window.removeEventListener('pointercancel', end); } };
}

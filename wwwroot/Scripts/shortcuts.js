let reference, listener, focusObserver, focusTimer, helpTimer, helpReturn;
const editing = target => !!target?.closest?.('input:not([type="range"]),textarea,select,[contenteditable="true"],[role="textbox"]');
export function actionFor(event) {
    if (event.defaultPrevented || event.isComposing || event.repeat) return null;
    const key = event.key.toLowerCase(), command = event.ctrlKey || event.metaKey;
    if (command && !event.altKey && key === 'k') return 'search';
    if (editing(event.target)) return null;
    if (key === 'escape') return 'dismiss';
    if (event.altKey && !command && !event.shiftKey) return key === 'l' ? 'library' : key === 'h' ? 'home' : null;
    if (command || event.altKey) return null;
    if (key === '?' || (key === '/' && event.shiftKey)) return 'help';
    if (event.shiftKey) return null;
    if (key === ' ') return 'toggle';
    if (key.startsWith('arrow') && event.target?.closest?.('button,a,[role="button"],input[type="range"]')) return null;
    return ({' ': 'toggle', '/': 'search', n: 'next', p: 'previous', arrowright: 'forward', arrowleft: 'backward', arrowup: 'volumeup', arrowdown: 'volumedown', m: 'mute', f: 'favorite', b: 'save', q: 'queue'})[key] ?? null;
}
export function initialize(callback) {
    dispose(); reference = callback;
    listener = event => {
        const action = actionFor(event);
        if (!action) return;
        event.preventDefault(); reference?.invokeMethodAsync('ExecuteShortcut', action).catch(() => {});
    };
    document.addEventListener('keydown', listener);
}
export function focusSearch() {
    focusObserver?.disconnect(); clearTimeout(focusTimer);
    const settle = () => {
        if (!(document.getElementById('topbar-search') || document.getElementById('music-search'))) return false;
        // Route focus runs after the render batch. Apply the requested search focus after that navigation settles.
        focusObserver?.disconnect(); clearTimeout(focusTimer);
        focusTimer = setTimeout(() => {
            const input = (document.getElementById('topbar-search') || document.getElementById('music-search'));
            if (input) { input.focus(); input.select(); }
        }, 200);
        return true;
    };
    if (settle()) return;
    focusObserver = new MutationObserver(settle); focusObserver.observe(document.body, {childList:true,subtree:true});
    focusTimer = setTimeout(() => focusObserver?.disconnect(), 3000);
}
export function focusHelp() {
    helpReturn = document.activeElement;
    clearTimeout(helpTimer);
    helpTimer = setTimeout(() => document.getElementById('shortcuts-close')?.focus(), 0);
}
export function restoreHelpFocus() {
    clearTimeout(helpTimer);
    if (helpReturn?.isConnected) helpReturn.focus();
    helpReturn = undefined;
}
export function dispose() {
    if (listener) document.removeEventListener('keydown', listener);
    listener = undefined; reference = undefined; focusObserver?.disconnect(); clearTimeout(focusTimer); clearTimeout(helpTimer);
}

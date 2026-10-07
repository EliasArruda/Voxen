export function applyPalette(palette, language, enabled = true) {
    document.documentElement.lang = language;
    document.documentElement.dataset.coverColors = String(enabled);
    const style=document.documentElement.style;
    if (!palette) { for(const key of ['--bg','--surface','--raised','--hover','--primary','--line','--album-tint'])style.removeProperty(key);delete document.documentElement.dataset.albumTheme;return; }
    const rgb=[palette.r,palette.g,palette.b];
    const mix=(amount,base)=>rgb.map(v=>Math.round(v*amount+base*(1-amount))).join(' ');
    style.setProperty('--bg',`rgb(${mix(.13,10)})`);
    style.setProperty('--surface',`rgb(${mix(.16,13)} / .86)`);
    style.setProperty('--raised',`rgb(${mix(.24,20)} / .88)`);
    style.setProperty('--hover',`rgb(${mix(.32,26)} / .92)`);
    style.setProperty('--primary',`rgb(${mix(.42,255)})`);
    style.setProperty('--line',`rgb(${mix(.4,210)} / .2)`);
    style.setProperty('--album-tint',`rgb(${mix(.45,8)} / .82)`);
    document.documentElement.dataset.albumTheme='true';
}

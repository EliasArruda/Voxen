export function applyPalette(palette, language, enabled = true) {
    document.documentElement.lang = language;
    document.documentElement.dataset.coverColors = String(enabled);
    const style=document.documentElement.style;
    if (!palette) { for(const key of ['--bg','--surface','--raised','--hover','--primary','--line','--album-tint','--ink','--muted','--selected','--youtube','--soundcloud'])style.removeProperty(key);delete document.documentElement.dataset.albumTheme;return; }
    const rgb=[palette.r,palette.g,palette.b];
    const mix=(amount,base)=>rgb.map(v=>Math.round(v*amount+base*(1-amount))).join(' ');
    style.setProperty('--bg',`rgb(${mix(.13,10)})`);
    style.setProperty('--surface',`rgb(${mix(.27,7)} / .90)`);
    style.setProperty('--raised',`rgb(${mix(.34,10)} / .94)`);
    style.setProperty('--hover',`rgb(${mix(.40,12)} / .96)`);
    style.setProperty('--primary',`rgb(${mix(.42,255)})`);
    style.setProperty('--line',`rgb(${mix(.4,210)} / .2)`);
    style.setProperty('--album-tint',`rgb(${mix(.35,8)} / .18)`);
    style.setProperty('--ink',`rgb(${mix(.08,255)})`);
    style.setProperty('--muted',`rgb(${mix(.12,222)})`);
    style.setProperty('--selected',`rgb(${mix(.42,255)} / .14)`);
    style.setProperty('--youtube',`rgb(${mix(.42,255)})`);
    style.setProperty('--soundcloud',`rgb(${mix(.42,255)})`);
    document.documentElement.dataset.albumTheme='true';
}

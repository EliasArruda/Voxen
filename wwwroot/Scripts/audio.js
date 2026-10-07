let callback, audio, version = 0, level = .7, hls, loadingTimer;
export function initialize(reference, volume) { callback = reference; level = volume; }
function detach() {
    clearTimeout(loadingTimer);
    hls?.destroy(); hls = undefined;
    if (!audio) return;
    sourceNode?.disconnect();for(const node of toneNodes)node.disconnect();toneNodes=[];sourceNode=undefined;for(const node of balanceNodes)node.disconnect();balanceNodes=[];
    const old = audio; audio = undefined;
    old.pause(); old.removeAttribute('src'); old.load();
}
export function stop(nextVersion) { version = nextVersion; detach(); }
export async function load(url, nextVersion, isHls = false) {
    detach(); version = nextVersion;
    const current = new Audio(); audio = current;
    current.preload = 'metadata'; current.crossOrigin = 'anonymous'; current.volume = level;
    attachTone(current);
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
    if(context?.state==='suspended')void context.resume().catch(()=>{});
    current?.play().catch(() => {
        if (audio !== current || version !== currentVersion) return;
        callback.invokeMethodAsync('OnAudioEvent', currentVersion, 'error', current.currentTime || 0,
            Number.isFinite(current.duration) ? current.duration : 0).catch(() => {});
    });
}
export function seek(seconds) { if (audio && Number.isFinite(audio.duration)) audio.currentTime = seconds; }
export function volume(value) { level = value; if (audio) audio.volume = value; }
export function dispose() { version++; detach(); void context?.close();context=undefined; callback = undefined; }

let toneSettings={bass:0,mid:0,treble:0}, context, sourceNode, toneNodes=[], balanceNodes=[];
const bands=[["subBass",32,"peaking"],["bass",100,"lowshelf"],["lowMid",300,"peaking"],["mid",1000,"peaking"],["highMid",3000,"peaking"],["treble",8000,"highshelf"],["air",16000,"peaking"]];
export function tone(settings) {
    toneSettings=settings;
    if (!context) return;
    const values=bands.map(([key])=>settings[key]||0);
    for(let i=0;i<7;i++)toneNodes[i]?.gain.setTargetAtTime(values[i],context.currentTime,.04);
    const headroom=Math.pow(10,-values.reduce((sum,gain)=>sum+Math.max(0,gain),0)/20);
    toneNodes[7]?.gain.setTargetAtTime(headroom,context.currentTime,.04);
    const balance=settings.balance||0;
    balanceNodes[1]?.gain.setTargetAtTime(1-Math.max(0,balance),context.currentTime,.04);
    balanceNodes[2]?.gain.setTargetAtTime(1+Math.min(0,balance),context.currentTime,.04);
}
function attachTone(current) {
    const AudioContext=globalThis.AudioContext || globalThis.webkitAudioContext;
    if (!AudioContext) return;
    context ||= new AudioContext();sourceNode=context.createMediaElementSource(current);
    toneNodes=bands.map(([,frequency,type])=>{const node=context.createBiquadFilter();node.type=type;node.frequency.value=frequency;node.Q.value=.707;return node;});
    toneNodes.push(context.createGain());
    let previous=sourceNode;for(const node of toneNodes){previous.connect(node);previous=node;}
    if(context.createChannelSplitter && context.createChannelMerger) {
        previous.channelCount=2;previous.channelCountMode='explicit';previous.channelInterpretation='speakers';
        const splitter=context.createChannelSplitter(2), left=context.createGain(), right=context.createGain(), merger=context.createChannelMerger(2);
        previous.connect(splitter);splitter.connect(left,0);splitter.connect(right,1);left.connect(merger,0,0);right.connect(merger,0,1);merger.connect(context.destination);
        balanceNodes=[splitter,left,right,merger];
    }else previous.connect(context.destination);
    tone(toneSettings);void context.resume().catch(()=>{});
}

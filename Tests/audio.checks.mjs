import { readFile } from 'node:fs/promises';
const source = await readFile(new URL('../wwwroot/Scripts/audio.js', import.meta.url), 'utf8');
let rejectResume, plays = 0;
class TestAudio {
    currentTime = 2; duration = 12;
    pause() {} removeAttribute() {} load() {} addEventListener() {}
    play() { plays++; return plays === 2 ? new Promise((_, reject) => { rejectResume = reject; }) : Promise.resolve(); }
}
globalThis.Audio = TestAudio;
const module = await import('data:text/javascript;base64,' + Buffer.from(source).toString('base64'));
const events = [];
module.initialize({invokeMethodAsync: (...args) => { events.push(args); return Promise.resolve(); }}, .7);
await module.load('first', 1);
module.resume();
await module.load('second', 2);
rejectResume(new Error('interrupted old playback'));
await new Promise(resolve => setImmediate(resolve));
if (events.length) throw new Error('Old resume failure leaked into newest track: ' + JSON.stringify(events));
module.dispose();
console.log('PASS Old resume rejection cannot report an error for a newer audio element');

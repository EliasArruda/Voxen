import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
const nodes=[];
class Node {
    constructor(){this.gain={value:0,setTargetAtTime(value){this.value=value;}};this.frequency={value:0};this.Q={value:0};nodes.push(this);}
    connect(next){this.next=next;return next;}disconnect(){this.disconnected=true;}
}
class Context {
    currentTime=0;destination={};state='running';
    createMediaElementSource(){return new Node();}createBiquadFilter(){return new Node();}createGain(){return new Node();}
    resume(){return Promise.resolve();}close(){return Promise.resolve();}
}
class AudioMock {addEventListener(){}play(){return Promise.resolve();}pause(){}removeAttribute(){}load(){}}
globalThis.AudioContext=Context;globalThis.Audio=AudioMock;
const source=await readFile(new URL('../wwwroot/Scripts/audio.js',import.meta.url),'utf8');
const audio=await import('data:text/javascript;base64,'+Buffer.from(source).toString('base64'));
audio.initialize({invokeMethodAsync:()=>Promise.resolve()},.7);
audio.tone({bass:8,mid:-2,treble:1});await audio.load('/fixture.wav',1);
assert.deepEqual(nodes.slice(1,4).map(n=>n.type),['lowshelf','peaking','highshelf']);
assert.deepEqual(nodes.slice(1,4).map(n=>n.frequency.value),[100,1000,8000]);
assert.deepEqual(nodes.slice(1,4).map(n=>n.gain.value),[8,-2,1]);
assert.ok(nodes[4].gain.value<.36,'headroom compensates combined boosts');
audio.tone({bass:0,mid:0,treble:0});assert.equal(nodes[4].gain.value,1);
audio.dispose();assert.ok(nodes.every(n=>n.disconnected));
console.log('PASS Browser equalizer uses three frequency bands, live gains, headroom and clean teardown');

import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
const nodes=[];
class Node {
    constructor(){this.gain={value:0,setTargetAtTime(value){this.value=value;}};this.pan={value:0,setTargetAtTime(value){this.value=value;}};this.frequency={value:0};this.Q={value:0};nodes.push(this);}
    connect(next){this.next=next;return next;}disconnect(){this.disconnected=true;}
}
class Context {
    currentTime=0;destination={};state='running';
    createMediaElementSource(){return new Node();}createBiquadFilter(){return new Node();}createGain(){return new Node();}createChannelSplitter(){return new Node();}createChannelMerger(){return new Node();}
    resume(){return Promise.resolve();}close(){return Promise.resolve();}
}
class AudioMock {addEventListener(){}play(){return Promise.resolve();}pause(){}removeAttribute(){}load(){}}
globalThis.AudioContext=Context;globalThis.Audio=AudioMock;
const source=await readFile(new URL('../wwwroot/Scripts/audio.js',import.meta.url),'utf8');
const audio=await import('data:text/javascript;base64,'+Buffer.from(source).toString('base64'));
audio.initialize({invokeMethodAsync:()=>Promise.resolve()},.7);
audio.tone({bass:8,mid:-2,treble:1});await audio.load('/fixture.wav',1);
assert.deepEqual(nodes.slice(1,8).map(n=>n.frequency.value),[32,100,300,1000,3000,8000,16000]);
assert.equal(nodes[2].type,'lowshelf');assert.equal(nodes[6].type,'highshelf');
assert.equal(nodes[2].gain.value,8);assert.equal(nodes[4].gain.value,-2);
assert.ok(nodes[8].gain.value<.36,'headroom compensates combined boosts');
audio.tone({bass:0,mid:0,treble:0,subBass:6,air:3,balance:.5});
assert.equal(nodes[1].gain.value,6);assert.equal(nodes[7].gain.value,3);assert.equal(nodes[10].gain.value,.5);assert.equal(nodes[11].gain.value,1);
audio.tone({bass:0,mid:0,treble:0});assert.equal(nodes[8].gain.value,1);assert.equal(nodes[10].gain.value,1);assert.equal(nodes[11].gain.value,1);
audio.dispose();assert.ok(nodes.every(n=>n.disconnected));
console.log('PASS Seven browser frequency bands, live gains, stereo balance, headroom and teardown');

import assert from 'node:assert/strict';
import fs from 'node:fs';
import ts from '../web/node_modules/typescript/lib/typescript.js';
const source=fs.readFileSync(new URL('../web/src/utils/fleetLayout.ts',import.meta.url),'utf8');
const {outputText}=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.ESNext,target:ts.ScriptTarget.ES2020}});
const {layoutRovers}=await import('data:text/javascript;base64,'+Buffer.from(outputText).toString('base64'));
const rovers=Array.from({length:6},(_,i)=>({id:String(i),identifier:`RO-0${i+1}`,position:{x:85,y:82}}));
const original=JSON.stringify(rovers);
function check(input){
  const result=layoutRovers(input), spots=[...result.values()];assert.equal(result.size,input.length);
  for(let i=0;i<spots.length;i++){
    const a=spots[i];assert.ok(a.x>=42&&a.x<=958&&a.y>=105&&a.y<=585);
    for(let j=i+1;j<spots.length;j++)assert.ok(Math.abs(a.x-spots[j].x)>=72||Math.abs(a.y-spots[j].y)>=84,'Icons and labels need separate space');
  }
  return result;
}
const first=check(rovers);assert.deepEqual([...first],[...check([...rovers].reverse())]);
assert.equal(JSON.stringify(rovers),original,'Visual offsets must not mutate rover positions');
assert.ok([...first.values()].every(p=>p.anchorX===850&&Math.abs(p.anchorY-524.8)<1e-9));
check(rovers.map((r,i)=>({...r,position:{x:i<2?85:15,y:i<2?28:82}})));
check(rovers.map((r,i)=>({...r,position:{x:50+i*.5,y:52}})));
for(const position of [{x:0,y:0},{x:100,y:100}])check(rovers.map(r=>({...r,position})));
const solo=check([rovers[0]]).get('0');assert.equal(solo.displaced,false);
console.log('PASS: six co-located rovers, mixed zones, nearby moving rovers, bounds, stable ordering and unchanged real positions');

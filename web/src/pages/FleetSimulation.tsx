import { useCallback, useEffect, useRef, useState } from 'react';
import { Activity, Bot, BatteryCharging, Play, RefreshCw, ShieldCheck, Wrench, AlertTriangle, Package, ArrowRight, CheckCircle2 } from 'lucide-react';
import { workflowsApi, Fleet, Mission, RunDetails, Zone } from '../api/workflowsApi';
import { breakdownApi } from '../api/breakdownApi';
import { useAuth } from '../context/AuthContext';
import './FleetSimulation.css';
import { formatIST } from '../utils/time';

const agents = [ ['MissionPlannerAgent','Mission Planner','Delegated checklist'], ['DispatchTelemetryAgent','Dispatch & Telemetry','Battery · weather · reservation'], ['MaintenanceMechanicAgent','Maintenance Mechanic','Catalog diagnosis · fleet exclusion'], ['SafetyGuardAgent','Safety Guard','Rules · risk · human approval'] ];
const colors = ['#56d5bd','#7eb4ff','#eab66d','#c7a2ff','#fb8191','#a5ce73'];
type Point = {x:number; y:number};
function route(m: Mission, zones: Zone[]): Point[] {
  const at = (id:string) => zones.find(z => z.id === id) || {x:50,y:52};
  const a = at(m.startZone), b = at(m.sourceZone), c = at(m.destinationZone);
  return [...(m.startZone === m.sourceZone ? [b] : [a,{x:a.x,y:52},{x:b.x,y:52},b]),{x:b.x,y:52},{x:c.x,y:52},c];
}
function pretty(value:string) { try{return JSON.stringify(JSON.parse(value),null,2);}catch{return value;} }

export function FleetSimulation() {
  const {user} = useAuth();
  const [fleet,setFleet]=useState<Fleet|null>(null), [selected,setSelected]=useState<string|null>(null), [details,setDetails]=useState<RunDetails|null>(null);
  const [roverId,setRoverId]=useState<string|null>(null), [error,setError]=useState(''), [busy,setBusy]=useState(false);
  const [source,setSource]=useState('WarehouseA-DockA1'), [destination,setDestination]=useState('WarehouseA-DockB3');
  const [cargo,setCargo]=useState('Standard'), [scenario,setScenario]=useState('low'), [notes,setNotes]=useState(''), [agent,setAgent]=useState('MissionPlannerAgent');
  const inFlight=useRef(false);
  const [refreshNotice,setRefreshNotice]=useState('');
  const refresh=useCallback(async(manual=false)=>{
    if(inFlight.current){if(manual)setRefreshNotice('Refresh already in progress...');return;} inFlight.current=true;
    try{const next=await workflowsApi.fleet();setFleet(next); if(manual)setRefreshNotice(`Fleet refreshed at ${formatIST(next.serverTime)}. Mission progress is preserved.`); if(selected) setDetails(await workflowsApi.details(selected));}
    catch(e:any){setError(e.response?.data?.message || 'Cannot reach fleet service. Check the API connection.');}
    finally{inFlight.current=false;}
  },[selected]);
  useEffect(()=>{void refresh();const timer=setInterval(()=>void refresh(),1000);return()=>clearInterval(timer);},[refresh]);
  useEffect(()=>{setDetails(null);},[selected]);
  const act=async(fn:()=>Promise<unknown>)=>{setBusy(true);setError('');try{await fn();await refresh();}catch(e:any){setError(e.response?.data?.message || e.message || 'Action failed.');}finally{setBusy(false);}};
  const mission=fleet?.runs.find(m=>m.id===selected);
  const rover=fleet?.rovers.find(r=>r.id===roverId);
  const openReport=fleet?.breakdowns.find(b=>b.roverId===roverId);
  const agentLogs=details?.logs.filter(l=>l.agentName===agent)||[];
  const active=fleet?.runs.filter(m=>m.status==='Executing')||[];
  const waiting=fleet?.runs.filter(m=>m.status==='AwaitingApproval')||[];
  const supervisor=user?.role==='Supervisor', technician=user?.role==='Technician';

  return <div className="fleet-lab">
    <header className="lab-heading"><div><div className="lab-eyebrow"><span className="live-dot"/> WAREHOUSE A / FLEET CONTROL</div><h1>Watch the fleet work.</h1><p>Follow every mission from a delegated plan to a completed delivery.</p></div><span className="mode-label">{fleet?.demo?'DEMO INPUTS · REAL WORKFLOW':'LIVE INPUTS · SIMULATED ROBOTS'}</span></header>
    {refreshNotice&&<p role="status">{refreshNotice}</p>}
    {error&&<div className="lab-error" role="alert"><AlertTriangle size={18}/>{error}<button onClick={()=>setError('')} aria-label="Dismiss error">×</button></div>}
    <div className="lab-metrics"><div><Bot/><strong>{fleet?.rovers.length??'—'}</strong><span>Fleet robots</span></div><div><Activity/><strong>{active.length}</strong><span>Moving cargo</span></div><div><ShieldCheck/><strong>{waiting.length}</strong><span>Waiting for approval</span></div><div><Wrench/><strong>{fleet?.breakdowns.length??0}</strong><span>Open breakdowns</span></div></div>
    <div className="lab-workspace"><section className="lab-map-panel"><div className="panel-title"><div><h2>Warehouse floor</h2><span>Click a robot to inspect its state</span></div><button className="lab-icon" aria-label="Refresh fleet" onClick={()=>void refresh(true)}><RefreshCw size={17}/></button></div>
      <svg className="warehouse-map" viewBox="0 0 1000 640" role="img" aria-label="Live warehouse map showing robot positions and mission routes">
        <defs><pattern id="floor-grid" width="25" height="25" patternUnits="userSpaceOnUse"><path d="M 25 0 L 0 0 0 25" fill="none" stroke="#ffffff" strokeOpacity=".035"/></pattern></defs>
        <rect width="1000" height="640" rx="18" fill="#111e2a"/><rect width="1000" height="640" fill="url(#floor-grid)"/>
        <path d="M150 160 V525 M500 170 V490 M850 160 V525 M100 333 H900" stroke="#293746" strokeWidth="52" fill="none" strokeLinejoin="round"/>
        <path d="M100 333 H900" stroke="#637385" strokeWidth="2" strokeDasharray="9 13"/>
        {[280,370,580,670].map(x=><g key={x}>{[105,190,410,490].map(y=><g key={y}><rect x={x} y={y} width="65" height="48" rx="4" fill="#1e3040" stroke="#3b4b59"/><path d={`M${x+8} ${y+24}h49`} stroke="#4a5c6d"/></g>)}</g>)}
        <text x="500" y="55" textAnchor="middle" fill="#617789" fontSize="13" letterSpacing="4">SMARTFLEET / AUTONOMOUS TRANSPORT SIMULATION</text>
        {fleet?.zones.map(z=><g key={z.id}><rect x={z.x*10-62} y={z.y*6.4-30} width="124" height="60" rx="10" fill="#142935" stroke="#53747d" strokeDasharray="4 4"/><text x={z.x*10} y={z.y*6.4+50} textAnchor="middle" fill="#adc3d2" fontSize="14">{z.label}</text></g>)}
        {fleet?.runs.filter(m=>['Executing','AwaitingApproval'].includes(m.status)).map(m=>{const index=fleet.rovers.findIndex(r=>r.id===m.roverId);return <polyline key={m.id} points={route(m,fleet.zones).map(p=>`${p.x*10},${p.y*6.4}`).join(' ')} fill="none" stroke={colors[Math.max(index,0)%colors.length]} strokeOpacity={selected===m.id?.toString()?'.9':'.35'} strokeWidth="3" strokeDasharray={m.status==='AwaitingApproval'?'6 8':undefined}/>;})}
        {fleet?.rovers.map((r,i)=>{const m=active.find(m=>m.roverId===r.id);const p=r.position;const offset=0;return <g key={r.id} transform={`translate(${p.x*10+offset} ${p.y*6.4})`} className="map-robot" role="button" tabIndex={0} aria-label={`${r.identifier}, ${r.status}, ${r.batteryPercentage}% battery`} onClick={()=>{setRoverId(r.id);if(m)setSelected(m.id);}} onKeyDown={e=>{if(e.key==='Enter'||e.key===' '){setRoverId(r.id);if(m)setSelected(m.id);}}}>
          <circle r="29" fill={colors[i%colors.length]} opacity={roverId===r.id?'.22':'.07'}/><rect x="-17" y="-15" width="34" height="30" rx="9" fill="#101c28" stroke={r.status==='Maintenance'||r.status==='Faulted'?'#fb8191':colors[i%colors.length]} strokeWidth="3"/><rect x="-10" y="-7" width="20" height="10" rx="3" fill={colors[i%colors.length]}/><circle cx="-7" cy="8" r="2" fill="#fff"/><circle cx="7" cy="8" r="2" fill="#fff"/><text y="-35" textAnchor="middle" fill="#e6f1f7" fontSize="15" fontWeight="600">{r.identifier}</text>{m&&<text y="42" textAnchor="middle" fill={colors[i%colors.length]} fontSize="13">{Math.round(m.progress*100)}%</text>}
        </g>;})}
      </svg><div className="map-caption"><span><i className="legend-dot"/> Real database state · updates every second</span><span>Illustrative routes; no physical navigation hardware</span></div>
      <div className="rover-inspector"><label>Inspect robot <select aria-label="Inspect robot" value={roverId||''} onChange={e=>setRoverId(e.target.value)}><option value="">Select a robot</option>{fleet?.rovers.map(r=><option key={r.id} value={r.id}>{r.identifier} / {r.status} / {r.batteryPercentage}%</option>)}</select></label></div>
      {rover&&<div className="rover-inspector"><div><strong>{rover.identifier}</strong><span>{rover.status} · {rover.batteryPercentage}% battery</span></div><div className="rover-actions">
        {fleet?.demo&&supervisor&&['Idle','Charging'].includes(rover.status)&&<button disabled={busy} onClick={()=>void act(()=>workflowsApi.charge(rover.id))}><BatteryCharging size={15}/> Demo charge</button>}
        {!openReport&&<button disabled={busy} onClick={()=>void act(async()=>{const form=new FormData();form.append('RoverId',rover.id);form.append('SymptomCategory','MotorOverheating');form.append('Description','Motor overheating detected during fleet demonstration');form.append('ErrorCode','E204');await breakdownApi.createReport(form);})}><AlertTriangle size={15}/> Report motor fault</button>}
        {openReport&&fleet?.demo&&(supervisor||technician)&&rover.locationZone!=='WarehouseA-MaintenanceArea'&&<button disabled={busy} onClick={()=>void act(()=>workflowsApi.recover(rover.id))}><Wrench size={15}/> Recover to maintenance (demo)</button>}
        {openReport&&(supervisor||technician)&&(!fleet?.demo||rover.locationZone==='WarehouseA-MaintenanceArea')&&<button disabled={busy} onClick={()=>void act(()=>breakdownApi.updateStatus(openReport.id,'Repaired'))}><Wrench size={15}/> Mark repaired</button>}
      </div></div>}
    </section><aside className="mission-composer"><div className="lab-eyebrow">DISPATCH CONSOLE</div><h2>Move your next load</h2><p>Every request runs through the agents before a robot can move.</p>
      <form onSubmit={e=>{e.preventDefault();void act(async()=>{const result=await workflowsApi.demo({sourceZone:source,destinationZone:destination,cargoType:cargo,weatherRisk:scenario});setSelected(result.id);});}}>
        <label>Pickup zone<select value={source} onChange={e=>setSource(e.target.value)}>{fleet?.zones.map(z=><option key={z.id} value={z.id}>{z.label}</option>)}</select></label>
        <label>Delivery zone<select value={destination} onChange={e=>setDestination(e.target.value)}>{fleet?.zones.map(z=><option key={z.id} value={z.id}>{z.label}</option>)}</select></label>
        <label>Cargo<select value={cargo} onChange={e=>setCargo(e.target.value)}><option>Standard</option><option>Fragile</option><option>Refrigerated</option></select></label>
        <fieldset><legend>Demonstration scenario</legend>{[['low','Clear route','Automatic approval'],['medium','Weather caution','Supervisor approval required'],['high','Severe weather','Safe rejection; no movement']].map(([v,title,desc])=><label key={v} className={`scenario-choice ${scenario===v?'chosen':''}`}><input type="radio" name="scenario" value={v} checked={scenario===v} onChange={()=>setScenario(v)}/><span><strong>{title}</strong><small>{desc}</small></span></label>)}</fieldset>
        <button className="launch-mission" disabled={busy||!fleet?.demo||source===destination||technician}><Play size={17}/>{busy?'Processing…':'Dispatch mission'}<ArrowRight size={17}/></button>
        {!fleet?.demo&&<p className="composer-note">Create a dispatch in Dispatch Requests when using live providers.</p>}
        {fleet?.demo&&<p className="composer-note">Weather is a labelled fixture. Planning, reservation, safety checks and approvals run on the backend.</p>}
      </form>
    </aside></div>
    <section className="lab-run-panel"><div className="panel-title"><h2>Mission activity</h2><span>{fleet?.runs.length??0} recent runs</span></div><div className="mission-strip">{fleet?.runs.length===0&&<p className="empty-lab">Dispatch a mission to see the agents work.</p>}{fleet?.runs.map(m=><button key={m.id} className={`mission-chip ${selected===m.id?'selected':''}`} onClick={()=>setSelected(m.id)}><Package size={17}/><span><strong>{m.cargoType} · {m.id.slice(0,8)}</strong><small>{m.status} {m.status==='Executing'?`· ${Math.round(m.progress*100)}%`:''}</small></span>{m.status==='Completed'&&<CheckCircle2 size={16}/>}</button>)}</div></section>
    {mission&&<section className="agent-workbench"><div className="panel-title"><div><h2>Inside the workflow</h2><span>Run {mission.id} · {mission.status}</span></div><span className="mode-label">{mission.weatherRisk.toUpperCase()} WEATHER RISK</span></div>
      {mission.failureReason&&<div className="lab-error">{mission.failureReason}</div>}
      <div className="agent-cards">{agents.map(([id,name,description],i)=>{const logs=details?.logs.filter(l=>l.agentName===id)||[];const latest=logs[logs.length-1];return <button key={id} onClick={()=>setAgent(id)} className={`agent-card ${agent===id?'selected':''}`}><span className="agent-number">0{i+1}</span><strong>{name}</strong><small>{description}</small><span className={`agent-result ${latest?.validationResult==='AutoRejected'?'blocked':''}`}>{latest?.validationResult||'Waiting'}</span></button>;})}</div>
      {details?.approval?.status==='Pending'&&supervisor&&<div className="approval-gate"><ShieldCheck/><div><strong>Supervisor decision required · risk {details.approval.riskScore}/100</strong><p>{details.approval.riskReason}</p><input aria-label="Review notes" placeholder="Review notes" value={notes} onChange={e=>setNotes(e.target.value)}/></div><div className="decision-buttons">{[['approve','Approve & dispatch'],['reject','Reject'],['request-revision','Request revision']].map(([action,label])=><button key={action} disabled={busy} onClick={()=>void act(()=>workflowsApi.decision(details.approval!.id,action,notes))}>{label}</button>)}</div></div>}
      {mission.status==='AwaitingApproval'&&!supervisor&&<p className="waiting-note">This robot is reserved. Sign in as a Supervisor to review; it will not move before approval.</p>}
      {['RevisionRequested','Failed','Rejected'].includes(mission.status)&&!technician&&<button className="retry-mission" disabled={busy} onClick={()=>void act(async()=>{const result=await workflowsApi.start(mission.dispatchRequestId,fleet?.demo?scenario:undefined);setSelected(result.id);})}>Replan this dispatch with current conditions</button>}
      <div className="agent-detail-grid"><div className="agent-json"><h3>Agent input → output</h3>{agentLogs.length===0?<p>No execution recorded for this agent.</p>:agentLogs.map(l=><details key={l.id} open><summary>{l.stepName} · {l.validationResult}</summary><div><section><h4>INPUT</h4><pre>{pretty(l.inputJson)}</pre></section><section><h4>OUTPUT</h4><pre>{pretty(l.outputJson)}</pre></section></div></details>)}</div><div className="execution-timeline"><h3>Execution timeline</h3>{details?.logs.map(l=><div className="timeline-event" key={l.id}><span className="timeline-dot"/><div><strong>{l.stepName}</strong><small>{l.agentName} · {l.validationResult}</small><time>{formatIST(l.timestamp)}</time></div></div>)}</div></div>
    </section>}
  </div>;
}

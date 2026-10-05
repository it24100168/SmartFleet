// Read-only assessment workload. Credentials are supplied through process environment variables.
import { performance } from 'node:perf_hooks';

const base = (process.env.SMARTFLEET_PERF_BASE_URL || 'http://localhost:5078').replace(/\/$/, '');
const email = process.env.SMARTFLEET_PERF_EMAIL;
const password = process.env.SMARTFLEET_PERF_PASSWORD;
const requests = Number(process.env.SMARTFLEET_PERF_REQUESTS || 30);
const concurrency = Number(process.env.SMARTFLEET_PERF_CONCURRENCY || 5);

if (!email || !password) throw new Error('Set SMARTFLEET_PERF_EMAIL and SMARTFLEET_PERF_PASSWORD in this process; do not put credentials in the script or report.');
if (!Number.isInteger(requests) || requests < 1 || requests > 500 || !Number.isInteger(concurrency) || concurrency < 1 || concurrency > 30)
  throw new Error('Use 1–500 requests and 1–30 concurrent workers.');

async function request(path, token, options = {}) {
  const started = performance.now();
  try {
    const response = await fetch(`${base}${path}`, {
      ...options,
      headers: { ...(options.headers || {}), ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      signal: AbortSignal.timeout(10000),
    });
    const body = options.parseJson ? await response.json() : null;
    return { status: response.status, ms: performance.now() - started, body };
  } catch (error) {
    return { status: 'network-error', ms: performance.now() - started, error: error?.name || 'Error' };
  }
}

const login = await request('/api/auth/login', null, {
  method: 'POST', headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password }), parseJson: true,
});
if (login.status !== 200 || !login.body?.token) throw new Error(`Performance account login failed (HTTP ${login.status}).`);
const token = login.body.token;

function percentile(sorted, fraction) {
  if (sorted.length === 0) return null;
  return Math.round(sorted[Math.min(sorted.length - 1, Math.ceil(sorted.length * fraction) - 1)] * 10) / 10;
}

async function sample(label, path, useToken = true) {
  let next = 0;
  const results = [];
  const started = performance.now();
  await Promise.all(Array.from({ length: Math.min(concurrency, requests) }, async () => {
    for (;;) {
      const index = next++;
      if (index >= requests) return;
      results[index] = await request(path, useToken ? token : null);
    }
  }));
  const wallMs = performance.now() - started;
  const latencies = results.map(x => x.ms).sort((a, b) => a - b);
  const success = results.filter(x => typeof x.status === 'number' && x.status >= 200 && x.status < 300).length;
  const statuses = {};
  for (const result of results) statuses[result.status] = (statuses[result.status] || 0) + 1;
  return {
    label, path, requests, concurrency, success, failure: requests - success,
    successRatePercent: Math.round(success * 1000 / requests) / 10,
    wallMs: Math.round(wallMs), throughputRequestsPerSecond: Math.round(requests * 1000 / wallMs * 10) / 10,
    latencyMs: { p50: percentile(latencies, .5), p95: percentile(latencies, .95), max: percentile(latencies, 1) },
    statuses,
  };
}

const workloads = [];
workloads.push(await sample('health including database connectivity', '/api/health', false));
workloads.push(await sample('PostgreSQL rover list', '/api/rovers?page=1&pageSize=10'));
workloads.push(await sample('PostgreSQL workflow/fleet snapshot', '/api/workflows/fleet'));

const fleet = await request('/api/workflows/fleet', token, { parseJson: true });
const runId = process.env.SMARTFLEET_PERF_RUN_ID || fleet.body?.runs?.[0]?.id;
const agentTimings = [];
if (runId) {
  const details = await request(`/api/workflows/${encodeURIComponent(runId)}`, token, { parseJson: true });
  if (details.status === 200 && Array.isArray(details.body?.logs)) {
    for (const item of details.body.logs.filter(x => x.stepName === 'AgentTiming')) {
      try {
        const timing = JSON.parse(item.outputJson);
        if (Number.isFinite(timing.durationMs))
          agentTimings.push({ agent: item.agentName, operation: JSON.parse(item.inputJson).operation, durationMs: timing.durationMs, attempt: timing.attempt });
      } catch { /* malformed historical log is excluded, not fabricated */ }
    }
  }
}

const report = {
  measuredAtUtc: new Date().toISOString(), baseUrl: base, databaseProvider: fleet.body?.databaseProvider || 'unverified',
  method: 'Read-only HTTP workload; 10-second per-request timeout; p50/p95 nearest-rank.',
  workloads, agentTimingRunId: runId || null, agentTimings,
  note: agentTimings.length ? 'Durations are persisted agent-operation timings from one existing workflow, not SQL timings.' : 'No persisted agent timing was available for the selected run.',
};
console.log(JSON.stringify(report, null, 2));
if (workloads.some(x => x.failure > 0)) process.exitCode = 1;

/** Read-only checks for the public assessment deployment. No credentials used. */
const api = process.env.SMARTFLEET_API_URL?.replace(/\/$/, '');
const web = process.env.SMARTFLEET_WEB_URL?.replace(/\/$/, '');
if (!api || !web || !api.startsWith('https://') || !web.startsWith('https://')) {
  console.error('Set SMARTFLEET_API_URL and SMARTFLEET_WEB_URL to public HTTPS origins.');
  process.exit(2);
}

const checks = [];
async function check(label, url, options, predicate) {
  try {
    const response = await fetch(url, { ...options, signal: AbortSignal.timeout(20000) });
    const body = await response.text();
    const passed = predicate(response, body);
    checks.push({ label, status: response.status, passed });
  } catch (error) {
    checks.push({ label, passed: false, error: error?.name || 'NetworkError' });
  }
}

await check('API health', `${api}/api/health`, {}, (response, body) => {
  if (!response.ok) return false;
  try {
    const data = JSON.parse(body);
    return data.status === 'ready' && data.databaseProvider === 'PostgreSQL' && data.demo === false;
  } catch { return false; }
});
await check('Swagger document', `${api}/swagger/v1/swagger.json`, {},
  (response, body) => response.ok && body.includes('"openapi"'));
await check('React entry', web, {},
  (response, body) => response.ok && body.includes('<div id="root">'));
await check('React direct route', `${web}/login`, {},
  (response, body) => response.ok && body.includes('<div id="root">'));
await check('API CORS preflight', `${api}/api/health`, {
  method: 'OPTIONS',
  headers: { Origin: web, 'Access-Control-Request-Method': 'GET' }
}, (response) => response.ok && response.headers.get('access-control-allow-origin') === web);

for (const result of checks) console.log(`${result.passed ? 'PASS' : 'FAIL'} ${result.label}: ${result.status ?? result.error}`);
if (checks.some(result => !result.passed)) process.exitCode = 1;

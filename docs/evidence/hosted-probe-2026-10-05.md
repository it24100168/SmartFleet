# First public Azure probe — 5 October 2026

The group supplied two newly created public URLs. A read-only run of `scripts/verify-hosted.mjs` returned:

| Check | Result | Interpretation |
| --- | --- | --- |
| `https://smartfleet-api-a0gmajhpg2ercjch.eastasia-01.azurewebsites.net/api/health` | HTTP 404 | The ASP.NET Core API has not been deployed to this Web App. Its `/` returns HTTP 200 with the title **Microsoft Azure App Service - Welcome**. |
| `/swagger/v1/swagger.json` on the same Web App | HTTP 404 | No public Swagger document yet. |
| `https://brave-tree-0ae688500.4.azurestaticapps.net/` | HTTP 200 | Serves a SmartFleet Vite page, but this alone is not a working application. |
| `/login` on the Static Web App | HTTP 404 | SPA direct-route fallback is absent from the currently deployed `main` build. |
| CORS preflight to the API | HTTP 404 | Cannot validate the intended React origin until the API is deployed/configured. |

The deployed Vite bundle `/assets/index-BVtVEHtg.js` did **not** contain the public API hostname; it uses relative `/api`. The deployed `/staticwebapp.config.json` was HTTP 404. Local `assessment/final-readiness` has the routing config in `web/public/staticwebapp.config.json`, but the Static Web App is connected to older `main`. Azure added its Static Web Apps GitHub workflow to `main` at `985b6f1` after the assessment branch was created. That workflow has now been merged into the assessment branch with public `VITE_API_BASE_URL` in the build step. A local production build confirmed the bundle contains the public API host and copies `staticwebapp.config.json` into `web/dist`. Review/merge the assessment branch and let Static Web Apps redeploy `main`.

The Web App still needs a deployment of `backend/backend.csproj`, private Neon/JWT/role settings, migration startup, CORS and Swagger enablement. Repeat the probe and a real hosted cross-client login/dispatch after those steps. These URLs are **created resources, not verified working deployment evidence**.

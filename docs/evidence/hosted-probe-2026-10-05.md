# First public Azure probe — 5 October 2026

The group supplied two newly created public URLs. A read-only run of `scripts/verify-hosted.mjs` returned:

| Check | Result | Interpretation |
| --- | --- | --- |
| `https://smartfleet-api-a0gmajhpg2ercjch.eastasia-01.azurewebsites.net/api/health` | HTTP 404 | The ASP.NET Core API has not been deployed to this Web App. Its `/` returns HTTP 200 with the title **Microsoft Azure App Service - Welcome**. |
| `/swagger/v1/swagger.json` on the same Web App | HTTP 404 | No public Swagger document yet. |
| `https://brave-tree-0ae688500.4.azurestaticapps.net/` | HTTP 200 | Serves a SmartFleet Vite page, but this alone is not a working application. |
| `/login` on the Static Web App | HTTP 404 | SPA direct-route fallback is absent from the currently deployed `main` build. |
| CORS preflight to the API | HTTP 404 | Cannot validate the intended React origin until the API is deployed/configured. |

The deployed Vite bundle `/assets/index-BVtVEHtg.js` did **not** contain the public API hostname; it uses relative `/api`. Local `assessment/final-readiness` has the routing config in `web/public/staticwebapp.config.json`, but the Static Web App production site is connected to older `main`. Azure added its Static Web Apps GitHub workflow to `main` at `985b6f1` after the assessment branch was created. That workflow has now been merged into the assessment branch with public `VITE_API_BASE_URL` in the build step. A local production build confirmed the bundle contains the public API host and copies `staticwebapp.config.json` into `web/dist`. [PR #1](https://github.com/it24100168/SmartFleet/pull/1) preview `https://brave-tree-0ae688500-1.4.azurestaticapps.net/login` returned HTTP 200 and its deployed bundle contains the public API host. Azure consumes the routing config internally, so the config file's public URL returning 404 is not evidence that the fallback is absent; the direct-route response is the relevant check. Review/merge the PR and let Static Web Apps redeploy `main`.

The Web App still needs a deployment of `backend/backend.csproj`, private Neon/JWT/role settings, migration startup, CORS and Swagger enablement. Repeat the probe and a real hosted cross-client login/dispatch after those steps. These URLs are **created resources, not verified working deployment evidence**.

After [PR #1](https://github.com/it24100168/SmartFleet/pull/1) was approved by `it24100382` and merged into `main` at `3369130`, the production Static Web App rebuilt. A second public probe returned **HTTP 200** for `/login`. Its deployed bundle `/assets/index-D1pdaCRf.js` contains the full Azure API URL. API health, Swagger and CORS preflight still returned **HTTP 404** because the Web App remained on Azure's welcome page. Thus React hosting/routing/build-time configuration is verified, while the application is not yet operational end to end. At that check, no API deployment credential was configured.

The team saved the Web App's private settings and selected **GitHub Actions with user-assigned identity** in Azure Deployment Center. Azure then committed `.github/workflows/main_smartfleet-api.yml` to `main` (`8e9af2b`). Its [first deployment run](https://github.com/it24100168/SmartFleet/actions/runs/37347105436) failed at **Build with dotnet** because it invoked `dotnet build` without a project path at the repository root. The assessment branch changes both build and publish commands to target `backend/backend.csproj`, retaining Azure's generated identity secret references. A passing hosted API has **not yet been observed**.

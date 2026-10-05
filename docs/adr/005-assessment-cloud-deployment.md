# ADR 005: Assessment cloud topology

**Status:** selected for deployment, subject to the Azure student subscription's allowed-region policy; 5 October 2026. This ADR records a decision, not a claim that resources are live.

## Context

SE3090 requires a public ASP.NET Core API with health and Swagger URLs, a public React site, PostgreSQL, and a runnable Flutter APK. The fleet worker advances missions once per second while the API runs. The group has Azure for Students credit and a fresh Neon Free project. The first Azure creation attempt was rejected by `RequestDisallowedByAzure` for its selected region. The deadline is reported as 6 October 2026, 11:00 AM Sri Lanka time, so the deployment must be simple to verify and keep available for evaluators through 21 October.

## Options considered

1. **Azure Static Web Apps Free + Azure App Service Basic B1 + Neon Free:** React/GitHub HTTPS hosting, supported .NET 8 API, App Service Always On for the worker, managed PostgreSQL with no Azure database-plan charge. The Basic plan consumes student credit and must be monitored. Neon can cold-start. App Service local upload persistence needs testing. Azure may block regions under the student offer.
2. **Azure Static Web Apps Free + App Service Free F1 + Neon Free:** lower direct cost, but App Service Free lacks Always On and has a daily CPU quota. The mission worker can stop after the API idles, making the demonstration unreliable.
3. **Render Free web service and static site + Neon Free:** Docker hosting and HTTPS without the Azure region policy. The free web service spins down after idle, loses local uploaded files on restart and may take about a minute to wake, so the worker does not advance continuously between visits. Suitable only as a disclosed fallback with a warm-up demonstration.
4. **Local workstation only:** already works, but does not meet the public deployment and evaluator-access requirements.

## Decision

Prefer option 1 in a region explicitly allowed by the Azure for Students subscription, using one Basic B1 instance after viewing its portal estimate. React and Flutter call the same public API. The API connects privately to the new Neon database through Npgsql with `SSL Mode=VerifyFull`, applies migrations on first controlled start, and stores its JWT secret and privileged bootstrap credentials in host settings. The backend uses Open-Meteo fixed Colombo coordinates for real educational weather data; invalid/unavailable observations fail closed. GitHub Actions build/test the submitted revision before merge and deployment.

If Azure offers no App Service-capable allowed region in time, use the repository's `Dockerfile.api` on Render or another .NET container host, record the free-tier limitations in the report, and keep Neon for durable relational state. Do not represent free-tier sleep as an always-running fleet.

## Consequences and checks

The selected topology is inexpensive within the student offer but not automatically free: the group must monitor credit and keep the service alive through the evaluation access period. It requires exact CORS and Vite build-time API URLs, production role provisioning, real migrations, HTTPS, a final Android rebuild, and a complete native → API/agents/PostgreSQL → React approval → native status check. Uploaded breakdown images need a restart/redeploy persistence check; if they do not survive, record the limitation or move them to durable object storage. The [deployment runbook](../AZURE-NEON-DEPLOY.md) contains the concrete configuration and verification steps.

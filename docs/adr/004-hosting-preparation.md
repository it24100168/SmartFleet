# ADR 004: Local hosting preparation before cloud selection

Status: cloud platform decision deferred by the team, 27 September 2026.

The team requested local preparation first. PostgreSQL runs in an isolated Docker Compose volume; API and clients run on the workstation. This supports reproducible verification without opening cloud accounts or publishing credentials. It does not satisfy hosted evaluator access.

Before selecting a provider, compare .NET runtime support, persistent upload storage, managed PostgreSQL, TLS, CORS, health checks, logs, cost and the required access period. Deploy React's production build and the API with private server configuration. Disable simulation for live inputs, configure weather credentials and provision privileged roles through trusted administration. Database migrations and uploads need explicit deployment/backup policies. No cloud provider, paid plan or availability claim is approved by this ADR.

# SmartFleet

SmartFleet is an internal autonomous warehouse rover management system designed for simulated factory operations, enabling Factory Operators to request cargo dispatches and log breakdowns via a Flutter mobile application, Maintenance Technicians to perform repair diagnostics, Fleet Supervisors to monitor and approve AI-flagged actions via a React web dashboard, and an integrated multi-agent AI pipeline to plan, validate, and coordinate simulated rover workflows.

---

## Tech Stack

- **Backend**: ASP.NET Core Web API (.NET 8), C#, Entity Framework Core, Npgsql PostgreSQL provider, JWT Bearer Authentication, BCrypt password hashing, Swagger/OpenAPI.
- **Web Frontend**: React 18, Vite, TypeScript, React Router v6, Axios, Lucide Icons, Vanilla Modern CSS design system.
- **Mobile Client**: Flutter (Dart, null-safe), `go_router`, `flutter_secure_storage`, `http`.
- **Database**: PostgreSQL (externally hosted, e.g., [Neon](https://neon.tech)).
- **CI/CD**: GitHub Actions (`.github/workflows/backend-ci.yml`) targeting .NET 8.

---

## Monorepo Layout

```text
smartfleet/
├── backend/                  # ASP.NET Core Web API (.NET 8)
│   ├── Controllers/          # API Controllers (Auth, Role Test endpoints)
│   ├── DTOs/                 # Data Transfer Objects (Auth, Common)
│   ├── Services/             # Application services (Auth, Token)
│   ├── Data/                 # EF Core DbContext, Repositories, Migrations
│   ├── Models/               # Entities (User) and Enums (Role)
│   ├── Middleware/           # Global Exception Handling Middleware
│   ├── Agents/               # Placeholder for 4 planned AI Agents
│   ├── backend.csproj        # Backend project configuration
│   └── backend.sln           # Visual Studio solution file
├── backend.Tests/            # xUnit automated test project
│   ├── UnitTest1.cs          # Initial passing test for CI
│   └── backend.Tests.csproj  # Test project file
├── web/                      # React Web Dashboard (Vite + TypeScript)
│   ├── src/
│   │   ├── api/              # Axios HTTP client with JWT interceptor
│   │   ├── context/          # React AuthContext and state management
│   │   ├── components/       # ProtectedRoute, Layout, Common UI states
│   │   ├── pages/            # Login, Dashboard, and placeholder screens
│   │   └── index.css         # Modern styling and design system
│   ├── package.json          # Web dependencies and scripts
│   └── vite.config.ts        # Vite configuration
├── mobile/                   # Flutter Mobile App
│   ├── lib/
│   │   ├── core/             # API client, secure storage, UI widgets
│   │   ├── models/           # User, Auth, and Role models
│   │   ├── services/         # Mobile authentication service
│   │   ├── routes/           # go_router configuration
│   │   └── screens/          # Login, Home, Dispatch, and Breakdown screens
│   └── pubspec.yaml          # Flutter package manifest
├── docs/                     # Architecture Decision Records (ADRs) & ERDs
├── .github/workflows/        # GitHub Actions CI workflow
├── .gitignore                # Unified monorepo ignore rules
└── README.md                 # Project documentation and setup guide
```

---

## Step-by-Step Local Setup

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js (v18+) & npm](https://nodejs.org/)
- [Flutter SDK (3.x+)](https://flutter.dev/docs/get-started/install)
- An active PostgreSQL database instance (e.g. a free serverless branch on [Neon](https://neon.tech) or any hosted PostgreSQL).

---

### 1. Backend Setup (`backend/`)

1. **Navigate to the backend folder**:
   ```bash
   cd backend
   ```

2. **Configure Connection String**:
   Copy the example environment settings:
   ```bash
   cp .env.example .env
   ```
   Update `appsettings.Development.json` (or set the environment variable `ConnectionStrings__DefaultConnection`) with your external PostgreSQL connection string (such as your Neon connection string):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=ep-xyz.us-east-2.aws.neon.tech;Database=neondb;Username=your_user;Password=your_password;SSL Mode=Require;Trust Server Certificate=true"
     },
     "JwtSettings": {
       "Secret": "SmartFleetSuperSecretKeyForJwtAuthentication2026!",
       "Issuer": "SmartFleetAPI",
       "Audience": "SmartFleetClients",
       "ExpirationMinutes": 1440
     }
   }
   ```

3. **Restore and Build**:
   ```bash
   dotnet restore backend.sln
   dotnet build backend.sln
   ```

4. **Apply EF Core Migrations**:
   Run the initial migration against your database:
   ```bash
   dotnet ef database update --project backend.csproj
   ```

5. **Run the API**:
   ```bash
   dotnet run --project backend.csproj
   ```
   The backend API will start at `http://localhost:5000` (or `https://localhost:5001`).
   Open Swagger UI at `http://localhost:5000/swagger` to inspect endpoints and test authentication.

6. **Run Backend Tests**:
   ```bash
   dotnet test ../backend.Tests/backend.Tests.csproj
   ```

---

### 2. Web Setup (`web/`)

1. **Navigate to the web folder**:
   ```bash
   cd web
   ```

2. **Configure Environment Variables**:
   Copy `.env.example` to `.env`:
   ```bash
   cp .env.example .env
   ```
   Ensure `VITE_API_BASE_URL` points to your running backend:
   ```env
   VITE_API_BASE_URL=http://localhost:5000/api
   ```

3. **Install Dependencies**:
   ```bash
   npm install
   ```

4. **Start Vite Development Server**:
   ```bash
   npm run dev
   ```
   Open `http://localhost:5173` in your browser. The app will automatically redirect unauthenticated users to `/login`.

---

### 3. Mobile Setup (`mobile/`)

1. **Navigate to the mobile folder**:
   ```bash
   cd mobile
   ```

2. **Configure Backend URL**:
   Inspect `lib/core/constants/api_constants.dart` and update `baseUrl`:
   - Android Emulator: `http://10.0.2.2:5000/api`
   - iOS Simulator: `http://localhost:5000/api`
   - Physical Device: `http://<YOUR_LOCAL_IP>:5000/api`
   - Chrome / Web: `http://localhost:5000/api`

3. **Fetch Dependencies**:
   ```bash
   flutter pub get
   ```

4. **Run Application**:
   ```bash
   # Run on connected device or emulator
   flutter run

   # Or run directly in Chrome for testing
   flutter run -d chrome
   ```

---

## Multi-Agent AI Pipeline (Planned)

The `backend/Agents/` directory will host the 4 autonomous agents:
1. **Mission Planner Agent**: Analyzes pending dispatch requests, schedules rover batches, and optimizes paths.
2. **Dispatch & Telemetry Agent**: Monitors real-time rover telemetry, battery status, and execution states.
3. **Maintenance Mechanic Agent**: Analyzes breakdown reports and sensor anomalies to prescribe diagnostic workflows.
4. **Safety Guard Agent**: Enforces warehouse safety protocols and escalates flagged exceptions to the Supervisor dashboard for manual override.

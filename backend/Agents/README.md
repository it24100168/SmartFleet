# SmartFleet Agentic AI Pipeline

This directory is reserved for the 4-agent autonomous pipeline that coordinates warehouse rover workflows, validates safety constraints, and manages dispatch life cycles.

## Planned Agents

### 1. Mission Planner Agent
- **Responsibility**: Analyzes incoming cargo dispatch requests, assesses priority levels, queries current rover availability and warehouse map topologies, and generates optimal multi-stop mission plans.

### 2. Dispatch & Telemetry Agent (Owner: Hamdhan) [IMPLEMENTED]
- **Location**: `backend/Agents/DispatchTelemetryAgent/`
- **Contract Reference**: `docs/agent-contracts.md` (Section 2)
- **Responsibility**: Queries candidate rovers by zone and battery threshold against the `Rovers` database, checks environmental route weather risk via OpenWeather API, validates safe operating parameters, and transactionally locks the selected rover for mission execution. Handoffs directly to Safety Guard Agent (Dilukshi).

### 3. Maintenance Mechanic Agent
- **Responsibility**: Ingests breakdown alerts and telemetry error codes, runs automated diagnostics on rover subsystems (drivetrain, sensors, battery cell health), and recommends corrective work orders to Maintenance Technicians.

### 4. Safety Guard Agent
- **Responsibility**: Continuously evaluates rover operating bounds (speed limits, proximity thresholds, battery safety margins). Any unsafe trajectory or anomalies are immediately flagged, pausing the operation and routing an approval request to the Fleet Supervisor web dashboard.

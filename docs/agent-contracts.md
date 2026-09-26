cd back# SmartFleet — Agent Contracts

This file defines the exact input and output shape for each of the four
agents in the pipeline. Field names, types, and structure here are locked
by team agreement. Sample values below are illustrative only — actual
values will differ at runtime.

## 1. Mission Planner Agent (owner: Chathumini)

INPUT:
{
  "dispatchRequestId": "d3f1-892a",
  "sourceZone": "WarehouseA-DockA1",
  "destinationZone": "WarehouseA-DockB3",
  "cargoType": "Fragile",
  "priority": "High",
  "preferredTimeWindow": "2026-09-19T15:00:00Z"
}

OUTPUT:
{
  "dispatchRequestId": "d3f1-892a",
  "plan": [
    { "stepNumber": 1, "stepName": "Locate available rover", "status": "Pending" },
    { "stepNumber": 2, "stepName": "Verify battery sufficient", "status": "Pending" }
  ],
  "createdAt": "2026-09-19T14:02:11Z"
}

## 2. Dispatch & Telemetry Agent (owner: Hamdhan)

INPUT:
{
  "dispatchRequestId": "d3f1-892a",
  "planSteps": [
    { "stepNumber": 1, "stepName": "Locate available rover", "status": "Pending" }
  ],
  "sourceZone": "WarehouseA-DockA1",
  "destinationZone": "WarehouseA-DockB3"
}

OUTPUT:
{
  "dispatchRequestId": "d3f1-892a",
  "selectedRoverId": "RO-04",
  "batteryOk": true,
  "weatherRisk": "low",
  "locked": true,
  "reason": null
}

## 3. Maintenance Mechanic Agent (owner: Pubudini)

INPUT:
{
  "breakdownReportId": "b7c2-441e",
  "symptomCategory": "MotorOverheating",
  "description": "Front wheel motor making grinding noise",
  "errorCode": "E204"
}

OUTPUT:
{
  "breakdownReportId": "b7c2-441e",
  "likelyPart": "Drive Motor Unit",
  "estimatedRepairHours": 2,
  "severity": "High",
  "confidenceNote": "Matched via FailureCatalog keyword: motor overheating",
  "recommendedAction": "ScheduleRepair"
}

## 4. Safety Guard Agent (owner: Dilukshi)

INPUT:
{
  "dispatchRequestId": "d3f1-892a",
  "roverId": "RO-04",
  "missionPlanSummary": {
    "plan": [
      { "stepNumber": 1, "stepName": "Locate available rover", "status": "Completed" }
    ]
  },
  "telemetryResult": {
    "batteryOk": true,
    "weatherRisk": "low",
    "locked": true
  },
  "maintenanceResult": null
}

OUTPUT:
{
  "dispatchRequestId": "d3f1-892a",
  "riskScore": 12,
  "riskReason": "Low weather risk, sufficient battery, no open maintenance flags",
  "requiresApproval": false,
  "autoOutcome": "AutoApproved"
}

## Rules for Using This Contract

- Field names and types above are locked. Do not rename or restructure a
  field without notifying the whole team and updating this file first.
- Enum values (e.g. "low"/"medium"/"high" for weatherRisk, "Pending"/
  "Completed" for step status) must use this exact casing everywhere.
- Sample values shown are illustrative only; real runtime values will differ.
- Until the real upstream agent exists in your branch, build and test your
  agent against a MOCKED object matching the INPUT shape defined here for
  your agent.
- When two agents are wired together during integration week, the real
  output of the upstream agent must match the INPUT shape defined here for
  the downstream agent exactly, or the handoff will fail validation.

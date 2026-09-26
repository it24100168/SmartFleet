import client from './axiosClient';
export interface Zone { id: string; label: string; x: number; y: number }
export interface FleetRover { id: string; identifier: string; status: string; batteryPercentage: number; locationZone: string; currentMissionId: string | null }
export interface Mission { id: string; dispatchRequestId: string; roverId: string | null; startZone: string; status: string; progress: number; isDemo: boolean; weatherRisk: string; failureReason: string | null; sourceZone: string; destinationZone: string; cargoType: string; createdAt: string }
export interface Fleet { demo: boolean; serverTime: string; zones: Zone[]; rovers: FleetRover[]; runs: Mission[]; breakdowns: {id: string; roverId: string; symptomCategory: string; status: string}[] }
export interface RunDetails { run: {id: string; status: string; planJson: string}; logs: {id: string; agentName: string; stepName: string; validationResult: string; inputJson: string; outputJson: string; timestamp: string}[]; approval: {id: string; status: string; riskScore: number; riskReason: string} | null }
export const workflowsApi = {
  fleet: async () => (await client.get<Fleet>('/workflows/fleet')).data,
  details: async (id: string) => (await client.get<RunDetails>(`/workflows/${id}`)).data,
  demo: async (input: {sourceZone: string; destinationZone: string; cargoType: string; weatherRisk: string}) => (await client.post<{id: string}>('/workflows/demo-mission', input)).data,
  start: async (id: string, weatherRisk?: string) => (await client.post<{id: string}>(`/workflows/dispatch/${id}/start`, {weatherRisk})).data,
  charge: async (id: string) => client.post(`/workflows/demo-rovers/${id}/charge`),
  decision: async (id: string, action: string, reviewNotes: string) => client.post(`/approval-requests/${id}/${action}`, {reviewNotes}),
};

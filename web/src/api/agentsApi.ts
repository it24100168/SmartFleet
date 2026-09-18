import axiosClient from './axiosClient';

export interface PlanStepDto {
  stepNumber: number;
  stepName: string;
  status: string;
}

export interface DispatchTelemetryAgentInput {
  dispatchRequestId: string;
  planSteps: PlanStepDto[];
  sourceZone: string;
  destinationZone: string;
}

export interface DispatchTelemetryAgentOutput {
  dispatchRequestId: string;
  selectedRoverId: string | null;
  batteryOk: boolean;
  weatherRisk: 'low' | 'medium' | 'high';
  locked: boolean;
  reason: string | null;
}

export interface WeatherAssessmentResult {
  weatherRisk: string;
  conditionDescription: string;
  temperatureCelsius: number;
  rainVolumeMm: number;
  isSimulatedFallback: boolean;
}

export const agentsApi = {
  executeDispatchTelemetryAgent: async (
    input: DispatchTelemetryAgentInput
  ): Promise<DispatchTelemetryAgentOutput> => {
    const response = await axiosClient.post<DispatchTelemetryAgentOutput>(
      '/agents/dispatch-telemetry/execute',
      input
    );
    return response.data;
  },

  getMockInput: async (): Promise<DispatchTelemetryAgentInput> => {
    const response = await axiosClient.get<DispatchTelemetryAgentInput>(
      '/agents/dispatch-telemetry/mock-input'
    );
    return response.data;
  },

  getCurrentWeather: async (
    sourceZone = 'WarehouseA-DockA1',
    destinationZone = 'WarehouseA-DockB3'
  ): Promise<WeatherAssessmentResult> => {
    const response = await axiosClient.get<WeatherAssessmentResult>(
      '/agents/dispatch-telemetry/weather',
      { params: { sourceZone, destinationZone } }
    );
    return response.data;
  },
};

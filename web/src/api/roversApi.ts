import axiosClient from './axiosClient';

export interface Rover {
  id: string;
  identifier: string;
  status: 'Idle' | 'Dispatched' | 'Charging' | 'Maintenance' | 'Faulted';
  batteryPercentage: number;
  locationZone: string;
  currentMissionId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface RoverQueryParameters {
  status?: string;
  zone?: string;
  sortBy?: string;
  isDescending?: boolean;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface UpdateRoverSimulationPayload {
  status?: string;
  batteryPercentage?: number;
  locationZone?: string;
}

export const roversApi = {
  getRovers: async (params?: RoverQueryParameters): Promise<PagedResult<Rover>> => {
    const response = await axiosClient.get<PagedResult<Rover>>('/rovers', { params });
    return response.data;
  },

  getRoverById: async (id: string): Promise<Rover> => {
    const response = await axiosClient.get<Rover>(`/rovers/${id}`);
    return response.data;
  },

  updateRoverSimulation: async (id: string, payload: UpdateRoverSimulationPayload): Promise<Rover> => {
    const response = await axiosClient.patch<Rover>(`/rovers/${id}`, payload);
    return response.data;
  },

  lockRoverForMission: async (id: string, missionId: string) => {
    const response = await axiosClient.post(`/rovers/${id}/lock`, { missionId });
    return response.data;
  },
};

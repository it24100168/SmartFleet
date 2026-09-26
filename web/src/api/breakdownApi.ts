import axiosClient from './axiosClient';
import {
  BreakdownReport,
  BreakdownStatus,
  MaintenanceMechanicOutput,
  PagedBreakdownReports,
} from '../types/breakdown';

export const breakdownApi = {
  getReports: async (params?: {
    status?: BreakdownStatus;
    symptomCategory?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PagedBreakdownReports> => {
    const response = await axiosClient.get<PagedBreakdownReports>('/breakdown-reports', {
      params,
    });
    return response.data;
  },

  getReportById: async (id: string): Promise<BreakdownReport> => {
    const response = await axiosClient.get<BreakdownReport>(`/breakdown-reports/${id}`);
    return response.data;
  },

  updateStatus: async (id: string, status: BreakdownStatus): Promise<BreakdownReport> => {
    const response = await axiosClient.patch<BreakdownReport>(`/breakdown-reports/${id}/status`, {
      status,
    });
    return response.data;
  },

  createReport: async (formData: FormData): Promise<BreakdownReport> => {
    const response = await axiosClient.post<BreakdownReport>('/breakdown-reports', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  diagnoseReport: async (id: string): Promise<MaintenanceMechanicOutput> => {
    const response = await axiosClient.post<MaintenanceMechanicOutput>(
      `/breakdown-reports/${id}/diagnose`
    );
    return response.data;
  },
};

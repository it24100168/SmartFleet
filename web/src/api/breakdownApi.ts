import axiosClient from './axiosClient';
import {
  BreakdownReport,
  BreakdownStatus,
  MaintenanceMechanicOutput,
  PagedBreakdownReports,
} from '../types/breakdown';

const resolvePhoto = (report: BreakdownReport): BreakdownReport => ({ ...report,
  photoUrl: report.photoUrl ? new URL(report.photoUrl, new URL(axiosClient.defaults.baseURL || '/api', window.location.origin)).href : null });
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
    return { ...response.data, items: response.data.items.map(resolvePhoto) };
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

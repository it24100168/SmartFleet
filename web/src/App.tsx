import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { AppLayout } from './components/Layout/AppLayout';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { DispatchRequests } from './pages/DispatchRequests';
import { FleetTelemetry } from './pages/FleetTelemetry';
import { MaintenanceQueue } from './pages/MaintenanceQueue';
import { ApprovalCenter } from './pages/ApprovalCenter';

export const App: React.FC = () => {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Public Authentication Route */}
          <Route path="/login" element={<Login />} />

          {/* Protected Dashboard & Module Routes */}
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <AppLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<Dashboard />} />
            <Route path="dispatch" element={<DispatchRequests />} />
            <Route path="telemetry" element={<FleetTelemetry />} />
            <Route path="maintenance" element={<MaintenanceQueue />} />
            <Route
              path="approvals"
              element={
                <ProtectedRoute allowedRoles={['Supervisor']}>
                  <ApprovalCenter />
                </ProtectedRoute>
              }
            />
          </Route>

          {/* Fallback route */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
};

export default App;

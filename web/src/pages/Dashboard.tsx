import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { authApi } from '../api/authApi';
import { Bot, Cpu, Activity, AlertTriangle, ShieldCheck, CheckCircle2, XCircle } from 'lucide-react';
import { Link } from 'react-router-dom';

export const Dashboard: React.FC = () => {
  const { user } = useAuth();
  const [testResult, setTestResult] = useState<{ success: boolean; message: string } | null>(null);
  const [testingEndpoint, setTestingEndpoint] = useState<string | null>(null);

  const runRoleCheck = async (endpointName: string, callFn: () => Promise<any>) => {
    setTestingEndpoint(endpointName);
    setTestResult(null);
    try {
      const data = await callFn();
      setTestResult({
        success: true,
        message: data.message || `200 OK - Access allowed for role ${user?.role}`,
      });
    } catch (err: any) {
      const status = err.response?.status;
      const msg = err.response?.data?.message || err.message;
      setTestResult({
        success: false,
        message: `HTTP ${status}: ${status === 403 ? 'Forbidden - role not authorized' : msg}`,
      });
    } finally {
      setTestingEndpoint(null);
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <div>
          <h1 style={{ fontSize: '2rem', marginBottom: '0.25rem' }}>Fleet Mission Control</h1>
          <p style={{ color: 'var(--text-muted)' }}>
            Welcome back, <strong>{user?.name}</strong>. Simulated warehouse rovers and agents are active.
          </p>
        </div>
      </div>

      {/* Metrics Grid */}
      <div className="stats-grid">
        <div className="glass-card stat-item">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="stat-label">Simulated Rovers</span>
            <Bot size={20} color="var(--primary-light)" />
          </div>
          <div className="stat-value">10</div>
          <div style={{ fontSize: '0.8rem', color: 'var(--success)' }}>100% Operational In-Silico</div>
        </div>

        <div className="glass-card stat-item">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="stat-label">Active Missions</span>
            <Activity size={20} color="#38bdf8" />
          </div>
          <div className="stat-value">0</div>
          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Awaiting cargo dispatch</div>
        </div>

        <div className="glass-card stat-item">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="stat-label">Breakdown Alerts</span>
            <AlertTriangle size={20} color="#f59e0b" />
          </div>
          <div className="stat-value">0</div>
          <div style={{ fontSize: '0.8rem', color: 'var(--success)' }}>All subsystems green</div>
        </div>

        <div className="glass-card stat-item">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="stat-label">AI Pipeline</span>
            <Cpu size={20} color="#8b5cf6" />
          </div>
          <div className="stat-value">4 Agents</div>
          <div style={{ fontSize: '0.8rem', color: 'var(--primary-light)' }}>Mission, Dispatch, Mechanic, Safety</div>
        </div>
      </div>

      {/* Role Authorization Diagnostics Card */}
      <div className="glass-card" style={{ marginTop: '2rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1rem' }}>
          <ShieldCheck size={24} color="var(--primary-light)" />
          <h3 style={{ fontSize: '1.2rem' }}>Role-Based Authorization Diagnostics</h3>
        </div>
        <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginBottom: '1.25rem' }}>
          Test the backend ASP.NET Core role-protected endpoints in real-time with your current JWT token.
          Your current session role is <strong>{user?.role}</strong>.
        </p>

        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.75rem' }}>
          <button
            className="btn btn-secondary"
            onClick={() => runRoleCheck('Operator', authApi.testOperatorEndpoint)}
            disabled={testingEndpoint !== null}
          >
            Test GET /api/test/operator-only
          </button>
          <button
            className="btn btn-secondary"
            onClick={() => runRoleCheck('Technician', authApi.testTechnicianEndpoint)}
            disabled={testingEndpoint !== null}
          >
            Test GET /api/test/technician-only
          </button>
          <button
            className="btn btn-secondary"
            onClick={() => runRoleCheck('Supervisor', authApi.testSupervisorEndpoint)}
            disabled={testingEndpoint !== null}
          >
            Test GET /api/test/supervisor-only
          </button>
        </div>

        {testResult && (
          <div
            style={{
              marginTop: '1.25rem',
              padding: '0.85rem 1rem',
              borderRadius: 'var(--radius-sm)',
              background: testResult.success ? 'var(--success-bg)' : 'var(--danger-bg)',
              border: `1px solid ${testResult.success ? 'rgba(16, 185, 129, 0.4)' : 'rgba(239, 68, 68, 0.4)'}`,
              display: 'flex',
              alignItems: 'center',
              gap: '0.75rem',
            }}
          >
            {testResult.success ? (
              <CheckCircle2 size={18} color="var(--success)" />
            ) : (
              <XCircle size={18} color="var(--danger)" />
            )}
            <span style={{ fontSize: '0.9rem', color: testResult.success ? '#6ee7b7' : '#fca5a5' }}>
              {testResult.message}
            </span>
          </div>
        )}
      </div>

      {/* Quick Navigation Cards */}
      <div style={{ marginTop: '2rem' }}>
        <h3 style={{ fontSize: '1.2rem', marginBottom: '1rem' }}>Modules</h3>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '1rem' }}>
          <Link to="/dispatch" className="glass-card" style={{ display: 'block' }}>
            <h4 style={{ fontSize: '1rem', color: 'var(--primary-light)', marginBottom: '0.25rem' }}>Dispatch Requests</h4>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>Review incoming transport tasks from Factory Operators.</p>
          </Link>
          <Link to="/telemetry" className="glass-card" style={{ display: 'block' }}>
            <h4 style={{ fontSize: '1rem', color: 'var(--primary-light)', marginBottom: '0.25rem' }}>Fleet Telemetry</h4>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>Simulated rover coordinates, velocity, and battery stats.</p>
          </Link>
          <Link to="/maintenance" className="glass-card" style={{ display: 'block' }}>
            <h4 style={{ fontSize: '1rem', color: 'var(--primary-light)', marginBottom: '0.25rem' }}>Maintenance Queue</h4>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>Technician repair work orders and breakdown diagnostics.</p>
          </Link>
          <Link to="/approvals" className="glass-card" style={{ display: 'block' }}>
            <h4 style={{ fontSize: '1rem', color: 'var(--primary-light)', marginBottom: '0.25rem' }}>Approval Center</h4>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>Supervisor authorization for AI-flagged rover anomalies.</p>
          </Link>
        </div>
      </div>
    </div>
  );
};

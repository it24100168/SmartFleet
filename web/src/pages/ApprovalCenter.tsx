import React, { useState, useEffect, useCallback } from 'react';
import {
  ShieldCheck,
  ShieldAlert,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  RotateCcw,
  Clock,
  User,
  Search,
  Battery,
  CloudRain,
  Lock,
  Unlock,
  Wrench,
  ChevronRight,
  X,
  Sparkles,
  RefreshCw,
  Activity,
  Layers,
  Code2
} from 'lucide-react';
import { approvalApi } from '../api/approvalApi';
import {
  ApprovalRequest,
  ApprovalStats,
  WorkflowExecutionLog,
  ParsedAgentSummary,
  ApprovalStatus
} from '../types/approval';

export const ApprovalCenter: React.FC = () => {
  // Data state
  const [requests, setRequests] = useState<ApprovalRequest[]>([]);
  const [stats, setStats] = useState<ApprovalStats | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(8);
  const [totalPages, setTotalPages] = useState(1);

  // Filter & Search state
  const [selectedStatus, setSelectedStatus] = useState<string>('all');
  const [searchTerm, setSearchTerm] = useState('');
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortOrder, setSortOrder] = useState<'desc' | 'asc'>('desc');

  // Loading & Action state
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [simulating, setSimulating] = useState(false);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  // Detail Modal / Drawer state
  const [selectedRequest, setSelectedRequest] = useState<ApprovalRequest | null>(null);
  const [executionLogs, setExecutionLogs] = useState<WorkflowExecutionLog[]>([]);
  const [logsLoading, setLogsLoading] = useState(false);
  const [activeDetailTab, setActiveDetailTab] = useState<'assessment' | 'audit'>('assessment');
  const [reviewNotes, setReviewNotes] = useState('');
  const [expandedLogId, setExpandedLogId] = useState<string | null>(null);

  // Auto-dismiss notification after 4s
  useEffect(() => {
    if (notification) {
      const timer = setTimeout(() => setNotification(null), 4000);
      return () => clearTimeout(timer);
    }
  }, [notification]);

  // Fetch Requests & Stats
  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const [pagedData, statsData] = await Promise.all([
        approvalApi.getApprovals({
          status: selectedStatus,
          sortBy,
          sortOrder,
          search: searchTerm || undefined,
          page,
          pageSize,
        }),
        approvalApi.getStats(),
      ]);

      setRequests(pagedData.items);
      setTotalCount(pagedData.totalCount);
      setTotalPages(pagedData.totalPages || 1);
      setStats(statsData);
    } catch (err: any) {
      console.error('Failed to fetch approval requests:', err);
      setNotification({
        type: 'error',
        message: err.response?.data?.message || 'Failed to load approval requests.',
      });
    } finally {
      setLoading(false);
    }
  }, [selectedStatus, sortBy, sortOrder, searchTerm, page, pageSize]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Fetch execution logs when drawer opens
  const openDetailDrawer = async (request: ApprovalRequest) => {
    setSelectedRequest(request);
    setReviewNotes(request.reviewNotes || '');
    setActiveDetailTab('assessment');
    setLogsLoading(true);
    try {
      const logs = await approvalApi.getExecutionLogs(request.id);
      setExecutionLogs(logs);
    } catch (err) {
      console.error('Failed to load execution logs:', err);
      setExecutionLogs([]);
    } finally {
      setLogsLoading(false);
    }
  };

  const closeDetailDrawer = () => {
    setSelectedRequest(null);
    setExecutionLogs([]);
    setReviewNotes('');
    setExpandedLogId(null);
  };

  // Supervisor Decision Actions
  const handleDecision = async (decision: 'approve' | 'reject' | 'revision') => {
    if (!selectedRequest) return;
    setActionLoading(true);
    try {
      let updated: ApprovalRequest;
      if (decision === 'approve') {
        updated = await approvalApi.approve(selectedRequest.id, reviewNotes);
        setNotification({ type: 'success', message: `Dispatch Request ${updated.dispatchRequestId} approved successfully.` });
      } else if (decision === 'reject') {
        updated = await approvalApi.reject(selectedRequest.id, reviewNotes);
        setNotification({ type: 'success', message: `Dispatch Request ${updated.dispatchRequestId} rejected.` });
      } else {
        updated = await approvalApi.requestRevision(selectedRequest.id, reviewNotes);
        setNotification({ type: 'success', message: `Revision requested for ${updated.dispatchRequestId}.` });
      }

      setSelectedRequest(updated);
      await fetchData();

      // Refresh logs to show the new supervisor action entry
      const logs = await approvalApi.getExecutionLogs(updated.id);
      setExecutionLogs(logs);
    } catch (err: any) {
      console.error('Decision action failed:', err);
      setNotification({
        type: 'error',
        message: err.response?.data?.message || `Failed to ${decision} request.`,
      });
    } finally {
      setActionLoading(false);
    }
  };

  // AI Pipeline Simulator Trigger
  const handleSimulate = async (scenario: string) => {
    setSimulating(true);
    try {
      const output = await approvalApi.simulateEvaluation(scenario);
      setNotification({
        type: 'success',
        message: `Safety Guard Agent evaluated scenario "${scenario}" -> Outcome: ${output.autoOutcome} (Risk Score: ${output.riskScore}).`,
      });
      await fetchData();
    } catch (err: any) {
      console.error('Simulation failed:', err);
      setNotification({
        type: 'error',
        message: err.response?.data?.message || 'Simulation execution failed.',
      });
    } finally {
      setSimulating(false);
    }
  };

  // Parse Agent Summary JSON safely
  const parseSummary = (jsonStr: string): ParsedAgentSummary => {
    try {
      return JSON.parse(jsonStr) as ParsedAgentSummary;
    } catch {
      return {};
    }
  };

  const getRiskScoreColor = (score: number) => {
    if (score < 30) return '#10b981'; // Green
    if (score < 75) return '#f59e0b'; // Amber
    return '#ef4444'; // Red
  };

  const getStatusBadge = (status: ApprovalStatus) => {
    switch (status) {
      case 'Pending':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.25rem 0.65rem',
              borderRadius: '9999px',
              fontSize: '0.75rem',
              fontWeight: 600,
              background: 'rgba(245, 158, 11, 0.15)',
              color: '#fbbf24',
              border: '1px solid rgba(245, 158, 11, 0.35)',
              boxShadow: '0 0 10px rgba(245, 158, 11, 0.2)',
            }}
          >
            <Clock size={12} /> Pending Review
          </span>
        );
      case 'Approved':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.25rem 0.65rem',
              borderRadius: '9999px',
              fontSize: '0.75rem',
              fontWeight: 600,
              background: 'rgba(16, 185, 129, 0.15)',
              color: '#34d399',
              border: '1px solid rgba(16, 185, 129, 0.35)',
            }}
          >
            <CheckCircle2 size={12} /> Approved
          </span>
        );
      case 'Rejected':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.25rem 0.65rem',
              borderRadius: '9999px',
              fontSize: '0.75rem',
              fontWeight: 600,
              background: 'rgba(239, 68, 68, 0.15)',
              color: '#f87171',
              border: '1px solid rgba(239, 68, 68, 0.35)',
            }}
          >
            <XCircle size={12} /> Rejected
          </span>
        );
      case 'RevisionRequested':
        return (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              padding: '0.25rem 0.65rem',
              borderRadius: '9999px',
              fontSize: '0.75rem',
              fontWeight: 600,
              background: 'rgba(139, 92, 246, 0.15)',
              color: '#a78bfa',
              border: '1px solid rgba(139, 92, 246, 0.35)',
            }}
          >
            <RotateCcw size={12} /> Revision Requested
          </span>
        );
    }
  };

  const parsedSummary = selectedRequest ? parseSummary(selectedRequest.agentSummaryJson) : null;

  return (
    <div style={{ paddingBottom: '3rem' }}>
      {/* Toast Notification */}
      {notification && (
        <div
          style={{
            position: 'fixed',
            top: '80px',
            right: '2rem',
            zIndex: 999,
            padding: '0.85rem 1.25rem',
            borderRadius: 'var(--radius-sm)',
            background: notification.type === 'success' ? 'rgba(16, 185, 129, 0.92)' : 'rgba(239, 68, 68, 0.92)',
            color: '#fff',
            fontWeight: 600,
            fontSize: '0.9rem',
            boxShadow: '0 8px 24px rgba(0,0,0,0.4)',
            backdropFilter: 'blur(10px)',
            display: 'flex',
            alignItems: 'center',
            gap: '0.75rem',
            border: '1px solid rgba(255, 255, 255, 0.2)',
            animation: 'fadeIn 0.2s ease',
          }}
        >
          {notification.type === 'success' ? <CheckCircle2 size={18} /> : <AlertTriangle size={18} />}
          <span>{notification.message}</span>
        </div>
      )}

      {/* Header & Simulator Bar */}
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'flex-start',
          justifyContent: 'space-between',
          gap: '1.25rem',
          marginBottom: '1.75rem',
        }}
      >
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
            <h1 style={{ fontSize: '1.85rem', fontWeight: 800, margin: 0 }}>Approval Center</h1>
            <span
              style={{
                background: 'linear-gradient(135deg, rgba(56, 189, 248, 0.2), rgba(2, 132, 199, 0.35))',
                border: '1px solid var(--border-glow)',
                color: 'var(--primary-light)',
                padding: '0.2rem 0.6rem',
                borderRadius: '9999px',
                fontSize: '0.75rem',
                fontWeight: 700,
                letterSpacing: '0.05em',
                textTransform: 'uppercase',
              }}
            >
              Supervisor Portal
            </span>
          </div>
          <p style={{ color: 'var(--text-muted)', marginTop: '0.35rem', fontSize: '0.95rem' }}>
            Fleet Supervisor manual authorization &amp; risk override portal for AI Safety Guard flagged events.
          </p>
        </div>

        {/* Live Simulator Toolbar */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.75rem',
            background: 'var(--bg-card)',
            padding: '0.4rem 0.65rem',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--border-subtle)',
          }}
        >
          <span
            style={{
              fontSize: '0.75rem',
              color: 'var(--text-muted)',
              fontWeight: 600,
              textTransform: 'uppercase',
              letterSpacing: '0.05em',
              display: 'flex',
              alignItems: 'center',
              gap: '0.35rem',
            }}
          >
            <Sparkles size={14} color="var(--primary-light)" /> Safety Guard AI:
          </span>

          <button
            className="btn btn-secondary"
            style={{ padding: '0.4rem 0.75rem', fontSize: '0.8rem' }}
            disabled={simulating}
            onClick={() => handleSimulate('pending-approval')}
            title="Simulate a mission evaluation that triggers a Human-in-the-Loop pause"
          >
            {simulating ? <RefreshCw size={12} className="spinner" /> : '+ Flagged Mission (Medium Risk)'}
          </button>

          <button
            className="btn btn-secondary"
            style={{ padding: '0.4rem 0.75rem', fontSize: '0.8rem' }}
            disabled={simulating}
            onClick={() => handleSimulate('auto-reject')}
            title="Simulate high risk critical failure auto-rejection"
          >
            + Auto-Reject (High Risk)
          </button>

          <button
            className="btn btn-secondary"
            style={{ padding: '0.4rem 0.6rem' }}
            onClick={fetchData}
            title="Refresh requests"
          >
            <RefreshCw size={14} />
          </button>
        </div>
      </div>

      {/* KPI Metrics Strip */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
          gap: '1.25rem',
          marginBottom: '2rem',
        }}
      >
        <div
          className="glass-card"
          style={{
            borderLeft: '4px solid #f59e0b',
            background: 'linear-gradient(135deg, rgba(30, 41, 59, 0.8) 0%, rgba(245, 158, 11, 0.05) 100%)',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="stat-label">Pending Approval</span>
            <div
              style={{
                width: 36,
                height: 36,
                borderRadius: '8px',
                background: 'rgba(245, 158, 11, 0.15)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#fbbf24',
              }}
            >
              <Clock size={18} />
            </div>
          </div>
          <div className="stat-value" style={{ marginTop: '0.5rem', color: '#fbbf24' }}>
            {stats ? stats.totalPending : '-'}
          </div>
          <p style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
            Requires supervisor authorization
          </p>
        </div>

        <div
          className="glass-card"
          style={{
            borderLeft: '4px solid #10b981',
            background: 'linear-gradient(135deg, rgba(30, 41, 59, 0.8) 0%, rgba(16, 185, 129, 0.05) 100%)',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="stat-label">Authorized</span>
            <div
              style={{
                width: 36,
                height: 36,
                borderRadius: '8px',
                background: 'rgba(16, 185, 129, 0.15)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#34d399',
              }}
            >
              <CheckCircle2 size={18} />
            </div>
          </div>
          <div className="stat-value" style={{ marginTop: '0.5rem', color: '#34d399' }}>
            {stats ? stats.totalApproved : '-'}
          </div>
          <p style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
            Dispatches cleared for transit
          </p>
        </div>

        <div
          className="glass-card"
          style={{
            borderLeft: '4px solid #ef4444',
            background: 'linear-gradient(135deg, rgba(30, 41, 59, 0.8) 0%, rgba(239, 68, 68, 0.05) 100%)',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="stat-label">Rejected / Flagged</span>
            <div
              style={{
                width: 36,
                height: 36,
                borderRadius: '8px',
                background: 'rgba(239, 68, 68, 0.15)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#f87171',
              }}
            >
              <ShieldAlert size={18} />
            </div>
          </div>
          <div className="stat-value" style={{ marginTop: '0.5rem', color: '#f87171' }}>
            {stats ? stats.totalRejected : '-'}
          </div>
          <p style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
            Halted due to critical hazard
          </p>
        </div>

        <div
          className="glass-card"
          style={{
            borderLeft: '4px solid var(--primary-light)',
            background: 'linear-gradient(135deg, rgba(30, 41, 59, 0.8) 0%, rgba(56, 189, 248, 0.05) 100%)',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="stat-label">Avg Risk Score</span>
            <div
              style={{
                width: 36,
                height: 36,
                borderRadius: '8px',
                background: 'var(--primary-glow)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'var(--primary-light)',
              }}
            >
              <Activity size={18} />
            </div>
          </div>
          <div className="stat-value" style={{ marginTop: '0.5rem', color: 'var(--primary-light)' }}>
            {stats ? `${stats.averageRiskScore} / 100` : '-'}
          </div>
          <div
            style={{
              width: '100%',
              height: 4,
              background: 'rgba(255,255,255,0.1)',
              borderRadius: 2,
              marginTop: '0.5rem',
              overflow: 'hidden',
            }}
          >
            <div
              style={{
                width: `${stats ? Math.min(stats.averageRiskScore, 100) : 0}%`,
                height: '100%',
                background: getRiskScoreColor(stats?.averageRiskScore || 0),
                transition: 'width 0.4s ease',
              }}
            />
          </div>
        </div>
      </div>

      {/* Filter Tabs & Search Bar */}
      <div
        className="glass-card"
        style={{
          padding: '1rem 1.25rem',
          marginBottom: '1.5rem',
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '1rem',
        }}
      >
        {/* Status Pills */}
        <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
          {[
            { key: 'all', label: 'All Requests' },
            { key: 'Pending', label: 'Pending' },
            { key: 'Approved', label: 'Approved' },
            { key: 'Rejected', label: 'Rejected' },
            { key: 'RevisionRequested', label: 'Revision' },
          ].map((tab) => {
            const isActive = selectedStatus.toLowerCase() === tab.key.toLowerCase();
            return (
              <button
                key={tab.key}
                onClick={() => {
                  setSelectedStatus(tab.key);
                  setPage(1);
                }}
                style={{
                  padding: '0.45rem 0.95rem',
                  borderRadius: 'var(--radius-sm)',
                  fontSize: '0.85rem',
                  fontWeight: 600,
                  cursor: 'pointer',
                  border: isActive ? '1px solid var(--border-glow)' : '1px solid transparent',
                  background: isActive ? 'var(--primary-glow)' : 'rgba(255,255,255,0.04)',
                  color: isActive ? 'var(--primary-light)' : 'var(--text-muted)',
                  transition: 'all 0.15s ease',
                }}
              >
                {tab.label}
              </button>
            );
          })}
        </div>

        {/* Search & Sort Controls */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', flex: '1 1 320px', maxWidth: '520px' }}>
          <div style={{ position: 'relative', flex: 1 }}>
            <Search
              size={16}
              style={{
                position: 'absolute',
                left: '0.85rem',
                top: '50%',
                transform: 'translateY(-50%)',
                color: 'var(--text-faint)',
              }}
            />
            <input
              type="text"
              placeholder="Search by Dispatch ID, Rover, Reason..."
              className="form-input"
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setPage(1);
              }}
              style={{ paddingLeft: '2.5rem', paddingRight: '1rem', height: '38px' }}
            />
          </div>

          <select
            className="form-input"
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
            style={{ width: '130px', height: '38px', padding: '0 0.5rem' }}
          >
            <option value="createdAt">Date Created</option>
            <option value="riskScore">Risk Score</option>
            <option value="dispatchRequestId">Dispatch ID</option>
            <option value="roverId">Rover ID</option>
          </select>

          <button
            className="btn btn-secondary"
            onClick={() => setSortOrder((prev) => (prev === 'desc' ? 'asc' : 'desc'))}
            style={{ height: '38px', padding: '0 0.75rem' }}
            title={sortOrder === 'desc' ? 'Descending' : 'Ascending'}
          >
            {sortOrder === 'desc' ? '↓' : '↑'}
          </button>
        </div>
      </div>

      {/* Requests Table */}
      <div className="glass-card" style={{ padding: 0, overflow: 'hidden' }}>
        {loading ? (
          <div style={{ padding: '4rem', textAlign: 'center' }}>
            <div className="spinner" style={{ margin: '0 auto 1rem' }} />
            <p style={{ color: 'var(--text-muted)' }}>Querying safety approval requests...</p>
          </div>
        ) : requests.length === 0 ? (
          <div style={{ padding: '4rem 2rem', textAlign: 'center' }}>
            <div
              style={{
                width: 60,
                height: 60,
                borderRadius: '50%',
                background: 'rgba(56, 189, 248, 0.1)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                margin: '0 auto 1rem',
                color: 'var(--primary-light)',
              }}
            >
              <ShieldCheck size={32} />
            </div>
            <h3 style={{ fontSize: '1.25rem', marginBottom: '0.35rem' }}>No Approval Requests Found</h3>
            <p style={{ color: 'var(--text-muted)', maxWidth: '460px', margin: '0 auto 1.5rem', fontSize: '0.9rem' }}>
              {searchTerm
                ? 'No approval requests match your search criteria.'
                : 'All clear! When the Safety Guard Agent pauses high-risk operations, manual override requests will appear here.'}
            </p>
            <button
              className="btn btn-primary"
              onClick={() => handleSimulate('pending-approval')}
            >
              <Sparkles size={16} /> Simulate AI Safety Flag
            </button>
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
              <thead>
                <tr
                  style={{
                    background: 'rgba(15, 23, 42, 0.95)',
                    borderBottom: '1px solid var(--border-subtle)',
                    color: 'var(--text-muted)',
                    fontSize: '0.78rem',
                    textTransform: 'uppercase',
                    letterSpacing: '0.06em',
                  }}
                >
                  <th style={{ padding: '0.95rem 1.25rem' }}>Status</th>
                  <th style={{ padding: '0.95rem 1.25rem' }}>Dispatch ID</th>
                  <th style={{ padding: '0.95rem 1.25rem' }}>Rover</th>
                  <th style={{ padding: '0.95rem 1.25rem' }}>Risk Assessment</th>
                  <th style={{ padding: '0.95rem 1.25rem' }}>Reason Summary</th>
                  <th style={{ padding: '0.95rem 1.25rem' }}>Logged</th>
                  <th style={{ padding: '0.95rem 1.25rem', textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {requests.map((req) => {
                  const riskColor = getRiskScoreColor(req.riskScore);
                  return (
                    <tr
                      key={req.id}
                      style={{
                        borderBottom: '1px solid var(--border-subtle)',
                        transition: 'background 0.15s ease',
                      }}
                      onMouseEnter={(e) => (e.currentTarget.style.background = 'rgba(255,255,255,0.025)')}
                      onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}
                    >
                      <td style={{ padding: '1rem 1.25rem' }}>{getStatusBadge(req.status)}</td>
                      <td style={{ padding: '1rem 1.25rem' }}>
                        <span
                          style={{
                            fontFamily: 'monospace',
                            fontWeight: 700,
                            color: 'var(--text-main)',
                            background: 'rgba(255,255,255,0.05)',
                            padding: '0.2rem 0.5rem',
                            borderRadius: '4px',
                            border: '1px solid var(--border-subtle)',
                          }}
                        >
                          {req.dispatchRequestId}
                        </span>
                      </td>
                      <td style={{ padding: '1rem 1.25rem' }}>
                        <span
                          style={{
                            fontWeight: 600,
                            color: 'var(--primary-light)',
                          }}
                        >
                          {req.roverId}
                        </span>
                      </td>
                      <td style={{ padding: '1rem 1.25rem' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
                          <div
                            style={{
                              width: 32,
                              height: 32,
                              borderRadius: '50%',
                              border: `2px solid ${riskColor}`,
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                              fontWeight: 800,
                              fontSize: '0.8rem',
                              color: riskColor,
                            }}
                          >
                            {req.riskScore}
                          </div>
                          <div style={{ width: 65, height: 5, background: 'rgba(255,255,255,0.1)', borderRadius: 3, overflow: 'hidden' }}>
                            <div style={{ width: `${Math.min(req.riskScore, 100)}%`, height: '100%', background: riskColor }} />
                          </div>
                        </div>
                      </td>
                      <td style={{ padding: '1rem 1.25rem', maxWidth: '340px' }}>
                        <p
                          style={{
                            margin: 0,
                            fontSize: '0.85rem',
                            color: 'var(--text-muted)',
                            whiteSpace: 'nowrap',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                          }}
                          title={req.riskReason}
                        >
                          {req.riskReason}
                        </p>
                        {req.reviewedByName && (
                          <span style={{ fontSize: '0.75rem', color: 'var(--text-faint)', display: 'block', marginTop: '0.15rem' }}>
                            Reviewed by: {req.reviewedByName}
                          </span>
                        )}
                      </td>
                      <td style={{ padding: '1rem 1.25rem', fontSize: '0.825rem', color: 'var(--text-faint)' }}>
                        {new Date(req.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', month: 'short', day: 'numeric' })}
                      </td>
                      <td style={{ padding: '1rem 1.25rem', textAlign: 'right' }}>
                        <button
                          className="btn btn-secondary"
                          style={{ padding: '0.45rem 0.85rem', fontSize: '0.825rem' }}
                          onClick={() => openDetailDrawer(req)}
                        >
                          Inspect &amp; Decide <ChevronRight size={14} />
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination Footer */}
        {totalPages > 1 && (
          <div
            style={{
              padding: '1rem 1.5rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              borderTop: '1px solid var(--border-subtle)',
              fontSize: '0.85rem',
              color: 'var(--text-muted)',
            }}
          >
            <span>
              Showing {requests.length} of {totalCount} requests (Page {page} of {totalPages})
            </span>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <button
                className="btn btn-secondary"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                style={{ padding: '0.35rem 0.75rem', fontSize: '0.8rem' }}
              >
                Previous
              </button>
              <button
                className="btn btn-secondary"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                style={{ padding: '0.35rem 0.75rem', fontSize: '0.8rem' }}
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Slide-over Inspection & Decision Drawer */}
      {selectedRequest && (
        <div
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            background: 'rgba(0, 0, 0, 0.7)',
            backdropFilter: 'blur(8px)',
            zIndex: 100,
            display: 'flex',
            justifyContent: 'flex-end',
            animation: 'fadeIn 0.2s ease',
          }}
          onClick={closeDetailDrawer}
        >
          <div
            style={{
              width: '100%',
              maxWidth: '680px',
              height: '100%',
              background: 'var(--bg-surface)',
              borderLeft: '1px solid var(--border-subtle)',
              boxShadow: '-10px 0 40px rgba(0,0,0,0.6)',
              display: 'flex',
              flexDirection: 'column',
              overflow: 'hidden',
            }}
            onClick={(e) => e.stopPropagation()}
          >
            {/* Drawer Header */}
            <div
              style={{
                padding: '1.5rem',
                borderBottom: '1px solid var(--border-subtle)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                background: 'rgba(15, 23, 42, 0.8)',
              }}
            >
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
                  <h2 style={{ fontSize: '1.35rem', margin: 0 }}>Review Dispatch Approval</h2>
                  {getStatusBadge(selectedRequest.status)}
                </div>
                <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', marginTop: '0.25rem' }}>
                  Dispatch <strong style={{ color: 'var(--text-main)', fontFamily: 'monospace' }}>{selectedRequest.dispatchRequestId}</strong> • Assigned Rover <strong style={{ color: 'var(--primary-light)' }}>{selectedRequest.roverId}</strong>
                </p>
              </div>

              <button
                onClick={closeDetailDrawer}
                style={{
                  background: 'transparent',
                  border: 'none',
                  color: 'var(--text-muted)',
                  cursor: 'pointer',
                  padding: '0.5rem',
                  borderRadius: '4px',
                }}
              >
                <X size={20} />
              </button>
            </div>

            {/* Tab Navigation */}
            <div
              style={{
                display: 'flex',
                borderBottom: '1px solid var(--border-subtle)',
                background: 'rgba(255,255,255,0.02)',
              }}
            >
              <button
                onClick={() => setActiveDetailTab('assessment')}
                style={{
                  flex: 1,
                  padding: '0.85rem 1rem',
                  fontSize: '0.875rem',
                  fontWeight: 600,
                  border: 'none',
                  background: 'transparent',
                  cursor: 'pointer',
                  color: activeDetailTab === 'assessment' ? 'var(--primary-light)' : 'var(--text-muted)',
                  borderBottom: activeDetailTab === 'assessment' ? '2px solid var(--primary-light)' : '2px solid transparent',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  gap: '0.45rem',
                }}
              >
                <ShieldCheck size={16} /> AI Safety Assessment &amp; Decision
              </button>

              <button
                onClick={() => setActiveDetailTab('audit')}
                style={{
                  flex: 1,
                  padding: '0.85rem 1rem',
                  fontSize: '0.875rem',
                  fontWeight: 600,
                  border: 'none',
                  background: 'transparent',
                  cursor: 'pointer',
                  color: activeDetailTab === 'audit' ? 'var(--primary-light)' : 'var(--text-muted)',
                  borderBottom: activeDetailTab === 'audit' ? '2px solid var(--primary-light)' : '2px solid transparent',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  gap: '0.45rem',
                }}
              >
                <Layers size={16} /> Audit Trail ({executionLogs.length} Steps)
              </button>
            </div>

            {/* Drawer Body */}
            <div style={{ flex: 1, overflowY: 'auto', padding: '1.5rem' }}>
              {activeDetailTab === 'assessment' ? (
                <div>
                  {/* Safety Guard Risk Callout */}
                  <div
                    style={{
                      background: 'rgba(15, 23, 42, 0.9)',
                      border: `1px solid ${getRiskScoreColor(selectedRequest.riskScore)}40`,
                      borderRadius: 'var(--radius-md)',
                      padding: '1.25rem',
                      marginBottom: '1.5rem',
                      position: 'relative',
                      overflow: 'hidden',
                    }}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
                        <ShieldAlert size={20} color={getRiskScoreColor(selectedRequest.riskScore)} />
                        <span style={{ fontWeight: 700, fontSize: '0.95rem' }}>Safety Guard Agent Assessment</span>
                      </div>
                      <span
                        style={{
                          fontSize: '0.75rem',
                          fontWeight: 700,
                          padding: '0.2rem 0.5rem',
                          borderRadius: '4px',
                          background: `${getRiskScoreColor(selectedRequest.riskScore)}20`,
                          color: getRiskScoreColor(selectedRequest.riskScore),
                        }}
                      >
                        Risk Score: {selectedRequest.riskScore} / 100
                      </span>
                    </div>

                    {/* Progress meter */}
                    <div
                      style={{
                        width: '100%',
                        height: 6,
                        background: 'rgba(255,255,255,0.08)',
                        borderRadius: 3,
                        margin: '0.85rem 0',
                        overflow: 'hidden',
                      }}
                    >
                      <div
                        style={{
                          width: `${Math.min(selectedRequest.riskScore, 100)}%`,
                          height: '100%',
                          background: getRiskScoreColor(selectedRequest.riskScore),
                        }}
                      />
                    </div>

                    <p style={{ margin: 0, fontSize: '0.875rem', color: 'var(--text-main)', lineHeight: 1.5 }}>
                      <strong>Risk Factor:</strong> {selectedRequest.riskReason}
                    </p>
                  </div>

                  {/* Assembled Multi-Agent Pipeline Data */}
                  <h3 style={{ fontSize: '1rem', marginBottom: '0.75rem', display: 'flex', alignItems: 'center', gap: '0.45rem' }}>
                    <Activity size={16} color="var(--primary-light)" /> Assembled AI Pipeline Summary
                  </h3>

                  {/* Telemetry Status Cards */}
                  {parsedSummary?.telemetryResult && (
                    <div
                      style={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(3, 1fr)',
                        gap: '0.75rem',
                        marginBottom: '1.25rem',
                      }}
                    >
                      <div
                        style={{
                          background: 'rgba(255,255,255,0.03)',
                          border: '1px solid var(--border-subtle)',
                          borderRadius: 'var(--radius-sm)',
                          padding: '0.85rem',
                        }}
                      >
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.45rem', color: 'var(--text-muted)', fontSize: '0.75rem' }}>
                          <Battery size={14} /> Battery Margin
                        </div>
                        <div style={{ marginTop: '0.35rem', fontWeight: 700, fontSize: '0.9rem', color: parsedSummary.telemetryResult.batteryOk ? '#34d399' : '#f87171' }}>
                          {parsedSummary.telemetryResult.batteryOk ? 'Sufficient (OK)' : 'Low / Depleted'}
                        </div>
                      </div>

                      <div
                        style={{
                          background: 'rgba(255,255,255,0.03)',
                          border: '1px solid var(--border-subtle)',
                          borderRadius: 'var(--radius-sm)',
                          padding: '0.85rem',
                        }}
                      >
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.45rem', color: 'var(--text-muted)', fontSize: '0.75rem' }}>
                          <CloudRain size={14} /> Weather Risk
                        </div>
                        <div
                          style={{
                            marginTop: '0.35rem',
                            fontWeight: 700,
                            fontSize: '0.9rem',
                            textTransform: 'capitalize',
                            color: parsedSummary.telemetryResult.weatherRisk === 'high' ? '#f87171' : parsedSummary.telemetryResult.weatherRisk === 'medium' ? '#fbbf24' : '#34d399',
                          }}
                        >
                          {parsedSummary.telemetryResult.weatherRisk}
                        </div>
                      </div>

                      <div
                        style={{
                          background: 'rgba(255,255,255,0.03)',
                          border: '1px solid var(--border-subtle)',
                          borderRadius: 'var(--radius-sm)',
                          padding: '0.85rem',
                        }}
                      >
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.45rem', color: 'var(--text-muted)', fontSize: '0.75rem' }}>
                          {parsedSummary.telemetryResult.locked ? <Lock size={14} /> : <Unlock size={14} />} Hardware Lock
                        </div>
                        <div style={{ marginTop: '0.35rem', fontWeight: 700, fontSize: '0.9rem', color: parsedSummary.telemetryResult.locked ? '#34d399' : '#fbbf24' }}>
                          {parsedSummary.telemetryResult.locked ? 'Locked' : 'Unconfirmed'}
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Mission Plan Steps */}
                  <div
                    style={{
                      background: 'rgba(255,255,255,0.03)',
                      border: '1px solid var(--border-subtle)',
                      borderRadius: 'var(--radius-sm)',
                      padding: '1rem',
                      marginBottom: '1.25rem',
                    }}
                  >
                    <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 600, textTransform: 'uppercase', marginBottom: '0.65rem' }}>
                      Mission Planner Agent Steps
                    </div>
                    {parsedSummary?.missionPlanSummary?.plan && parsedSummary.missionPlanSummary.plan.length > 0 ? (
                      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                        {parsedSummary.missionPlanSummary.plan.map((step) => (
                          <div
                            key={step.stepNumber}
                            style={{
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'space-between',
                              fontSize: '0.85rem',
                              padding: '0.35rem 0',
                              borderBottom: '1px solid rgba(255,255,255,0.04)',
                            }}
                          >
                            <span style={{ color: 'var(--text-main)' }}>
                              <strong style={{ color: 'var(--primary-light)', marginRight: '0.5rem' }}>Step {step.stepNumber}:</strong>
                              {step.stepName}
                            </span>
                            <span
                              style={{
                                fontSize: '0.725rem',
                                padding: '0.15rem 0.5rem',
                                borderRadius: '4px',
                                background: step.status === 'Completed' ? 'rgba(16, 185, 129, 0.15)' : 'rgba(255,255,255,0.05)',
                                color: step.status === 'Completed' ? '#34d399' : 'var(--text-muted)',
                              }}
                            >
                              {step.status}
                            </span>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <p style={{ margin: 0, fontSize: '0.85rem', color: 'var(--text-faint)' }}>No plan steps recorded.</p>
                    )}
                  </div>

                  {/* Maintenance Mechanic Diagnosis Flag */}
                  <div
                    style={{
                      background: 'rgba(255,255,255,0.03)',
                      border: '1px solid var(--border-subtle)',
                      borderRadius: 'var(--radius-sm)',
                      padding: '1rem',
                      marginBottom: '1.75rem',
                    }}
                  >
                    <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 600, textTransform: 'uppercase', marginBottom: '0.65rem', display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                      <Wrench size={13} /> Maintenance Mechanic Diagnostics
                    </div>
                    {parsedSummary?.maintenanceResult ? (
                      <div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.4rem' }}>
                          <span style={{ fontWeight: 600, fontSize: '0.875rem' }}>
                            Report {parsedSummary.maintenanceResult.breakdownReportId}: {parsedSummary.maintenanceResult.likelyPart}
                          </span>
                          <span
                            style={{
                              fontSize: '0.75rem',
                              fontWeight: 700,
                              color: parsedSummary.maintenanceResult.severity === 'High' ? '#f87171' : '#fbbf24',
                            }}
                          >
                            Severity: {parsedSummary.maintenanceResult.severity}
                          </span>
                        </div>
                        <p style={{ margin: 0, fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                          {parsedSummary.maintenanceResult.confidenceNote} • Recommended Action:{' '}
                          <strong style={{ color: 'var(--text-main)' }}>{parsedSummary.maintenanceResult.recommendedAction}</strong>
                        </p>
                      </div>
                    ) : (
                      <p style={{ margin: 0, fontSize: '0.85rem', color: '#34d399', display: 'flex', alignItems: 'center', gap: '0.45rem' }}>
                        <CheckCircle2 size={14} /> No open breakdown reports or active maintenance flags attached to this rover.
                      </p>
                    )}
                  </div>

                  {/* Supervisor Decision Console */}
                  <div
                    style={{
                      background: 'rgba(30, 41, 59, 0.8)',
                      border: '1px solid var(--border-glow)',
                      borderRadius: 'var(--radius-md)',
                      padding: '1.25rem',
                    }}
                  >
                    <h4 style={{ fontSize: '0.95rem', margin: '0 0 0.75rem 0', display: 'flex', alignItems: 'center', gap: '0.45rem' }}>
                      <User size={16} color="var(--primary-light)" /> Supervisor Action Console
                    </h4>

                    {selectedRequest.status === 'Pending' ? (
                      <div>
                        <label className="form-label" style={{ marginBottom: '0.4rem' }}>
                          Review Notes &amp; Authorization Rationale
                        </label>
                        <textarea
                          className="form-input"
                          rows={3}
                          placeholder="e.g. Route clearance verified manually with floor operator. Battery safety margin confirmed."
                          value={reviewNotes}
                          onChange={(e) => setReviewNotes(e.target.value)}
                          style={{ resize: 'vertical', marginBottom: '0.75rem' }}
                        />

                        {/* Quick templates */}
                        <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap', marginBottom: '1.25rem' }}>
                          {[
                            'Approved: Transit zone confirmed clear.',
                            'Override: Battery verified on rapid charger.',
                            'Reject: High weather hazard in transit corridor.',
                          ].map((tmpl) => (
                            <button
                              key={tmpl}
                              type="button"
                              onClick={() => setReviewNotes(tmpl)}
                              style={{
                                fontSize: '0.725rem',
                                background: 'rgba(255,255,255,0.05)',
                                border: '1px solid var(--border-subtle)',
                                borderRadius: '4px',
                                padding: '0.2rem 0.5rem',
                                color: 'var(--text-muted)',
                                cursor: 'pointer',
                              }}
                            >
                              + {tmpl}
                            </button>
                          ))}
                        </div>

                        {/* Decision Buttons */}
                        <div style={{ display: 'flex', gap: '0.75rem' }}>
                          <button
                            className="btn btn-primary"
                            style={{
                              flex: 1,
                              background: 'linear-gradient(135deg, #059669 0%, #10b981 100%)',
                              boxShadow: '0 4px 14px rgba(16, 185, 129, 0.3)',
                            }}
                            disabled={actionLoading}
                            onClick={() => handleDecision('approve')}
                          >
                            {actionLoading ? <RefreshCw size={14} className="spinner" /> : <CheckCircle2 size={16} />}
                            Approve Mission
                          </button>

                          <button
                            className="btn btn-secondary"
                            style={{
                              flex: 1,
                              borderColor: 'rgba(245, 158, 11, 0.4)',
                              color: '#fbbf24',
                            }}
                            disabled={actionLoading}
                            onClick={() => handleDecision('revision')}
                          >
                            <RotateCcw size={16} /> Request Revision
                          </button>

                          <button
                            className="btn btn-secondary"
                            style={{
                              flex: 1,
                              borderColor: 'rgba(239, 68, 68, 0.4)',
                              color: '#f87171',
                            }}
                            disabled={actionLoading}
                            onClick={() => handleDecision('reject')}
                          >
                            <XCircle size={16} /> Reject Mission
                          </button>
                        </div>
                      </div>
                    ) : (
                      <div
                        style={{
                          padding: '1rem',
                          background: 'rgba(255,255,255,0.02)',
                          borderRadius: 'var(--radius-sm)',
                          border: '1px solid var(--border-subtle)',
                        }}
                      >
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                          {getStatusBadge(selectedRequest.status)}
                          <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                            by <strong>{selectedRequest.reviewedByName || 'Supervisor'}</strong> on{' '}
                            {new Date(selectedRequest.updatedAt).toLocaleString()}
                          </span>
                        </div>
                        {selectedRequest.reviewNotes ? (
                          <p style={{ margin: 0, fontSize: '0.875rem', color: 'var(--text-main)', fontStyle: 'italic' }}>
                            "{selectedRequest.reviewNotes}"
                          </p>
                        ) : (
                          <p style={{ margin: 0, fontSize: '0.8rem', color: 'var(--text-faint)' }}>No review notes recorded.</p>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              ) : (
                /* Tab 2: Auditable Execution Log Timeline */
                <div>
                  <div style={{ marginBottom: '1.25rem' }}>
                    <h3 style={{ fontSize: '1.05rem', margin: '0 0 0.35rem 0' }}>Workflow Execution Audit Trail</h3>
                    <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', margin: 0 }}>
                      Complete immutable log of agent pipeline steps, inputs, and validation results.
                    </p>
                  </div>

                  {logsLoading ? (
                    <div style={{ padding: '3rem', textAlign: 'center' }}>
                      <div className="spinner" style={{ margin: '0 auto 0.75rem' }} />
                      <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>Loading execution trail...</p>
                    </div>
                  ) : executionLogs.length === 0 ? (
                    <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-faint)' }}>
                      No execution logs recorded for this dispatch request.
                    </div>
                  ) : (
                    <div style={{ position: 'relative', paddingLeft: '1.5rem' }}>
                      {/* Timeline line */}
                      <div
                        style={{
                          position: 'absolute',
                          left: '7px',
                          top: '12px',
                          bottom: '12px',
                          width: '2px',
                          background: 'rgba(56, 189, 248, 0.25)',
                        }}
                      />

                      {executionLogs.map((log) => {
                        const isExpanded = expandedLogId === log.id;
                        return (
                          <div
                            key={log.id}
                            style={{
                              position: 'relative',
                              marginBottom: '1.5rem',
                            }}
                          >
                            {/* Dot */}
                            <div
                              style={{
                                position: 'absolute',
                                left: '-1.5rem',
                                top: '4px',
                                width: '16px',
                                height: '16px',
                                borderRadius: '50%',
                                background: 'var(--bg-surface)',
                                border: '3px solid var(--primary-light)',
                                boxShadow: '0 0 10px var(--primary-glow)',
                              }}
                            />

                            {/* Card */}
                            <div
                              style={{
                                background: 'rgba(255,255,255,0.03)',
                                border: '1px solid var(--border-subtle)',
                                borderRadius: 'var(--radius-sm)',
                                padding: '1rem',
                              }}
                            >
                              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                  <span style={{ fontWeight: 700, fontSize: '0.9rem', color: 'var(--text-main)' }}>
                                    {log.stepName}
                                  </span>
                                  <span
                                    style={{
                                      fontSize: '0.725rem',
                                      padding: '0.15rem 0.5rem',
                                      borderRadius: '4px',
                                      background: 'rgba(56, 189, 248, 0.1)',
                                      color: 'var(--primary-light)',
                                    }}
                                  >
                                    {log.agentName}
                                  </span>
                                </div>
                                <span style={{ fontSize: '0.75rem', color: 'var(--text-faint)' }}>
                                  {new Date(log.timestamp).toLocaleTimeString()}
                                </span>
                              </div>

                              <div style={{ marginTop: '0.4rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                <span style={{ fontSize: '0.78rem', color: 'var(--text-muted)' }}>Outcome:</span>
                                <span
                                  style={{
                                    fontSize: '0.75rem',
                                    fontWeight: 600,
                                    color:
                                      log.validationResult.includes('Approved')
                                        ? '#34d399'
                                        : log.validationResult.includes('Reject')
                                        ? '#f87171'
                                        : '#fbbf24',
                                  }}
                                >
                                  {log.validationResult}
                                </span>
                              </div>

                              {/* Expand payloads */}
                              <div style={{ marginTop: '0.75rem' }}>
                                <button
                                  type="button"
                                  onClick={() => setExpandedLogId(isExpanded ? null : log.id)}
                                  style={{
                                    background: 'transparent',
                                    border: 'none',
                                    color: 'var(--primary-light)',
                                    fontSize: '0.78rem',
                                    cursor: 'pointer',
                                    display: 'flex',
                                    alignItems: 'center',
                                    gap: '0.35rem',
                                    padding: 0,
                                  }}
                                >
                                  <Code2 size={13} /> {isExpanded ? 'Hide JSON Payloads' : 'View Payload Details'}
                                </button>

                                {isExpanded && (
                                  <div style={{ marginTop: '0.75rem', display: 'flex', flexDirection: 'column', gap: '0.65rem' }}>
                                    <div>
                                      <div style={{ fontSize: '0.725rem', color: 'var(--text-muted)', marginBottom: '0.2rem' }}>Input Payload:</div>
                                      <pre
                                        style={{
                                          margin: 0,
                                          background: 'rgba(0,0,0,0.5)',
                                          padding: '0.65rem',
                                          borderRadius: '4px',
                                          fontSize: '0.75rem',
                                          overflowX: 'auto',
                                          color: '#94a3b8',
                                          maxHeight: '140px',
                                        }}
                                      >
                                        {JSON.stringify(JSON.parse(log.inputJson || '{}'), null, 2)}
                                      </pre>
                                    </div>
                                    <div>
                                      <div style={{ fontSize: '0.725rem', color: 'var(--text-muted)', marginBottom: '0.2rem' }}>Output Payload:</div>
                                      <pre
                                        style={{
                                          margin: 0,
                                          background: 'rgba(0,0,0,0.5)',
                                          padding: '0.65rem',
                                          borderRadius: '4px',
                                          fontSize: '0.75rem',
                                          overflowX: 'auto',
                                          color: '#94a3b8',
                                          maxHeight: '140px',
                                        }}
                                      >
                                        {JSON.stringify(JSON.parse(log.outputJson || '{}'), null, 2)}
                                      </pre>
                                    </div>
                                  </div>
                                )}
                              </div>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { breakdownApi } from '../api/breakdownApi';
import {
  BreakdownReport,
  BreakdownStatus,
  MaintenanceMechanicOutput,
} from '../types/breakdown';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { ErrorAlert } from '../components/Common/ErrorAlert';
import {
  Wrench,
  Bot,
  AlertTriangle,
  CheckCircle2,
  Clock,
  Eye,
  RefreshCw,
  Search,
  X,
  ChevronLeft,
  ChevronRight,
  Sparkles,
  PlusCircle,
  Upload,
} from 'lucide-react';

export const MaintenanceQueue: React.FC = () => {
  const { user } = useAuth();
  const isTechnicianOrSupervisor = user?.role === 'Technician' || user?.role === 'Supervisor';

  // State
  const [reports, setReports] = useState<BreakdownReport[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [page, setPage] = useState<number>(1);
  const [pageSize] = useState<number>(10);
  const [selectedStatus, setSelectedStatus] = useState<BreakdownStatus | 'ALL'>('ALL');
  const [searchQuery, setSearchQuery] = useState<string>('');

  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Agent Diagnosis State
  const [diagnosingId, setDiagnosingId] = useState<string | null>(null);
  const [activeDiagnosis, setActiveDiagnosis] = useState<{
    report: BreakdownReport;
    result: MaintenanceMechanicOutput;
  } | null>(null);

  // Lightbox modal for photo
  const [previewPhotoUrl, setPreviewPhotoUrl] = useState<string | null>(null);

  // Status update tracking
  const [updatingStatusId, setUpdatingStatusId] = useState<string | null>(null);

  // New Breakdown Report Modal State
  const [isCreateModalOpen, setIsCreateModalOpen] = useState<boolean>(false);
  const [newCategory, setNewCategory] = useState<string>('MotorOverheating');
  const [newErrorCode, setNewErrorCode] = useState<string>('');
  const [newDescription, setNewDescription] = useState<string>('');
  const [newPhotoFile, setNewPhotoFile] = useState<File | null>(null);
  const [photoPreview, setPhotoPreview] = useState<string | null>(null);
  const [isSubmittingReport, setIsSubmittingReport] = useState<boolean>(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const handlePhotoSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      setNewPhotoFile(file);
      setPhotoPreview(URL.createObjectURL(file));
    }
  };

  const handleCreateReport = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newDescription.trim()) {
      setCreateError('Please provide a description of the issue.');
      return;
    }

    setIsSubmittingReport(true);
    setCreateError(null);

    try {
      const formData = new FormData();
      formData.append('SymptomCategory', newCategory);
      formData.append('Description', newDescription.trim());
      if (newErrorCode.trim()) {
        formData.append('ErrorCode', newErrorCode.trim().toUpperCase());
      }
      if (newPhotoFile) {
        formData.append('Photo', newPhotoFile);
      }

      await breakdownApi.createReport(formData);
      setIsCreateModalOpen(false);
      setNewDescription('');
      setNewErrorCode('');
      setNewPhotoFile(null);
      setPhotoPreview(null);
      await fetchReports();
    } catch (err: any) {
      setCreateError(err.response?.data?.message || err.message || 'Failed to submit breakdown report.');
    } finally {
      setIsSubmittingReport(false);
    }
  };

  const fetchReports = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await breakdownApi.getReports({
        status: selectedStatus === 'ALL' ? undefined : selectedStatus,
        page,
        pageSize,
      });
      setReports(data.items);
      setTotalCount(data.totalCount);
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to load maintenance queue.');
    } finally {
      setIsLoading(false);
    }
  }, [selectedStatus, page, pageSize]);

  useEffect(() => {
    fetchReports();
  }, [fetchReports]);

  // Handle agent diagnosis trigger
  const handleRunDiagnosis = async (report: BreakdownReport) => {
    setDiagnosingId(report.id);
    setError(null);
    try {
      const diagnosis = await breakdownApi.diagnoseReport(report.id);
      setActiveDiagnosis({ report, result: diagnosis });
      // Refresh reports list to reflect updated diagnosis & status
      await fetchReports();
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Diagnostic agent failed to run.');
    } finally {
      setDiagnosingId(null);
    }
  };

  // Handle status update
  const handleStatusChange = async (reportId: string, newStatus: BreakdownStatus) => {
    setUpdatingStatusId(reportId);
    try {
      await breakdownApi.updateStatus(reportId, newStatus);
      await fetchReports();
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to update status.');
    } finally {
      setUpdatingStatusId(null);
    }
  };

  // Filtered reports locally by search query
  const filteredReports = reports.filter((r) => {
    if (!searchQuery) return true;
    const q = searchQuery.toLowerCase();
    return (
      r.symptomCategory.toLowerCase().includes(q) ||
      r.description.toLowerCase().includes(q) ||
      (r.errorCode && r.errorCode.toLowerCase().includes(q)) ||
      (r.roverId && r.roverId.toLowerCase().includes(q)) ||
      r.reportedByName.toLowerCase().includes(q)
    );
  });

  // Calculate metrics
  const totalReported = reports.filter((r) => r.status === 'Reported').length;
  const totalDiagnosing = reports.filter((r) => r.status === 'Diagnosing').length;
  const totalScheduled = reports.filter((r) => r.status === 'ScheduledForRepair').length;
  const totalRepaired = reports.filter((r) => r.status === 'Repaired').length;

  const getStatusBadgeStyle = (status: BreakdownStatus) => {
    switch (status) {
      case 'Reported':
        return { bg: 'rgba(239, 68, 68, 0.15)', text: '#f87171', border: 'rgba(239, 68, 68, 0.3)' };
      case 'Diagnosing':
        return { bg: 'rgba(245, 158, 11, 0.15)', text: '#fbbf24', border: 'rgba(245, 158, 11, 0.3)' };
      case 'ScheduledForRepair':
        return { bg: 'rgba(56, 189, 248, 0.15)', text: '#38bdf8', border: 'rgba(56, 189, 248, 0.3)' };
      case 'Repaired':
        return { bg: 'rgba(16, 185, 129, 0.15)', text: '#34d399', border: 'rgba(16, 185, 129, 0.3)' };
    }
  };

  const statusOptions: BreakdownStatus[] = ['Reported', 'Diagnosing', 'ScheduledForRepair', 'Repaired'];

  return (
    <div>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1.75rem' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', marginBottom: '0.35rem', display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
            <Wrench size={26} color="var(--primary-light)" />
            Maintenance Queue
          </h1>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.925rem' }}>
            Live triage of rover malfunction reports with automated diagnostics by the Maintenance Mechanic Agent.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
          <button
            onClick={() => setIsCreateModalOpen(true)}
            className="btn btn-primary"
            style={{ display: 'inline-flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.85rem' }}
          >
            <PlusCircle size={16} />
            Report Breakdown
          </button>
          <button
            onClick={fetchReports}
            disabled={isLoading}
            className="btn btn-secondary"
            style={{ display: 'inline-flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.85rem' }}
          >
            <RefreshCw size={15} className={isLoading ? 'spin-icon' : ''} />
            Refresh
          </button>
        </div>
      </div>

      {error && (
        <div style={{ marginBottom: '1.5rem' }}>
          <ErrorAlert message={error} onDismiss={() => setError(null)} />
        </div>
      )}

      {/* KPI Stats Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1rem', marginBottom: '1.75rem' }}>
        <div className="glass-card" style={{ display: 'flex', alignItems: 'center', gap: '1rem', padding: '1.25rem' }}>
          <div style={{ width: 44, height: 44, borderRadius: 10, background: 'rgba(239, 68, 68, 0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#f87171' }}>
            <AlertTriangle size={22} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>New Incidents</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 800, color: '#f87171' }}>{totalReported}</div>
          </div>
        </div>

        <div className="glass-card" style={{ display: 'flex', alignItems: 'center', gap: '1rem', padding: '1.25rem' }}>
          <div style={{ width: 44, height: 44, borderRadius: 10, background: 'rgba(245, 158, 11, 0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fbbf24' }}>
            <Bot size={22} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Under Diagnosis</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 800, color: '#fbbf24' }}>{totalDiagnosing}</div>
          </div>
        </div>

        <div className="glass-card" style={{ display: 'flex', alignItems: 'center', gap: '1rem', padding: '1.25rem' }}>
          <div style={{ width: 44, height: 44, borderRadius: 10, background: 'rgba(56, 189, 248, 0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#38bdf8' }}>
            <Clock size={22} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Scheduled Repair</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 800, color: '#38bdf8' }}>{totalScheduled}</div>
          </div>
        </div>

        <div className="glass-card" style={{ display: 'flex', alignItems: 'center', gap: '1rem', padding: '1.25rem' }}>
          <div style={{ width: 44, height: 44, borderRadius: 10, background: 'rgba(16, 185, 129, 0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#34d399' }}>
            <CheckCircle2 size={22} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Resolved / Repaired</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 800, color: '#34d399' }}>{totalRepaired}</div>
          </div>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="glass-card" style={{ padding: '1rem', marginBottom: '1.5rem', display: 'flex', flexWrap: 'wrap', gap: '1rem', alignItems: 'center', justifyContent: 'space-between' }}>
        {/* Status Pills */}
        <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
          {(['ALL', 'Reported', 'Diagnosing', 'ScheduledForRepair', 'Repaired'] as const).map((st) => {
            const isActive = selectedStatus === st;
            return (
              <button
                key={st}
                onClick={() => {
                  setSelectedStatus(st);
                  setPage(1);
                }}
                className={`btn ${isActive ? 'btn-primary' : 'btn-secondary'}`}
                style={{
                  fontSize: '0.8rem',
                  padding: '0.4rem 0.85rem',
                  borderRadius: 'var(--radius-full)',
                }}
              >
                {st === 'ALL' ? 'All Incidents' : st === 'ScheduledForRepair' ? 'Scheduled' : st}
              </button>
            );
          })}
        </div>

        {/* Search Field */}
        <div style={{ position: 'relative', minWidth: '240px' }}>
          <Search size={16} color="var(--text-muted)" style={{ position: 'absolute', left: '0.85rem', top: '50%', transform: 'translateY(-50%)' }} />
          <input
            type="text"
            placeholder="Search symptoms, rover, error code..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="form-input"
            style={{ paddingLeft: '2.4rem', fontSize: '0.85rem', paddingRight: '0.85rem' }}
          />
        </div>
      </div>

      {/* Breakdown Reports Table */}
      {isLoading ? (
        <div className="glass-card" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', padding: '4rem 2rem' }}>
          <LoadingSpinner text="Querying Maintenance Queue..." />
        </div>
      ) : filteredReports.length === 0 ? (
        <div className="empty-state-card">
          <div className="empty-state-icon">
            <Wrench size={32} color="#fbbf24" />
          </div>
          <h3 style={{ marginBottom: '0.5rem' }}>No Breakdown Reports Found</h3>
          <p style={{ color: 'var(--text-muted)', maxWidth: 450 }}>
            {selectedStatus !== 'ALL'
              ? `No incident reports currently match the "${selectedStatus}" status.`
              : 'The warehouse fleet is operating smoothly. New breakdown reports filed by operators will appear here for diagnostic triaging.'}
          </p>
        </div>
      ) : (
        <div className="glass-card" style={{ padding: 0, overflow: 'hidden' }}>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.875rem' }}>
              <thead>
                <tr style={{ background: 'rgba(15, 23, 42, 0.9)', borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-muted)', fontSize: '0.75rem', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                  <th style={{ padding: '1rem 1.25rem' }}>Photo</th>
                  <th style={{ padding: '1rem 1.25rem' }}>Rover / ID</th>
                  <th style={{ padding: '1rem 1.25rem' }}>Symptom Category</th>
                  <th style={{ padding: '1rem 1.25rem' }}>Description</th>
                  <th style={{ padding: '1rem 1.25rem' }}>Status</th>
                  <th style={{ padding: '1rem 1.25rem' }}>Reported By</th>
                  <th style={{ padding: '1rem 1.25rem', textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredReports.map((report) => {
                  const badgeStyle = getStatusBadgeStyle(report.status);
                  const isDiagnosingThis = diagnosingId === report.id;
                  const isUpdatingThis = updatingStatusId === report.id;

                  return (
                    <tr
                      key={report.id}
                      style={{
                        borderBottom: '1px solid var(--border-subtle)',
                        transition: 'background var(--transition-fast)',
                      }}
                      onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'rgba(255, 255, 255, 0.02)')}
                      onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
                    >
                      {/* Photo Thumbnail */}
                      <td style={{ padding: '1rem 1.25rem' }}>
                        {report.photoUrl ? (
                          <div
                            onClick={() => setPreviewPhotoUrl(report.photoUrl)}
                            title="Click to view full photo"
                            style={{
                              width: 46,
                              height: 46,
                              borderRadius: 8,
                              overflow: 'hidden',
                              cursor: 'pointer',
                              border: '1px solid var(--border-subtle)',
                              position: 'relative',
                            }}
                          >
                            <img
                              src={report.photoUrl}
                              alt="Breakdown evidence"
                              style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                            />
                            <div style={{ position: 'absolute', inset: 0, background: 'rgba(0,0,0,0.3)', display: 'flex', alignItems: 'center', justifyContent: 'center', opacity: 0, transition: 'opacity 0.2s' }}
                              onMouseEnter={(e) => (e.currentTarget.style.opacity = '1')}
                              onMouseLeave={(e) => (e.currentTarget.style.opacity = '0')}
                            >
                              <Eye size={16} color="#fff" />
                            </div>
                          </div>
                        ) : (
                          <div
                            style={{
                              width: 46,
                              height: 46,
                              borderRadius: 8,
                              background: 'rgba(255,255,255,0.04)',
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                              color: 'var(--text-faint)',
                              fontSize: '0.65rem',
                            }}
                          >
                            No Pic
                          </div>
                        )}
                      </td>

                      {/* Rover / ID */}
                      <td style={{ padding: '1rem 1.25rem' }}>
                        <div style={{ fontWeight: 600, color: 'var(--text-main)' }}>
                          {report.roverId ? `RO-${report.roverId.substring(0, 5).toUpperCase()}` : 'Unassigned Rover'}
                        </div>
                        <div style={{ fontSize: '0.72rem', color: 'var(--text-faint)', fontFamily: 'monospace' }}>
                          ID: {report.id.substring(0, 8)}
                        </div>
                      </td>

                      {/* Symptom Category & Error Code */}
                      <td style={{ padding: '1rem 1.25rem' }}>
                        <span
                          style={{
                            display: 'inline-block',
                            padding: '0.2rem 0.55rem',
                            borderRadius: 4,
                            background: 'rgba(56, 189, 248, 0.1)',
                            color: 'var(--primary-light)',
                            fontSize: '0.75rem',
                            fontWeight: 600,
                            marginBottom: report.errorCode ? '0.25rem' : 0,
                          }}
                        >
                          {report.symptomCategory}
                        </span>
                        {report.errorCode && (
                          <div>
                            <span style={{ fontSize: '0.7rem', color: '#fbbf24', background: 'rgba(251, 191, 36, 0.12)', padding: '0.1rem 0.4rem', borderRadius: 4, fontFamily: 'monospace' }}>
                              ERR: {report.errorCode}
                            </span>
                          </div>
                        )}
                      </td>

                      {/* Description */}
                      <td style={{ padding: '1rem 1.25rem', maxWidth: '300px' }}>
                        <div
                          style={{
                            color: 'var(--text-main)',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            display: '-webkit-box',
                            WebkitLineClamp: 2,
                            WebkitBoxOrient: 'vertical',
                          }}
                        >
                          {report.description}
                        </div>
                        {report.diagnosisResult && (
                          <div style={{ marginTop: '0.35rem', fontSize: '0.72rem', color: '#38bdf8', display: 'flex', alignItems: 'center', gap: '0.25rem' }}>
                            <Bot size={13} />
                            <span>AI: {report.diagnosisResult.likelyPart} ({report.diagnosisResult.recommendedAction})</span>
                          </div>
                        )}
                      </td>

                      {/* Status */}
                      <td style={{ padding: '1rem 1.25rem' }}>
                        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.35rem' }}>
                          <span
                            style={{
                              display: 'inline-flex',
                              alignItems: 'center',
                              gap: '0.35rem',
                              padding: '0.25rem 0.6rem',
                              borderRadius: 'var(--radius-full)',
                              fontSize: '0.725rem',
                              fontWeight: 700,
                              background: badgeStyle.bg,
                              color: badgeStyle.text,
                              border: `1px solid ${badgeStyle.border}`,
                              width: 'fit-content',
                            }}
                          >
                            <span style={{ width: 6, height: 6, borderRadius: '50%', background: badgeStyle.text }} />
                            {report.status}
                          </span>

                          {/* Technician controls to update status */}
                          {isTechnicianOrSupervisor && (
                            <select
                              disabled={isUpdatingThis}
                              value={report.status}
                              onChange={(e) => handleStatusChange(report.id, e.target.value as BreakdownStatus)}
                              style={{
                                background: 'rgba(15, 23, 42, 0.8)',
                                border: '1px solid var(--border-subtle)',
                                color: 'var(--text-muted)',
                                borderRadius: 4,
                                padding: '0.15rem 0.35rem',
                                fontSize: '0.7rem',
                                outline: 'none',
                                cursor: 'pointer',
                              }}
                            >
                              {statusOptions.map((st) => (
                                <option key={st} value={st}>
                                  Set: {st}
                                </option>
                              ))}
                            </select>
                          )}
                        </div>
                      </td>

                      {/* Reported By & Date */}
                      <td style={{ padding: '1rem 1.25rem' }}>
                        <div style={{ color: 'var(--text-main)', fontWeight: 500 }}>{report.reportedByName}</div>
                        <div style={{ fontSize: '0.72rem', color: 'var(--text-faint)' }}>
                          {new Date(report.createdAt).toLocaleDateString()} {new Date(report.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </div>
                      </td>

                      {/* Actions */}
                      <td style={{ padding: '1rem 1.25rem', textAlign: 'right' }}>
                        <div style={{ display: 'inline-flex', gap: '0.4rem', alignItems: 'center' }}>
                          {/* Run Diagnosis Button */}
                          <button
                            onClick={() => handleRunDiagnosis(report)}
                            disabled={isDiagnosingThis}
                            className="btn btn-primary"
                            style={{
                              fontSize: '0.75rem',
                              padding: '0.35rem 0.75rem',
                              display: 'inline-flex',
                              alignItems: 'center',
                              gap: '0.35rem',
                            }}
                            title="Run Maintenance Mechanic Agent diagnostics"
                          >
                            {isDiagnosingThis ? (
                              <>
                                <RefreshCw size={13} className="spin-icon" />
                                <span>Diagnosing...</span>
                              </>
                            ) : (
                              <>
                                <Sparkles size={13} />
                                <span>{report.diagnosisResult ? 'Re-Diagnose' : 'Run Diagnosis'}</span>
                              </>
                            )}
                          </button>

                          {/* If already diagnosed, button to open recommendation modal */}
                          {report.diagnosisResult && (
                            <button
                              onClick={() => setActiveDiagnosis({ report, result: report.diagnosisResult! })}
                              className="btn btn-secondary"
                              style={{ fontSize: '0.75rem', padding: '0.35rem 0.55rem' }}
                              title="View AI Agent Recommendation"
                            >
                              <Eye size={14} />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Pagination Controls */}
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              padding: '0.85rem 1.25rem',
              borderTop: '1px solid var(--border-subtle)',
              fontSize: '0.8rem',
              color: 'var(--text-muted)',
            }}
          >
            <div>
              Showing {filteredReports.length} of {totalCount} incident reports
            </div>

            <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1 || isLoading}
                className="btn btn-secondary"
                style={{ padding: '0.3rem 0.6rem', fontSize: '0.75rem' }}
              >
                <ChevronLeft size={14} />
                Previous
              </button>
              <span style={{ padding: '0 0.5rem', fontWeight: 600, color: 'var(--text-main)' }}>
                Page {page}
              </span>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={page * pageSize >= totalCount || isLoading}
                className="btn btn-secondary"
                style={{ padding: '0.3rem 0.6rem', fontSize: '0.75rem' }}
              >
                Next
                <ChevronRight size={14} />
              </button>
            </div>
          </div>
        </div>
      )}

      {/* AI Agent Recommendation Modal */}
      {activeDiagnosis && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.75)',
            backdropFilter: 'blur(8px)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 100,
            padding: '1rem',
          }}
          onClick={() => setActiveDiagnosis(null)}
        >
          <div
            className="glass-card"
            style={{
              maxWidth: '560px',
              width: '100%',
              padding: '2rem',
              border: '1px solid var(--border-glow)',
              position: 'relative',
              boxShadow: 'var(--shadow-lg)',
            }}
            onClick={(e) => e.stopPropagation()}
          >
            {/* Close Button */}
            <button
              onClick={() => setActiveDiagnosis(null)}
              style={{
                position: 'absolute',
                top: '1.25rem',
                right: '1.25rem',
                background: 'transparent',
                border: 'none',
                color: 'var(--text-muted)',
                cursor: 'pointer',
              }}
            >
              <X size={20} />
            </button>

            {/* Modal Header */}
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.25rem' }}>
              <div
                style={{
                  width: 44,
                  height: 44,
                  borderRadius: 12,
                  background: 'var(--primary-gradient)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: '#fff',
                  boxShadow: 'var(--shadow-glow)',
                }}
              >
                <Bot size={24} />
              </div>
              <div>
                <h3 style={{ fontSize: '1.25rem' }}>Maintenance Mechanic Agent</h3>
                <div style={{ fontSize: '0.75rem', color: 'var(--primary-light)', letterSpacing: '0.05em', textTransform: 'uppercase', fontWeight: 600 }}>
                  Automated Diagnostics & Triage Output
                </div>
              </div>
            </div>

            {/* Report Reference */}
            <div
              style={{
                padding: '0.75rem 1rem',
                background: 'rgba(15, 23, 42, 0.6)',
                borderRadius: 'var(--radius-sm)',
                border: '1px solid var(--border-subtle)',
                marginBottom: '1.25rem',
                fontSize: '0.8rem',
              }}
            >
              <span style={{ color: 'var(--text-muted)' }}>Report ID: </span>
              <span style={{ fontFamily: 'monospace', color: 'var(--text-main)', fontWeight: 600 }}>{activeDiagnosis.result.breakdownReportId}</span>
              <span style={{ margin: '0 0.5rem', color: 'var(--text-faint)' }}>•</span>
              <span style={{ color: 'var(--text-muted)' }}>Category: </span>
              <span style={{ color: '#38bdf8', fontWeight: 600 }}>{activeDiagnosis.report.symptomCategory}</span>
            </div>

            {/* Structured Contract Fields */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1.25rem' }}>
              {/* Likely Part */}
              <div style={{ background: 'rgba(255, 255, 255, 0.03)', padding: '0.85rem', borderRadius: 8, border: '1px solid var(--border-subtle)' }}>
                <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Likely Faulty Part</div>
                <div style={{ fontSize: '1.05rem', fontWeight: 700, color: 'var(--text-main)', marginTop: '0.2rem' }}>
                  {activeDiagnosis.result.likelyPart}
                </div>
              </div>

              {/* Estimated Repair Hours */}
              <div style={{ background: 'rgba(255, 255, 255, 0.03)', padding: '0.85rem', borderRadius: 8, border: '1px solid var(--border-subtle)' }}>
                <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Estimated Repair Hours</div>
                <div style={{ fontSize: '1.05rem', fontWeight: 700, color: '#fbbf24', marginTop: '0.2rem' }}>
                  {activeDiagnosis.result.estimatedRepairHours} {activeDiagnosis.result.estimatedRepairHours === 1 ? 'Hour' : 'Hours'}
                </div>
              </div>

              {/* Severity */}
              <div style={{ background: 'rgba(255, 255, 255, 0.03)', padding: '0.85rem', borderRadius: 8, border: '1px solid var(--border-subtle)' }}>
                <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Severity Assessment</div>
                <div style={{ fontSize: '1rem', fontWeight: 700, color: activeDiagnosis.result.severity === 'Critical' || activeDiagnosis.result.severity === 'High' ? '#f87171' : '#fbbf24', marginTop: '0.2rem' }}>
                  {activeDiagnosis.result.severity}
                </div>
              </div>

              {/* Recommended Action */}
              <div style={{ background: 'rgba(255, 255, 255, 0.03)', padding: '0.85rem', borderRadius: 8, border: '1px solid var(--border-subtle)' }}>
                <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Recommended Action</div>
                <div style={{ fontSize: '1rem', fontWeight: 700, color: activeDiagnosis.result.recommendedAction === 'ScheduleRepair' ? '#34d399' : '#fbbf24', marginTop: '0.2rem' }}>
                  {activeDiagnosis.result.recommendedAction}
                </div>
              </div>
            </div>

            {/* Confidence Note */}
            <div style={{ background: 'rgba(15, 23, 42, 0.8)', padding: '0.85rem', borderRadius: 8, border: '1px solid var(--border-subtle)', marginBottom: '1.5rem', fontSize: '0.825rem' }}>
              <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '0.2rem' }}>Confidence / Match Note</div>
              <div style={{ color: 'var(--text-main)' }}>{activeDiagnosis.result.confidenceNote}</div>
            </div>

            {/* Actions */}
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              {isTechnicianOrSupervisor && activeDiagnosis.report.status !== 'ScheduledForRepair' && activeDiagnosis.result.recommendedAction === 'ScheduleRepair' && (
                <button
                  onClick={() => {
                    handleStatusChange(activeDiagnosis.report.id, 'ScheduledForRepair');
                    setActiveDiagnosis(null);
                  }}
                  className="btn btn-primary"
                  style={{ fontSize: '0.85rem' }}
                >
                  <Wrench size={15} />
                  Approve & Schedule Repair
                </button>
              )}
              <button
                onClick={() => setActiveDiagnosis(null)}
                className="btn btn-secondary"
                style={{ fontSize: '0.85rem' }}
              >
                Dismiss
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Create Breakdown Report Modal */}
      {isCreateModalOpen && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.75)',
            backdropFilter: 'blur(8px)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 100,
            padding: '1.5rem',
          }}
          onClick={() => !isSubmittingReport && setIsCreateModalOpen(false)}
        >
          <div
            className="glass-panel"
            style={{
              width: '100%',
              maxWidth: '540px',
              maxHeight: '90vh',
              overflowY: 'auto',
              borderRadius: 16,
              padding: '1.75rem',
              position: 'relative',
              boxShadow: 'var(--shadow-xl)',
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
                <div style={{ width: 36, height: 36, borderRadius: 8, background: 'rgba(239, 68, 68, 0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#f87171' }}>
                  <AlertTriangle size={20} />
                </div>
                <div>
                  <h3 style={{ fontSize: '1.15rem', fontWeight: 700, margin: 0 }}>Report Rover Breakdown</h3>
                  <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', margin: 0 }}>Submit an incident for automated diagnostic analysis</p>
                </div>
              </div>
              <button
                onClick={() => setIsCreateModalOpen(false)}
                disabled={isSubmittingReport}
                style={{ background: 'transparent', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}
              >
                <X size={20} />
              </button>
            </div>

            {createError && (
              <div style={{ marginBottom: '1rem' }}>
                <ErrorAlert message={createError} onDismiss={() => setCreateError(null)} />
              </div>
            )}

            <form onSubmit={handleCreateReport} style={{ display: 'flex', flexDirection: 'column', gap: '1.15rem' }}>
              <div>
                <label className="form-label" style={{ display: 'block', fontSize: '0.85rem', marginBottom: '0.4rem', color: 'var(--text-main)' }}>
                  Symptom Category *
                </label>
                <select
                  value={newCategory}
                  onChange={(e) => setNewCategory(e.target.value)}
                  className="form-select"
                  style={{ width: '100%', padding: '0.65rem 0.85rem', borderRadius: 8 }}
                  required
                >
                  <option value="MotorOverheating">Motor Overheating</option>
                  <option value="WheelJam">Wheel Jam</option>
                  <option value="SensorFault">Sensor Fault</option>
                  <option value="BatteryDepletion">Battery Rapid Depletion</option>
                  <option value="CameraMalfunction">Camera / Vision Malfunction</option>
                  <option value="CommunicationLoss">Communication / Telemetry Loss</option>
                  <option value="ChassisObstruction">Chassis Mechanical Obstruction</option>
                  <option value="SteeringActuatorJam">Steering Actuator Jam</option>
                  <option value="Other">Other / Unknown Malfunction</option>
                </select>
              </div>

              <div>
                <label className="form-label" style={{ display: 'block', fontSize: '0.85rem', marginBottom: '0.4rem', color: 'var(--text-main)' }}>
                  Diagnostic Error Code (Optional)
                </label>
                <input
                  type="text"
                  placeholder="e.g. ERR-MTR-901 or ERR-WHL-104"
                  value={newErrorCode}
                  onChange={(e) => setNewErrorCode(e.target.value)}
                  className="form-input"
                  style={{ width: '100%', padding: '0.65rem 0.85rem', borderRadius: 8 }}
                />
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                  Entering a recognized error code matches against the autonomous failure catalog.
                </span>
              </div>

              <div>
                <label className="form-label" style={{ display: 'block', fontSize: '0.85rem', marginBottom: '0.4rem', color: 'var(--text-main)' }}>
                  Incident Description *
                </label>
                <textarea
                  rows={3}
                  placeholder="Describe the failure, environmental conditions, or unusual telemetry..."
                  value={newDescription}
                  onChange={(e) => setNewDescription(e.target.value)}
                  className="form-textarea"
                  style={{ width: '100%', padding: '0.65rem 0.85rem', borderRadius: 8, resize: 'vertical' }}
                  required
                />
              </div>

              <div>
                <label className="form-label" style={{ display: 'block', fontSize: '0.85rem', marginBottom: '0.4rem', color: 'var(--text-main)' }}>
                  Photo Evidence (Optional)
                </label>
                <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
                  <label
                    className="btn btn-secondary"
                    style={{
                      cursor: 'pointer',
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '0.5rem',
                      fontSize: '0.85rem',
                      padding: '0.55rem 1rem',
                    }}
                  >
                    <Upload size={16} />
                    {newPhotoFile ? 'Change Photo' : 'Select Photo'}
                    <input
                      type="file"
                      accept="image/*"
                      onChange={handlePhotoSelect}
                      style={{ display: 'none' }}
                    />
                  </label>
                  {newPhotoFile && (
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: '200px' }}>
                      {newPhotoFile.name}
                    </span>
                  )}
                </div>

                {photoPreview && (
                  <div style={{ marginTop: '0.75rem', position: 'relative', display: 'inline-block' }}>
                    <img
                      src={photoPreview}
                      alt="Preview"
                      style={{ width: '100px', height: '100px', objectFit: 'cover', borderRadius: 8, border: '1px solid var(--border-color)' }}
                    />
                    <button
                      type="button"
                      onClick={() => {
                        setNewPhotoFile(null);
                        setPhotoPreview(null);
                      }}
                      style={{
                        position: 'absolute',
                        top: -6,
                        right: -6,
                        background: '#ef4444',
                        color: '#fff',
                        border: 'none',
                        borderRadius: '50%',
                        width: 20,
                        height: 20,
                        cursor: 'pointer',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                      }}
                    >
                      <X size={12} />
                    </button>
                  </div>
                )}
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '0.75rem' }}>
                <button
                  type="button"
                  onClick={() => setIsCreateModalOpen(false)}
                  disabled={isSubmittingReport}
                  className="btn btn-secondary"
                  style={{ fontSize: '0.85rem' }}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmittingReport}
                  className="btn btn-primary"
                  style={{ display: 'inline-flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.85rem' }}
                >
                  {isSubmittingReport ? (
                    <>
                      <div className="spinner" style={{ width: 14, height: 14 }} />
                      Submitting...
                    </>
                  ) : (
                    'Submit Report'
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Photo Lightbox Modal */}
      {previewPhotoUrl && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.85)',
            backdropFilter: 'blur(10px)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 110,
            padding: '2rem',
          }}
          onClick={() => setPreviewPhotoUrl(null)}
        >
          <div style={{ position: 'relative', maxWidth: '800px', maxHeight: '80vh' }} onClick={(e) => e.stopPropagation()}>
            <button
              onClick={() => setPreviewPhotoUrl(null)}
              style={{
                position: 'absolute',
                top: '-2.5rem',
                right: 0,
                background: 'transparent',
                border: 'none',
                color: '#fff',
                cursor: 'pointer',
              }}
            >
              <X size={24} />
            </button>
            <img
              src={previewPhotoUrl}
              alt="Breakdown evidence zoom"
              style={{ maxWidth: '100%', maxHeight: '80vh', borderRadius: 12, objectFit: 'contain', boxShadow: 'var(--shadow-lg)' }}
            />
          </div>
        </div>
      )}
    </div>
  );
};

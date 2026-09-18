import React, { useState, useEffect, useCallback } from 'react';
import { roversApi, Rover, RoverQueryParameters } from '../api/roversApi';
import { agentsApi, DispatchTelemetryAgentInput, DispatchTelemetryAgentOutput, WeatherAssessmentResult } from '../api/agentsApi';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { ErrorAlert } from '../components/Common/ErrorAlert';
import {
  Bot,
  Activity,
  Battery,
  BatteryCharging,
  BatteryWarning,
  CloudSun,
  CloudRain,
  ShieldCheck,
  RotateCcw,
  Sliders,
  CheckCircle2,
  XCircle,
  Play,
  ArrowRight,
  Filter,
} from 'lucide-react';

export const FleetTelemetry: React.FC = () => {
  // Rovers State
  const [rovers, setRovers] = useState<Rover[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [isLoadingRovers, setIsLoadingRovers] = useState<boolean>(true);
  const [roverError, setRoverError] = useState<string | null>(null);

  // Filters & Pagination
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [zoneFilter, setZoneFilter] = useState<string>('');
  const [sortBy, setSortBy] = useState<string>('identifier');
  const [isDescending, setIsDescending] = useState<boolean>(false);
  const [page, setPage] = useState<number>(1);
  const pageSize = 10;

  // Weather Assessment State
  const [weather, setWeather] = useState<WeatherAssessmentResult | null>(null);
  const [isLoadingWeather, setIsLoadingWeather] = useState<boolean>(false);

  // Agent Execution State
  const [agentInput, setAgentInput] = useState<DispatchTelemetryAgentInput>({
    dispatchRequestId: 'd3f1-892a',
    planSteps: [
      { stepNumber: 1, stepName: 'Locate available rover', status: 'Pending' },
      { stepNumber: 2, stepName: 'Verify battery sufficient', status: 'Pending' },
    ],
    sourceZone: 'WarehouseA-DockA1',
    destinationZone: 'WarehouseA-DockB3',
  });
  const [agentOutput, setAgentOutput] = useState<DispatchTelemetryAgentOutput | null>(null);
  const [isExecutingAgent, setIsExecutingAgent] = useState<boolean>(false);
  const [agentError, setAgentError] = useState<string | null>(null);

  // Override / Simulation Modal State
  const [selectedRoverForOverride, setSelectedRoverForOverride] = useState<Rover | null>(null);
  const [overrideStatus, setOverrideStatus] = useState<string>('Idle');
  const [overrideBattery, setOverrideBattery] = useState<number>(100);
  const [overrideZone, setOverrideZone] = useState<string>('WarehouseA-DockA1');
  const [isSavingOverride, setIsSavingOverride] = useState<boolean>(false);

  // Fetch Rovers
  const fetchRovers = useCallback(async () => {
    setIsLoadingRovers(true);
    setRoverError(null);
    try {
      const params: RoverQueryParameters = {
        status: statusFilter || undefined,
        zone: zoneFilter || undefined,
        sortBy,
        isDescending,
        page,
        pageSize,
      };
      const result = await roversApi.getRovers(params);
      setRovers(result.items);
      setTotalCount(result.totalCount);
    } catch (err: any) {
      setRoverError(err.response?.data?.message || 'Failed to fetch rovers.');
    } finally {
      setIsLoadingRovers(false);
    }
  }, [statusFilter, zoneFilter, sortBy, isDescending, page]);

  // Fetch Weather
  const fetchWeather = useCallback(async () => {
    setIsLoadingWeather(true);
    try {
      const result = await agentsApi.getCurrentWeather(agentInput.sourceZone, agentInput.destinationZone);
      setWeather(result);
    } catch (e) {
      console.warn('Weather fetch error:', e);
    } finally {
      setIsLoadingWeather(false);
    }
  }, [agentInput.sourceZone, agentInput.destinationZone]);

  useEffect(() => {
    fetchRovers();
  }, [fetchRovers]);

  useEffect(() => {
    fetchWeather();
  }, [fetchWeather]);

  // Execute Dispatch & Telemetry Agent
  const handleRunAgent = async () => {
    setIsExecutingAgent(true);
    setAgentError(null);
    setAgentOutput(null);

    try {
      const result = await agentsApi.executeDispatchTelemetryAgent(agentInput);
      setAgentOutput(result);
      // Refresh rovers table to reflect any locked rover state
      fetchRovers();
    } catch (err: any) {
      setAgentError(err.response?.data?.message || err.message || 'Agent execution failed.');
    } finally {
      setIsExecutingAgent(false);
    }
  };

  // Load locked mock input from docs/agent-contracts.md
  const handleLoadMockInput = async () => {
    try {
      const mock = await agentsApi.getMockInput();
      setAgentInput(mock);
    } catch {
      setAgentInput({
        dispatchRequestId: 'd3f1-892a',
        planSteps: [
          { stepNumber: 1, stepName: 'Locate available rover', status: 'Pending' },
          { stepNumber: 2, stepName: 'Verify battery sufficient', status: 'Pending' },
        ],
        sourceZone: 'WarehouseA-DockA1',
        destinationZone: 'WarehouseA-DockB3',
      });
    }
  };

  // Open Override Modal
  const openOverrideModal = (rover: Rover) => {
    setSelectedRoverForOverride(rover);
    setOverrideStatus(rover.status);
    setOverrideBattery(rover.batteryPercentage);
    setOverrideZone(rover.locationZone);
  };

  // Save Override
  const handleSaveOverride = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRoverForOverride) return;

    setIsSavingOverride(true);
    try {
      await roversApi.updateRoverSimulation(selectedRoverForOverride.id, {
        status: overrideStatus,
        batteryPercentage: overrideBattery,
        locationZone: overrideZone,
      });
      setSelectedRoverForOverride(null);
      fetchRovers();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to update rover simulation.');
    } finally {
      setIsSavingOverride(false);
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Idle':
        return <span className="badge badge-operator">Idle</span>;
      case 'Dispatched':
        return <span className="badge badge-supervisor">Dispatched</span>;
      case 'Charging':
        return <span className="badge badge-technician">Charging</span>;
      case 'Maintenance':
        return <span className="badge" style={{ background: 'rgba(139, 92, 246, 0.2)', color: '#c084fc' }}>Maintenance</span>;
      case 'Faulted':
        return <span className="badge" style={{ background: 'rgba(239, 68, 68, 0.2)', color: '#f87171' }}>Faulted</span>;
      default:
        return <span className="badge">{status}</span>;
    }
  };

  const getBatteryColor = (percent: number) => {
    if (percent >= 60) return '#10b981'; // Green
    if (percent >= 30) return '#f59e0b'; // Amber
    return '#ef4444'; // Red
  };

  const getWeatherRiskBadge = (risk: string) => {
    switch (risk.toLowerCase()) {
      case 'high':
        return (
          <span style={{ color: '#ef4444', fontWeight: 700, display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
            <CloudRain size={16} /> HIGH RISK (Heavy Rain/Storm)
          </span>
        );
      case 'medium':
        return (
          <span style={{ color: '#f59e0b', fontWeight: 700, display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
            <CloudSun size={16} /> MEDIUM RISK (Light Rain/Drizzle)
          </span>
        );
      default:
        return (
          <span style={{ color: '#10b981', fontWeight: 700, display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
            <CloudSun size={16} /> LOW RISK (Safe Transit)
          </span>
        );
    }
  };

  return (
    <div>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '2rem', flexWrap: 'wrap', gap: '1rem' }}>
        <div>
          <h1 style={{ fontSize: '2rem', marginBottom: '0.25rem' }}>Fleet Telemetry & Dispatch Agent</h1>
          <p style={{ color: 'var(--text-muted)' }}>
            Real-time rover status, battery monitoring, OpenWeather transit risk analysis, and candidate rover locking.
          </p>
        </div>

        {/* Live Weather Indicator */}
        <div className="glass-card" style={{ padding: '0.75rem 1.25rem', display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <CloudSun size={24} color="var(--primary-light)" />
            <div>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase' }}>Route Weather</div>
              <div style={{ fontSize: '0.9rem', fontWeight: 600 }}>
                {weather ? `${weather.temperatureCelsius.toFixed(1)}°C • ${weather.conditionDescription}` : 'Assessing...'}
              </div>
            </div>
          </div>
          {weather && (
            <div style={{ borderLeft: '1px solid var(--border-subtle)', paddingLeft: '1rem' }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase' }}>Transit Risk</div>
              <div style={{ fontSize: '0.85rem' }}>{getWeatherRiskBadge(weather.weatherRisk)}</div>
            </div>
          )}
        </div>
      </div>

      {/* Dispatch & Telemetry Agent Interactive Test Bench */}
      <div className="glass-card" style={{ marginBottom: '2rem', border: '1px solid var(--border-glow)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap', gap: '0.75rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <div className="sidebar-logo-icon" style={{ width: '36px', height: '36px' }}>
              <Activity size={20} />
            </div>
            <div>
              <h3 style={{ fontSize: '1.2rem' }}>Dispatch & Telemetry Agent (Hamdhan)</h3>
              <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                Locked Input/Output Contract from <code>docs/agent-contracts.md</code>
              </span>
            </div>
          </div>

          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button className="btn btn-secondary" onClick={handleLoadMockInput} style={{ fontSize: '0.85rem' }}>
              <RotateCcw size={14} /> Reset to Team Contract Input
            </button>
            <button
              className="btn btn-primary"
              onClick={handleRunAgent}
              disabled={isExecutingAgent}
              style={{ fontSize: '0.85rem' }}
            >
              {isExecutingAgent ? <LoadingSpinner size="sm" text="Executing Agent..." /> : <><Play size={14} /> Run Agent Pipeline</>}
            </button>
          </div>
        </div>

        {/* Two-column Input / Output viewer */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '1.25rem', marginTop: '1rem' }}>
          {/* Input Panel */}
          <div style={{ background: 'rgba(15, 23, 42, 0.7)', padding: '1rem', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-subtle)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem' }}>
              <span style={{ fontSize: '0.8rem', fontWeight: 700, color: 'var(--primary-light)', textTransform: 'uppercase' }}>
                Handoff Input (from Mission Planner)
              </span>
              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Chathumini &rarr; Hamdhan</span>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem', marginBottom: '0.75rem' }}>
              <div>
                <label className="form-label" style={{ fontSize: '0.75rem' }}>Dispatch Request ID</label>
                <input
                  type="text"
                  className="form-input"
                  style={{ fontSize: '0.85rem', padding: '0.4rem 0.6rem' }}
                  value={agentInput.dispatchRequestId}
                  onChange={(e) => setAgentInput({ ...agentInput, dispatchRequestId: e.target.value })}
                />
              </div>
              <div>
                <label className="form-label" style={{ fontSize: '0.75rem' }}>Source Zone</label>
                <input
                  type="text"
                  className="form-input"
                  style={{ fontSize: '0.85rem', padding: '0.4rem 0.6rem' }}
                  value={agentInput.sourceZone}
                  onChange={(e) => setAgentInput({ ...agentInput, sourceZone: e.target.value })}
                />
              </div>
            </div>

            <div style={{ marginBottom: '0.75rem' }}>
              <label className="form-label" style={{ fontSize: '0.75rem' }}>Destination Zone</label>
              <input
                type="text"
                className="form-input"
                style={{ fontSize: '0.85rem', padding: '0.4rem 0.6rem' }}
                value={agentInput.destinationZone}
                onChange={(e) => setAgentInput({ ...agentInput, destinationZone: e.target.value })}
              />
            </div>

            <div style={{ fontSize: '0.75rem', color: 'var(--text-faint)', fontStyle: 'italic' }}>
              Plan Steps: {agentInput.planSteps.length} steps (e.g. &ldquo;Locate available rover&rdquo;, &ldquo;Verify battery&rdquo;)
            </div>
          </div>

          {/* Output Panel */}
          <div style={{ background: 'rgba(15, 23, 42, 0.7)', padding: '1rem', borderRadius: 'var(--radius-sm)', border: '1px solid var(--border-subtle)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem' }}>
              <span style={{ fontSize: '0.8rem', fontWeight: 700, color: 'var(--success)', textTransform: 'uppercase' }}>
                Handoff Output (to Safety Guard)
              </span>
              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Hamdhan &rarr; Dilukshi</span>
            </div>

            {agentError && <ErrorAlert message={agentError} onDismiss={() => setAgentError(null)} />}

            {agentOutput ? (
              <div>
                {/* Visual Output Badges */}
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(130px, 1fr))', gap: '0.5rem', marginBottom: '0.75rem' }}>
                  <div style={{ background: 'rgba(255,255,255,0.04)', padding: '0.5rem', borderRadius: '6px' }}>
                    <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>SELECTED ROVER</div>
                    <div style={{ fontSize: '1rem', fontWeight: 700, color: agentOutput.selectedRoverId ? 'var(--primary-light)' : 'var(--text-faint)' }}>
                      {agentOutput.selectedRoverId || 'None'}
                    </div>
                  </div>

                  <div style={{ background: 'rgba(255,255,255,0.04)', padding: '0.5rem', borderRadius: '6px' }}>
                    <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>BATTERY OK</div>
                    <div style={{ fontSize: '0.9rem', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '4px', color: agentOutput.batteryOk ? 'var(--success)' : 'var(--danger)' }}>
                      {agentOutput.batteryOk ? <CheckCircle2 size={16} /> : <XCircle size={16} />}
                      {agentOutput.batteryOk ? 'Pass (>=40%)' : 'Fail (<40%)'}
                    </div>
                  </div>

                  <div style={{ background: 'rgba(255,255,255,0.04)', padding: '0.5rem', borderRadius: '6px' }}>
                    <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>WEATHER RISK</div>
                    <div style={{ fontSize: '0.85rem', fontWeight: 700, textTransform: 'uppercase' }}>
                      {getWeatherRiskBadge(agentOutput.weatherRisk)}
                    </div>
                  </div>

                  <div style={{ background: 'rgba(255,255,255,0.04)', padding: '0.5rem', borderRadius: '6px' }}>
                    <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>TRANSACTION LOCKED</div>
                    <div style={{ fontSize: '0.9rem', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '4px', color: agentOutput.locked ? 'var(--success)' : 'var(--danger)' }}>
                      {agentOutput.locked ? <ShieldCheck size={16} /> : <XCircle size={16} />}
                      {agentOutput.locked ? 'Locked (Dispatched)' : 'Not Locked'}
                    </div>
                  </div>
                </div>

                {agentOutput.reason && (
                  <div style={{ fontSize: '0.8rem', color: '#fca5a5', background: 'rgba(239, 68, 68, 0.1)', padding: '0.5rem 0.75rem', borderRadius: '6px', marginBottom: '0.75rem' }}>
                    <strong>Reason:</strong> {agentOutput.reason}
                  </div>
                )}

                {/* Raw JSON Accordion */}
                <details style={{ fontSize: '0.75rem', color: 'var(--text-muted)', cursor: 'pointer' }}>
                  <summary style={{ fontWeight: 600 }}>Inspect Contract JSON Output</summary>
                  <pre style={{ background: '#020617', padding: '0.75rem', borderRadius: '6px', marginTop: '0.5rem', overflowX: 'auto' }}>
                    {JSON.stringify(agentOutput, null, 2)}
                  </pre>
                </details>
              </div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '140px', color: 'var(--text-muted)', textAlign: 'center' }}>
                <Activity size={24} style={{ opacity: 0.4, marginBottom: '0.5rem' }} />
                <span style={{ fontSize: '0.85rem' }}>Click &ldquo;Run Agent Pipeline&rdquo; to execute the agent.</span>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Rovers Fleet Table */}
      <div className="glass-card">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem', flexWrap: 'wrap', gap: '1rem' }}>
          <div>
            <h2 style={{ fontSize: '1.3rem' }}>Warehouse Rovers Fleet ({totalCount})</h2>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>
              Live status from database. Click &ldquo;Simulate / Override&rdquo; to alter battery levels or state for testing.
            </p>
          </div>

          {/* Filter Bar */}
          <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <Filter size={16} color="var(--text-muted)" />
              <select
                className="form-input"
                style={{ padding: '0.4rem 0.75rem', fontSize: '0.85rem', width: '130px' }}
                value={statusFilter}
                onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
              >
                <option value="">All Statuses</option>
                <option value="Idle">Idle</option>
                <option value="Dispatched">Dispatched</option>
                <option value="Charging">Charging</option>
                <option value="Maintenance">Maintenance</option>
                <option value="Faulted">Faulted</option>
              </select>
            </div>

            <input
              type="text"
              className="form-input"
              style={{ padding: '0.4rem 0.75rem', fontSize: '0.85rem', width: '160px' }}
              placeholder="Filter by zone..."
              value={zoneFilter}
              onChange={(e) => { setZoneFilter(e.target.value); setPage(1); }}
            />

            <select
              className="form-input"
              style={{ padding: '0.4rem 0.75rem', fontSize: '0.85rem', width: '140px' }}
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value)}
            >
              <option value="identifier">Sort: Rover Code</option>
              <option value="battery">Sort: Battery Level</option>
              <option value="status">Sort: Status</option>
              <option value="zone">Sort: Zone</option>
            </select>

            <button
              className="btn btn-secondary"
              style={{ padding: '0.4rem 0.75rem', fontSize: '0.85rem' }}
              onClick={() => setIsDescending(!isDescending)}
              title="Toggle Sort Direction"
            >
              {isDescending ? '↓ Desc' : '↑ Asc'}
            </button>

            <button
              className="btn btn-secondary"
              style={{ padding: '0.4rem 0.75rem', fontSize: '0.85rem' }}
              onClick={fetchRovers}
              title="Refresh rovers table"
            >
              <RotateCcw size={14} />
            </button>
          </div>
        </div>

        {roverError && <ErrorAlert message={roverError} onDismiss={() => setRoverError(null)} />}

        {isLoadingRovers ? (
          <div style={{ padding: '3rem', textAlign: 'center' }}>
            <LoadingSpinner text="Loading fleet rovers..." />
          </div>
        ) : rovers.length === 0 ? (
          <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-muted)' }}>
            No rovers found matching the specified filters.
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-muted)', fontSize: '0.75rem', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                  <th style={{ padding: '0.75rem 1rem' }}>Rover Identifier</th>
                  <th style={{ padding: '0.75rem 1rem' }}>Operating Status</th>
                  <th style={{ padding: '0.75rem 1rem' }}>Battery Level</th>
                  <th style={{ padding: '0.75rem 1rem' }}>Current Location</th>
                  <th style={{ padding: '0.75rem 1rem' }}>Assigned Mission</th>
                  <th style={{ padding: '0.75rem 1rem', textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {rovers.map((rover) => (
                  <tr
                    key={rover.id}
                    style={{
                      borderBottom: '1px solid var(--border-subtle)',
                      transition: 'background var(--transition-fast)',
                    }}
                  >
                    <td style={{ padding: '1rem', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <Bot size={18} color="var(--primary-light)" />
                      <span>{rover.identifier}</span>
                    </td>
                    <td style={{ padding: '1rem' }}>{getStatusBadge(rover.status)}</td>
                    <td style={{ padding: '1rem' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                        {rover.status === 'Charging' ? (
                          <BatteryCharging size={18} color="#f59e0b" />
                        ) : rover.batteryPercentage < 30 ? (
                          <BatteryWarning size={18} color="#ef4444" />
                        ) : (
                          <Battery size={18} color={getBatteryColor(rover.batteryPercentage)} />
                        )}
                        <div style={{ width: '90px', background: 'rgba(255,255,255,0.1)', height: '8px', borderRadius: '4px', overflow: 'hidden' }}>
                          <div
                            style={{
                              width: `${rover.batteryPercentage}%`,
                              background: getBatteryColor(rover.batteryPercentage),
                              height: '100%',
                              borderRadius: '4px',
                            }}
                          />
                        </div>
                        <span style={{ fontSize: '0.8rem', fontWeight: 600 }}>{rover.batteryPercentage}%</span>
                      </div>
                    </td>
                    <td style={{ padding: '1rem', color: 'var(--text-muted)' }}>{rover.locationZone}</td>
                    <td style={{ padding: '1rem' }}>
                      {rover.currentMissionId ? (
                        <span style={{ fontFamily: 'monospace', fontSize: '0.8rem', background: 'rgba(56, 189, 248, 0.1)', padding: '2px 6px', borderRadius: '4px', color: 'var(--primary-light)' }}>
                          {rover.currentMissionId}
                        </span>
                      ) : (
                        <span style={{ color: 'var(--text-faint)', fontSize: '0.85rem' }}>Idle (None)</span>
                      )}
                    </td>
                    <td style={{ padding: '1rem', textAlign: 'right' }}>
                      <button
                        className="btn btn-secondary"
                        style={{ padding: '0.35rem 0.65rem', fontSize: '0.8rem' }}
                        onClick={() => openOverrideModal(rover)}
                        title="Manually override battery/status for testing"
                      >
                        <Sliders size={14} /> Simulate
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Simulation Override Modal */}
      {selectedRoverForOverride && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0,0,0,0.7)',
            backdropFilter: 'blur(4px)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 100,
            padding: '1rem',
          }}
        >
          <div className="glass-card" style={{ maxWidth: '440px', width: '100%', border: '1px solid var(--border-glow)' }}>
            <h3 style={{ fontSize: '1.25rem', marginBottom: '0.25rem' }}>
              Simulate Rover: {selectedRoverForOverride.identifier}
            </h3>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', marginBottom: '1.25rem' }}>
              Modify rover telemetry parameters to test agent edge cases (e.g. low battery rejection or state recovery).
            </p>

            <form onSubmit={handleSaveOverride}>
              <div className="form-group">
                <label className="form-label">Operating Status</label>
                <select
                  className="form-input"
                  value={overrideStatus}
                  onChange={(e) => setOverrideStatus(e.target.value)}
                >
                  <option value="Idle">Idle</option>
                  <option value="Dispatched">Dispatched</option>
                  <option value="Charging">Charging</option>
                  <option value="Maintenance">Maintenance</option>
                  <option value="Faulted">Faulted</option>
                </select>
              </div>

              <div className="form-group">
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <label className="form-label">Battery Level: {overrideBattery}%</label>
                  <span style={{ fontSize: '0.75rem', color: overrideBattery < 40 ? 'var(--danger)' : 'var(--success)' }}>
                    {overrideBattery < 40 ? 'Below Safe Threshold (40%)' : 'Safe Operating Level'}
                  </span>
                </div>
                <input
                  type="range"
                  min="0"
                  max="100"
                  value={overrideBattery}
                  onChange={(e) => setOverrideBattery(parseInt(e.target.value, 10))}
                  style={{ width: '100%', cursor: 'pointer', accentColor: getBatteryColor(overrideBattery) }}
                />
              </div>

              <div className="form-group">
                <label className="form-label">Location Zone</label>
                <input
                  type="text"
                  className="form-input"
                  value={overrideZone}
                  onChange={(e) => setOverrideZone(e.target.value)}
                  placeholder="e.g. WarehouseA-DockA1"
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setSelectedRoverForOverride(null)}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={isSavingOverride}
                >
                  {isSavingOverride ? <LoadingSpinner size="sm" text="Saving..." /> : 'Apply Simulation'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

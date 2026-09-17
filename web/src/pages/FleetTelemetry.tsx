import React from 'react';
import { EmptyState } from '../components/Common/EmptyState';
import { Activity } from 'lucide-react';

export const FleetTelemetry: React.FC = () => {
  return (
    <div>
      <div style={{ marginBottom: '1.5rem' }}>
        <h1 style={{ fontSize: '1.75rem', marginBottom: '0.25rem' }}>Fleet Telemetry</h1>
        <p style={{ color: 'var(--text-muted)' }}>
          Real-time location, speed, battery health, and sensor feeds from simulated warehouse rovers.
        </p>
      </div>

      <EmptyState
        icon={<Activity size={32} color="#38bdf8" />}
        title="Telemetry Pipeline Idle"
        description="Live sensor telemetry streaming from simulated rovers (Ro-1 to Ro-10) will render on the interactive warehouse grid map during Phase 2."
      />
    </div>
  );
};

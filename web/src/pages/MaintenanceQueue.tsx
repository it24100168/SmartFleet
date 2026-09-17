import React from 'react';
import { EmptyState } from '../components/Common/EmptyState';
import { Wrench } from 'lucide-react';

export const MaintenanceQueue: React.FC = () => {
  return (
    <div>
      <div style={{ marginBottom: '1.5rem' }}>
        <h1 style={{ fontSize: '1.75rem', marginBottom: '0.25rem' }}>Maintenance Queue</h1>
        <p style={{ color: 'var(--text-muted)' }}>
          Diagnostic repair queues and breakdown tickets assigned to Maintenance Technicians.
        </p>
      </div>

      <EmptyState
        icon={<Wrench size={32} color="#fbbf24" />}
        title="Maintenance Queue Clear"
        description="No breakdown incidents reported. The Maintenance Mechanic Agent will log automated diagnostics and schedule tickets here when issues occur."
      />
    </div>
  );
};

import React from 'react';
import { EmptyState } from '../components/Common/EmptyState';
import { ShieldCheck } from 'lucide-react';

export const ApprovalCenter: React.FC = () => {
  return (
    <div>
      <div style={{ marginBottom: '1.5rem' }}>
        <h1 style={{ fontSize: '1.75rem', marginBottom: '0.25rem' }}>Approval Center</h1>
        <p style={{ color: 'var(--text-muted)' }}>
          Fleet Supervisor manual authorization portal for AI Safety Guard flagged events.
        </p>
      </div>

      <EmptyState
        icon={<ShieldCheck size={32} color="var(--primary-light)" />}
        title="Zero Pending Safety Approvals"
        description="When the Safety Guard Agent flags speed violations, proximity warnings, or unusual rover trajectories, manual override controls will appear here."
      />
    </div>
  );
};

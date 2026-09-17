import React from 'react';
import { AlertCircle } from 'lucide-react';

interface ErrorAlertProps {
  message: string;
  details?: string;
  onDismiss?: () => void;
}

export const ErrorAlert: React.FC<ErrorAlertProps> = ({ message, details, onDismiss }) => {
  return (
    <div className="error-alert" role="alert">
      <AlertCircle size={20} style={{ flexShrink: 0, marginTop: '2px', color: '#ef4444' }} />
      <div style={{ flex: 1 }}>
        <div style={{ fontWeight: 600, fontSize: '0.925rem' }}>{message}</div>
        {details && (
          <div style={{ fontSize: '0.825rem', marginTop: '0.25rem', opacity: 0.85, wordBreak: 'break-word' }}>
            {details}
          </div>
        )}
      </div>
      {onDismiss && (
        <button
          onClick={onDismiss}
          style={{
            background: 'none',
            border: 'none',
            color: '#fca5a5',
            cursor: 'pointer',
            fontSize: '1.2rem',
            lineHeight: 1,
          }}
          aria-label="Dismiss alert"
        >
          &times;
        </button>
      )}
    </div>
  );
};

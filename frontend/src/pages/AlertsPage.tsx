import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { alertService } from '../services/marketService';
import { Bell, Plus, Trash2, TrendingUp, TrendingDown, CheckCircle, AlertTriangle } from 'lucide-react';
import type { Alert, CreateAlertRequest } from '../types';
import { useAuthStore } from '../store';
import { useNavigate } from 'react-router-dom';

export default function AlertsPage() {
  const { isAuthenticated } = useAuthStore();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [form, setForm] = useState<CreateAlertRequest>({
    symbol: 'KC1',
    condition: 'above',
    threshold: 200,
    note: '',
  });
  const [showForm, setShowForm] = useState(false);

  const { data: alerts = [], isLoading } = useQuery<Alert[]>({
    queryKey: ['alerts'],
    queryFn: alertService.getAlerts,
    enabled: isAuthenticated,
  });

  const createMutation = useMutation({
    mutationFn: alertService.createAlert,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['alerts'] });
      setShowForm(false);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: alertService.deleteAlert,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['alerts'] }),
  });

  if (!isAuthenticated) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: 400, gap: 16 }}>
        <Bell size={48} style={{ color: 'var(--text-muted)' }} />
        <h2 style={{ color: 'var(--text-primary)', margin: 0 }}>Sign in to manage alerts</h2>
        <p style={{ color: 'var(--text-muted)', margin: 0 }}>Create price alerts to get notified when KC1 hits your target</p>
        <button className="btn-primary" onClick={() => navigate('/login')}>Sign In</button>
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24, maxWidth: 800 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontSize: 24, fontWeight: 800, color: 'var(--text-primary)', margin: 0 }}>Price Alerts</h1>
          <p style={{ fontSize: 13, color: 'var(--text-muted)', margin: '4px 0 0' }}>
            Get notified when KC1 Coffee hits your price targets
          </p>
        </div>
        <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
          <Plus size={14} />
          New Alert
        </button>
      </div>

      {/* Create Alert Form */}
      {showForm && (
        <div className="glass-card" style={{ padding: '24px', borderColor: 'var(--border-active)' }}>
          <h3 style={{ margin: '0 0 20px', fontSize: 16, fontWeight: 700, color: 'var(--text-primary)' }}>
            Create Price Alert
          </h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 16 }}>
            <div>
              <label style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 600, display: 'block', marginBottom: 6 }}>Symbol</label>
              <input className="input-field" value={form.symbol} onChange={e => setForm(p => ({ ...p, symbol: e.target.value.toUpperCase() }))} />
            </div>
            <div>
              <label style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 600, display: 'block', marginBottom: 6 }}>Condition</label>
              <div style={{ display: 'flex', gap: 8 }}>
                {(['above', 'below'] as const).map(c => (
                  <button
                    key={c}
                    className={`btn-ghost${form.condition === c ? ' active' : ''}`}
                    style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 4 }}
                    onClick={() => setForm(p => ({ ...p, condition: c }))}
                  >
                    {c === 'above' ? <TrendingUp size={13} /> : <TrendingDown size={13} />}
                    {c.charAt(0).toUpperCase() + c.slice(1)}
                  </button>
                ))}
              </div>
            </div>
            <div>
              <label style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 600, display: 'block', marginBottom: 6 }}>
                Price Threshold ($)
              </label>
              <input
                className="input-field"
                type="number"
                step="0.01"
                value={form.threshold}
                onChange={e => setForm(p => ({ ...p, threshold: parseFloat(e.target.value) }))}
              />
            </div>
            <div>
              <label style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 600, display: 'block', marginBottom: 6 }}>Note (optional)</label>
              <input className="input-field" value={form.note} onChange={e => setForm(p => ({ ...p, note: e.target.value }))} placeholder="e.g. Take profit" />
            </div>
          </div>
          <div style={{ display: 'flex', gap: 8, marginTop: 20 }}>
            <button
              className="btn-primary"
              onClick={() => createMutation.mutate(form)}
              disabled={createMutation.isPending}
            >
              {createMutation.isPending ? 'Creating...' : 'Create Alert'}
            </button>
            <button className="btn-ghost" onClick={() => setShowForm(false)}>Cancel</button>
          </div>
        </div>
      )}

      {/* Alert List */}
      {isLoading ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          {[1, 2, 3].map(i => <div key={i} className="skeleton" style={{ height: 80, borderRadius: 12 }} />)}
        </div>
      ) : alerts.length === 0 ? (
        <div className="glass-card" style={{ padding: '48px', textAlign: 'center' }}>
          <Bell size={40} style={{ color: 'var(--text-muted)', marginBottom: 16 }} />
          <p style={{ color: 'var(--text-muted)', margin: 0 }}>No alerts set. Create your first price alert above.</p>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          {alerts.map(alert => (
            <div key={alert.id} className="glass-card" style={{
              padding: '16px 20px',
              display: 'flex',
              alignItems: 'center',
              gap: 16,
              borderColor: alert.status === 'triggered' ? 'var(--accent-green)' : undefined,
            }}>
              <div style={{
                width: 40, height: 40,
                borderRadius: 10,
                background: alert.condition === 'above' ? 'rgba(16,185,129,0.1)' : 'rgba(239,68,68,0.1)',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                flexShrink: 0,
              }}>
                {alert.condition === 'above'
                  ? <TrendingUp size={18} style={{ color: 'var(--accent-green)' }} />
                  : <TrendingDown size={18} style={{ color: 'var(--accent-red)' }} />
                }
              </div>

              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
                  <span style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{alert.symbol}</span>
                  <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>
                    Price {alert.condition} ${alert.threshold.toFixed(2)}
                  </span>
                  <span className={alert.status === 'triggered' ? 'badge-bullish' : alert.status === 'active' ? 'badge-neutral' : 'badge-bearish'}>
                    {alert.status}
                  </span>
                </div>
                {alert.note && <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{alert.note}</div>}
                {alert.triggeredAt && (
                  <div style={{ fontSize: 11, color: 'var(--accent-green)', marginTop: 2 }}>
                    Triggered at ${alert.triggeredPrice?.toFixed(2)} on {new Date(alert.triggeredAt).toLocaleString()}
                  </div>
                )}
              </div>

              <button
                className="btn-ghost"
                style={{ color: 'var(--accent-red)', padding: '6px 10px' }}
                onClick={() => deleteMutation.mutate(alert.id)}
                disabled={deleteMutation.isPending}
              >
                <Trash2 size={14} />
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

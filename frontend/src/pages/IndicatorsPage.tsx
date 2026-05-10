import { useIndicators } from '../hooks/useMarket';
import { useState } from 'react';
import type { Timeframe } from '../types';
import { TIMEFRAMES } from '../types';
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, ReferenceLine, BarChart, Bar
} from 'recharts';

export default function IndicatorsPage() {
  const [timeframe, setTimeframe] = useState<Timeframe>('1day');
  const { data: indicators, isLoading } = useIndicators('KC1', timeframe);

  const rsiData = indicators?.rsi?.slice(-60).map(v => ({
    date: new Date(v.timestamp).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
    rsi: v.value,
  })) ?? [];

  const macdData = indicators?.macd?.slice(-60).map(v => ({
    date: new Date(v.timestamp).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
    macd: v.macd,
    signal: v.signal,
    histogram: v.histogram,
  })) ?? [];

  const latestRsi = indicators?.rsi?.[indicators.rsi.length - 1]?.value;
  const rsiSignal = latestRsi != null
    ? latestRsi > 70 ? { label: 'Overbought', color: 'var(--accent-red)' }
      : latestRsi < 30 ? { label: 'Oversold', color: 'var(--accent-green)' }
      : { label: 'Neutral', color: 'var(--accent-amber)' }
    : null;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontSize: 24, fontWeight: 800, color: 'var(--text-primary)', margin: 0 }}>Technical Indicators</h1>
          <p style={{ fontSize: 13, color: 'var(--text-muted)', margin: '4px 0 0' }}>RSI · MACD · Bollinger Bands · SMA · EMA</p>
        </div>
        <div style={{ display: 'flex', gap: 2, background: 'var(--bg-secondary)', borderRadius: 8, padding: 4 }}>
          {TIMEFRAMES.filter(t => ['1day', '1h', '4h', '1week'].includes(t.value)).map(tf => (
            <button key={tf.value} className={`timeframe-btn${timeframe === tf.value ? ' active' : ''}`} onClick={() => setTimeframe(tf.value)}>
              {tf.label}
            </button>
          ))}
        </div>
      </div>

      {isLoading ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {[1, 2].map(i => <div key={i} className="skeleton" style={{ height: 280, borderRadius: 12 }} />)}
        </div>
      ) : (
        <>
          {/* RSI Panel */}
          <div className="glass-card" style={{ padding: '20px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
              <div>
                <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700, color: 'var(--text-primary)' }}>RSI (14)</h3>
                <p style={{ margin: '2px 0 0', fontSize: 12, color: 'var(--text-muted)' }}>Relative Strength Index</p>
              </div>
              {rsiSignal && (
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <span style={{ fontSize: 24, fontWeight: 800, color: rsiSignal.color, fontVariantNumeric: 'tabular-nums' }}>
                    {latestRsi?.toFixed(1)}
                  </span>
                  <span className={rsiSignal.label === 'Overbought' ? 'badge-bearish' : rsiSignal.label === 'Oversold' ? 'badge-bullish' : 'badge-neutral'}>
                    {rsiSignal.label}
                  </span>
                </div>
              )}
            </div>
            <ResponsiveContainer width="100%" height={220}>
              <LineChart data={rsiData}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                <XAxis dataKey="date" tick={{ fill: 'var(--text-muted)', fontSize: 10 }} />
                <YAxis domain={[0, 100]} tick={{ fill: 'var(--text-muted)', fontSize: 10 }} />
                <Tooltip contentStyle={{ background: 'var(--bg-card)', border: '1px solid var(--border)', borderRadius: 8, color: 'var(--text-primary)' }} />
                <ReferenceLine y={70} stroke="rgba(239,68,68,0.5)" strokeDasharray="4 4" label={{ value: 'Overbought', fill: 'var(--accent-red)', fontSize: 10 }} />
                <ReferenceLine y={30} stroke="rgba(16,185,129,0.5)" strokeDasharray="4 4" label={{ value: 'Oversold', fill: 'var(--accent-green)', fontSize: 10 }} />
                <Line type="monotone" dataKey="rsi" stroke="#3b82f6" strokeWidth={2} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>

          {/* MACD Panel */}
          <div className="glass-card" style={{ padding: '20px' }}>
            <div style={{ marginBottom: 16 }}>
              <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700, color: 'var(--text-primary)' }}>MACD (12, 26, 9)</h3>
              <p style={{ margin: '2px 0 0', fontSize: 12, color: 'var(--text-muted)' }}>Moving Average Convergence Divergence</p>
            </div>
            <ResponsiveContainer width="100%" height={220}>
              <BarChart data={macdData}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                <XAxis dataKey="date" tick={{ fill: 'var(--text-muted)', fontSize: 10 }} />
                <YAxis tick={{ fill: 'var(--text-muted)', fontSize: 10 }} />
                <Tooltip contentStyle={{ background: 'var(--bg-card)', border: '1px solid var(--border)', borderRadius: 8, color: 'var(--text-primary)' }} />
                <ReferenceLine y={0} stroke="var(--border-active)" />
                <Bar dataKey="histogram" fill="rgba(59,130,246,0.4)" radius={[2, 2, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </>
      )}
    </div>
  );
}

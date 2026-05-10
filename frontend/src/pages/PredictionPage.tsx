import { usePrediction } from '../hooks/useMarket';
import { useState } from 'react';
import { TrendingUp, TrendingDown, Minus, Cpu, Calendar, Target } from 'lucide-react';
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, ReferenceLine
} from 'recharts';

const METHODS = [
  { value: 'linear', label: 'Linear Regression', desc: 'Fast & reliable for short trends' },
  { value: 'prophet', label: 'Prophet', desc: 'Best for seasonal patterns' },
];

export default function PredictionPage() {
  const [method, setMethod] = useState('linear');
  const [horizon, setHorizon] = useState(7);
  const { data: prediction, isLoading, refetch } = usePrediction('KC1', method, horizon);

  const trendColor = prediction?.trend === 'Bullish'
    ? 'var(--accent-green)'
    : prediction?.trend === 'Bearish'
    ? 'var(--accent-red)'
    : 'var(--accent-amber)';

  const TrendIcon = prediction?.trend === 'Bullish'
    ? TrendingUp
    : prediction?.trend === 'Bearish'
    ? TrendingDown
    : Minus;

  const confidencePct = ((prediction?.confidence ?? 0) * 100).toFixed(1);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
      <div>
        <h1 style={{ fontSize: 24, fontWeight: 800, color: 'var(--text-primary)', margin: 0 }}>
          AI Price Prediction
        </h1>
        <p style={{ fontSize: 13, color: 'var(--text-muted)', margin: '4px 0 0' }}>
          Machine learning forecast for KC1 Coffee Futures
        </p>
      </div>

      {/* Controls */}
      <div className="glass-card" style={{ padding: '20px', display: 'flex', gap: 24, flexWrap: 'wrap', alignItems: 'flex-end' }}>
        <div style={{ flex: 1, minWidth: 200 }}>
          <label style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 600, display: 'block', marginBottom: 8, textTransform: 'uppercase' }}>
            Model
          </label>
          <div style={{ display: 'flex', gap: 8 }}>
            {METHODS.map(m => (
              <button
                key={m.value}
                onClick={() => setMethod(m.value)}
                className={`btn-ghost${method === m.value ? ' active' : ''}`}
                style={{ flex: 1, flexDirection: 'column', gap: 2, height: 'auto', padding: '10px' }}
              >
                <div style={{ fontWeight: 700, fontSize: 13 }}>{m.label}</div>
                <div style={{ fontSize: 10, opacity: 0.7 }}>{m.desc}</div>
              </button>
            ))}
          </div>
        </div>

        <div>
          <label style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 600, display: 'block', marginBottom: 8, textTransform: 'uppercase' }}>
            Horizon (days)
          </label>
          <div style={{ display: 'flex', gap: 6 }}>
            {[3, 7, 14, 30].map(d => (
              <button
                key={d}
                className={`btn-ghost${horizon === d ? ' active' : ''}`}
                onClick={() => setHorizon(d)}
                style={{ minWidth: 40 }}
              >
                {d}d
              </button>
            ))}
          </div>
        </div>

        <button
          className="btn-primary"
          onClick={() => refetch()}
          disabled={isLoading}
          style={{ display: 'flex', alignItems: 'center', gap: 6 }}
        >
          <Cpu size={14} />
          {isLoading ? 'Predicting...' : 'Predict'}
        </button>
      </div>

      {/* Result Cards */}
      {isLoading ? (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 16 }}>
          {[1, 2, 3].map(i => <div key={i} className="skeleton glass-card" style={{ height: 120 }} />)}
        </div>
      ) : prediction ? (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 16 }}>
            {/* Predicted Price */}
            <div className="glass-card" style={{ padding: '20px', borderColor: 'var(--border-active)', boxShadow: 'var(--glow-blue)' }}>
              <div style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 600, textTransform: 'uppercase', marginBottom: 8 }}>
                Predicted Price
              </div>
              <div style={{ fontSize: 32, fontWeight: 800, color: 'var(--text-primary)', fontVariantNumeric: 'tabular-nums' }}>
                ${prediction.predictedPrice?.toFixed(2)}
              </div>
              <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>
                in {horizon} days
              </div>
            </div>

            {/* Trend */}
            <div className="glass-card" style={{ padding: '20px', borderColor: trendColor, boxShadow: `0 0 20px ${trendColor}33` }}>
              <div style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 600, textTransform: 'uppercase', marginBottom: 8 }}>
                Trend Signal
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 24, fontWeight: 800, color: trendColor }}>
                <TrendIcon size={28} />
                {prediction.trend}
              </div>
              <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>
                Market direction
              </div>
            </div>

            {/* Confidence */}
            <div className="glass-card" style={{ padding: '20px' }}>
              <div style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 600, textTransform: 'uppercase', marginBottom: 8 }}>
                Confidence
              </div>
              <div style={{ fontSize: 32, fontWeight: 800, color: 'var(--accent-blue)' }}>
                {confidencePct}%
              </div>
              <div style={{ marginTop: 8, height: 4, borderRadius: 2, background: 'var(--bg-hover)' }}>
                <div style={{
                  height: '100%',
                  borderRadius: 2,
                  width: `${confidencePct}%`,
                  background: `linear-gradient(90deg, var(--accent-blue), var(--accent-purple))`,
                  transition: 'width 1s ease',
                }} />
              </div>
            </div>
          </div>

          {/* Forecast Chart */}
          {prediction.forecast && prediction.forecast.length > 0 && (
            <div className="glass-card" style={{ padding: '20px' }}>
              <div style={{ fontSize: 14, fontWeight: 700, color: 'var(--text-secondary)', marginBottom: 16, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                Price Forecast
              </div>
              <ResponsiveContainer width="100%" height={280}>
                <AreaChart data={prediction.forecast}>
                  <defs>
                    <linearGradient id="forecastGradient" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.3} />
                      <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                  <XAxis dataKey="date" tick={{ fill: 'var(--text-muted)', fontSize: 11 }} />
                  <YAxis domain={['auto', 'auto']} tick={{ fill: 'var(--text-muted)', fontSize: 11 }} />
                  <Tooltip
                    contentStyle={{ background: 'var(--bg-card)', border: '1px solid var(--border)', borderRadius: 8, color: 'var(--text-primary)' }}
                    formatter={(v: number) => [`$${v.toFixed(2)}`, 'Price']}
                  />
                  <Area type="monotone" dataKey="price" stroke="#3b82f6" strokeWidth={2} fill="url(#forecastGradient)" dot={false} />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          )}

          <div style={{ fontSize: 11, color: 'var(--text-muted)', textAlign: 'center' }}>
            ⚠️ Predictions are for informational purposes only and do not constitute financial advice.
          </div>
        </>
      ) : null}
    </div>
  );
}

import CandlestickChart from '../components/charts/CandlestickChart';
import { useQuote } from '../hooks/useMarket';

export default function ChartPage() {
  const { data: quote } = useQuote();

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
      <div>
        <h1 style={{ fontSize: 24, fontWeight: 800, color: 'var(--text-primary)', margin: 0 }}>
          Candlestick Chart
        </h1>
        <p style={{ fontSize: 13, color: 'var(--text-muted)', margin: '4px 0 0' }}>
          OHLCV analysis with technical indicators overlay
        </p>
      </div>

      <CandlestickChart symbol="KC1" />

      {/* Quick Stats under chart */}
      {quote && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 12 }}>
          {[
            { label: 'Open', value: `$${quote.open?.toFixed(2)}` },
            { label: 'High', value: `$${quote.high?.toFixed(2)}`, color: 'var(--accent-green)' },
            { label: 'Low', value: `$${quote.low?.toFixed(2)}`, color: 'var(--accent-red)' },
            { label: 'Close', value: `$${quote.close?.toFixed(2)}` },
            { label: 'Volume', value: `${((quote.volume ?? 0) / 1000).toFixed(1)}K` },
          ].map(({ label, value, color }) => (
            <div key={label} className="glass-card" style={{ padding: '12px 16px', textAlign: 'center' }}>
              <div style={{ fontSize: 11, color: 'var(--text-muted)', fontWeight: 600, textTransform: 'uppercase', marginBottom: 4 }}>
                {label}
              </div>
              <div style={{ fontSize: 18, fontWeight: 700, color: color || 'var(--text-primary)', fontVariantNumeric: 'tabular-nums' }}>
                {value}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

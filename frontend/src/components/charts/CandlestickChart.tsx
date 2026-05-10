import { useEffect, useRef, useState } from 'react';
import { createChart, ColorType, CrosshairMode, LineStyle, IChartApi, ISeriesApi, CandlestickData } from 'lightweight-charts';
import { useChartData, useIndicators } from '../../hooks/useMarket';
import type { Timeframe } from '../../types';
import { TIMEFRAMES } from '../../types';

interface CandlestickChartProps {
  symbol?: string;
  data?: CandlestickData[];
}

const CHART_COLORS = {
  background: '#0a0e1a',
  gridLines: '#1e2d4a',
  text: '#8b9dc0',
  borderColor: '#1e2d4a',
  upColor: '#10b981',
  downColor: '#ef4444',
  wickUpColor: '#10b981',
  wickDownColor: '#ef4444',
  crosshair: '#4a5a7a',
  sma: '#3b82f6',
  ema: '#f59e0b',
  bollingerUpper: '#8b5cf6',
  bollingerLower: '#8b5cf6',
  bollingerMiddle: '#6366f1',
};

const INDICATORS = ['SMA', 'EMA', 'Bollinger Bands'];

export default function CandlestickChart({ symbol = 'KC1', data: dataProp }: CandlestickChartProps) {
  const [timeframe, setTimeframe] = useState<Timeframe>('1day');
  const [activeIndicators, setActiveIndicators] = useState<string[]>([]);
  const chartRef = useRef<HTMLDivElement>(null);
  const chartInstanceRef = useRef<IChartApi | null>(null);
  const candleSeriesRef = useRef<ISeriesApi<'Candlestick'> | null>(null);
  const smaSeriesRef = useRef<ISeriesApi<'Line'> | null>(null);
  const emaSeriesRef = useRef<ISeriesApi<'Line'> | null>(null);

  const outputSize = timeframe === '1day' ? 365 : timeframe === '1week' ? 200 : 200;
  const { data: chartData, isLoading } = useChartData(symbol, timeframe, outputSize);
  const { data: indicators } = useIndicators(symbol, timeframe);

  // Use provided data or fetch from API
  const finalData: CandlestickData[] | undefined = dataProp ? dataProp as CandlestickData[] : (chartData as any);

  // ─── Create Chart ────────────────────────────────────
  useEffect(() => {
    if (!chartRef.current) return;

    const chart = createChart(chartRef.current, {
      layout: {
        background: { type: ColorType.Solid, color: CHART_COLORS.background },
        textColor: CHART_COLORS.text,
        fontFamily: "'Inter', sans-serif",
        fontSize: 11,
      },
      grid: {
        vertLines: { color: CHART_COLORS.gridLines, style: LineStyle.Solid },
        horzLines: { color: CHART_COLORS.gridLines, style: LineStyle.Solid },
      },
      crosshair: {
        mode: CrosshairMode.Normal,
        vertLine: { color: CHART_COLORS.crosshair, labelBackgroundColor: '#1e2d4a' },
        horzLine: { color: CHART_COLORS.crosshair, labelBackgroundColor: '#1e2d4a' },
      },
      rightPriceScale: {
        borderColor: CHART_COLORS.borderColor,
        textColor: CHART_COLORS.text,
      },
      timeScale: {
        borderColor: CHART_COLORS.borderColor,
        timeVisible: true,
        secondsVisible: false,
        tickMarkFormatter: (time: number) => {
          const d = new Date(time * 1000);
          return `${d.getMonth() + 1}/${d.getDate()}`;
        },
      },
      handleScroll: true,
      handleScale: true,
    });

    const candleSeries = chart.addCandlestickSeries({
      upColor: CHART_COLORS.upColor,
      downColor: CHART_COLORS.downColor,
      wickUpColor: CHART_COLORS.wickUpColor,
      wickDownColor: CHART_COLORS.wickDownColor,
      borderVisible: false,
    });

    chartInstanceRef.current = chart;
    candleSeriesRef.current = candleSeries;

    // Responsive resize
    const ro = new ResizeObserver(() => {
      chart.applyOptions({ width: chartRef.current!.clientWidth });
    });
    ro.observe(chartRef.current);

    return () => {
      ro.disconnect();
      chart.remove();
    };
  }, []);

  // ─── Update Chart Data ────────────────────────────────
  useEffect(() => {
    if (!candleSeriesRef.current || !finalData || finalData.length === 0) return;

    const formattedData = finalData.map((d) => ({
      time: d.time as any,
      open: d.open,
      high: d.high,
      low: d.low,
      close: d.close,
    }));

    candleSeriesRef.current.setData(formattedData);
  }, [finalData]);

  // ─── Update Indicators ────────────────────────────────
  useEffect(() => {
    if (!chartInstanceRef.current || !indicators) return;

    // Remove old indicator series
    if (smaSeriesRef.current) {
      try { chartInstanceRef.current.removeSeries(smaSeriesRef.current); } catch {}
      smaSeriesRef.current = null;
    }
    if (emaSeriesRef.current) {
      try { chartInstanceRef.current.removeSeries(emaSeriesRef.current); } catch {}
      emaSeriesRef.current = null;
    }

    if (activeIndicators.includes('SMA') && indicators.sma) {
      const series = chartInstanceRef.current.addLineSeries({
        color: CHART_COLORS.sma,
        lineWidth: 1,
        title: 'SMA(20)',
        priceLineVisible: false,
      });
      series.setData(indicators.sma
        .filter(v => v.value != null)
        .map(v => ({ time: Math.floor(new Date(v.timestamp).getTime() / 1000) as any, value: v.value! }))
      );
      smaSeriesRef.current = series;
    }

    if (activeIndicators.includes('EMA') && indicators.ema) {
      const series = chartInstanceRef.current.addLineSeries({
        color: CHART_COLORS.ema,
        lineWidth: 1,
        title: 'EMA(20)',
        priceLineVisible: false,
      });
      series.setData(indicators.ema
        .filter(v => v.value != null)
        .map(v => ({ time: Math.floor(new Date(v.timestamp).getTime() / 1000) as any, value: v.value! }))
      );
      emaSeriesRef.current = series;
    }
  }, [indicators, activeIndicators]);

  const toggleIndicator = (name: string) => {
    setActiveIndicators(prev =>
      prev.includes(name) ? prev.filter(i => i !== name) : [...prev, name]
    );
  };

  return (
    <div className="glass-card" style={{ overflow: 'hidden' }}>
      {/* Chart Toolbar */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        gap: 16,
        padding: '12px 16px',
        borderBottom: '1px solid var(--border)',
        flexWrap: 'wrap',
      }}>
        {/* Symbol */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span style={{ fontSize: 16, fontWeight: 800, color: 'var(--text-primary)' }}>{symbol}</span>
          <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>Coffee Futures</span>
        </div>

        <div style={{ width: 1, height: 20, background: 'var(--border)' }} />

        {/* Timeframe Selector */}
        <div style={{ display: 'flex', gap: 2, background: 'var(--bg-primary)', borderRadius: 6, padding: 2 }}>
          {TIMEFRAMES.map(tf => (
            <button
              key={tf.value}
              className={`timeframe-btn${timeframe === tf.value ? ' active' : ''}`}
              onClick={() => setTimeframe(tf.value)}
            >
              {tf.label}
            </button>
          ))}
        </div>

        <div style={{ width: 1, height: 20, background: 'var(--border)' }} />

        {/* Indicators Toggle */}
        <div style={{ display: 'flex', gap: 4 }}>
          {INDICATORS.map(ind => (
            <button
              key={ind}
              className={`btn-ghost${activeIndicators.includes(ind) ? ' active' : ''}`}
              style={{ fontSize: 11, padding: '4px 8px' }}
              onClick={() => toggleIndicator(ind)}
            >
              {ind}
            </button>
          ))}
        </div>

        {isLoading && (
          <div style={{ marginLeft: 'auto', fontSize: 12, color: 'var(--text-muted)' }}>
            Loading...
          </div>
        )}
      </div>

      {/* TradingView Chart Canvas */}
      <div ref={chartRef} style={{ width: '100%', height: 500 }} />
    </div>
  );
}

// ─── Market Types ──────────────────────────────────────────

export interface RealtimePrice {
  symbol: string;
  price: number;
  change?: number;
  percentChange?: number;
  timestamp?: string;
}

export interface Quote {
  symbol: string;
  name: string;
  open: number;
  high: number;
  low: number;
  close: number;
  previousClose?: number;
  change?: number;
  percentChange?: number;
  volume?: number;
  timestamp?: string;
  isMarketOpen: boolean;
}

export interface Candle {
  timestamp: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume?: number;
}

export interface ChartData {
  symbol: string;
  interval: string;
  candles: Candle[];
}

export interface IndicatorValue {
  timestamp: string;
  value?: number;
}

export interface MacdValue {
  timestamp: string;
  macd?: number;
  signal?: number;
  histogram?: number;
}

export interface BollingerBand {
  timestamp: string;
  upper?: number;
  middle?: number;
  lower?: number;
}

export interface Indicators {
  rsi?: IndicatorValue[];
  macd?: MacdValue[];
  sma?: IndicatorValue[];
  ema?: IndicatorValue[];
  bollingerBands?: BollingerBand[];
}

// ─── Prediction Types ──────────────────────────────────────

export interface ForecastPoint {
  date: string;
  price: number;
}

export interface Prediction {
  symbol: string;
  method: string;
  predictedPrice: number;
  confidence: number;
  trend: 'Bullish' | 'Bearish' | 'Neutral';
  horizonDays: number;
  forecast: ForecastPoint[];
  createdAt: string;
}

// ─── Alert Types ───────────────────────────────────────────

export interface Alert {
  id: string;
  symbol: string;
  condition: 'above' | 'below';
  threshold: number;
  status: 'active' | 'triggered' | 'disabled';
  note?: string;
  triggeredAt?: string;
  triggeredPrice?: number;
  createdAt: string;
}

export interface CreateAlertRequest {
  symbol: string;
  condition: 'above' | 'below';
  threshold: number;
  note?: string;
}

// ─── Auth Types ────────────────────────────────────────────

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: 'Free' | 'Premium' | 'Admin';
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

// ─── Timeframe ─────────────────────────────────────────────

export type Timeframe = '1min' | '5min' | '15min' | '1h' | '4h' | '1day' | '1week';

export const TIMEFRAMES: { label: string; value: Timeframe }[] = [
  { label: '1M', value: '1min' },
  { label: '5M', value: '5min' },
  { label: '15M', value: '15min' },
  { label: '1H', value: '1h' },
  { label: '4H', value: '4h' },
  { label: '1D', value: '1day' },
  { label: '1W', value: '1week' },
];

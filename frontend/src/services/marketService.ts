import api from './api';
import type { RealtimePrice, Quote, ChartData, Indicators } from '../types';

export const marketService = {
  getRealtimePrice: (symbol = 'KC1') =>
    api.get<RealtimePrice>(`/market/realtime?symbol=${symbol}`).then(r => r.data),

  getQuote: (symbol = 'KC1') =>
    api.get<Quote>(`/market/quote?symbol=${symbol}`).then(r => r.data),

  getChart: (symbol = 'KC1', interval = '1day', outputSize = 100) =>
    api.get<ChartData>(`/market/chart?symbol=${symbol}&interval=${interval}&outputSize=${outputSize}`)
      .then(r => r.data),

  getIndicators: (symbol = 'KC1', interval = '1day') =>
    api.get<Indicators>(`/market/indicators?symbol=${symbol}&interval=${interval}`)
      .then(r => r.data),

  getHistory: (symbol = 'KC1', days = 365) =>
    api.get<ChartData>(`/market/history?symbol=${symbol}&days=${days}`).then(r => r.data),
};

export const predictionService = {
  getPrediction: (symbol = 'KC1', method = 'linear', horizon = 7) =>
    api.get(`/prediction?symbol=${symbol}&method=${method}&horizon=${horizon}`)
      .then(r => r.data),
};

export const alertService = {
  getAlerts: () => api.get('/alerts').then(r => r.data),
  createAlert: (data: { symbol: string; condition: string; threshold: number; note?: string }) =>
    api.post('/alerts', data).then(r => r.data),
  deleteAlert: (id: string) => api.delete(`/alerts/${id}`).then(r => r.data),
};

export const authService = {
  login: (email: string, password: string) =>
    api.post('/auth/login', { email, password }).then(r => r.data),
  register: (email: string, password: string, fullName: string) =>
    api.post('/auth/register', { email, password, fullName }).then(r => r.data),
  logout: (refreshToken: string) =>
    api.post('/auth/logout', { refreshToken }).then(r => r.data),
};

import { useQuery } from '@tanstack/react-query';
import { marketService, predictionService } from '../services/marketService';
import type { Timeframe } from '../types';

// ─── Market Hooks ──────────────────────────────────────────

export const useRealtimePrice = (symbol = 'KC1') =>
  useQuery({
    queryKey: ['price', symbol],
    queryFn: () => marketService.getRealtimePrice(symbol),
    refetchInterval: 15_000, // fallback polling if SignalR unavailable
    staleTime: 10_000,
  });

export const useQuote = (symbol = 'KC1') =>
  useQuery({
    queryKey: ['quote', symbol],
    queryFn: () => marketService.getQuote(symbol),
    refetchInterval: 30_000,
    staleTime: 20_000,
  });

export const useChartData = (symbol = 'KC1', interval: Timeframe = '1day', outputSize = 100) =>
  useQuery({
    queryKey: ['chart', symbol, interval, outputSize],
    queryFn: () => marketService.getChart(symbol, interval, outputSize),
    staleTime: 60_000,
  });

export const useIndicators = (symbol = 'KC1', interval: Timeframe = '1day') =>
  useQuery({
    queryKey: ['indicators', symbol, interval],
    queryFn: () => marketService.getIndicators(symbol, interval),
    staleTime: 5 * 60_000,
  });

// ─── Prediction Hook ───────────────────────────────────────

export const usePrediction = (symbol = 'KC1', method = 'linear', horizon = 7) =>
  useQuery({
    queryKey: ['prediction', symbol, method, horizon],
    queryFn: () => predictionService.getPrediction(symbol, method, horizon),
    staleTime: 10 * 60_000, // prediction doesn't change every second
  });

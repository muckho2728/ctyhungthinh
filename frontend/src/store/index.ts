import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { User } from '../types';

interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  login: (user: User, accessToken: string, refreshToken: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,

      login: (user, accessToken, refreshToken) => {
        localStorage.setItem('access_token', accessToken);
        localStorage.setItem('refresh_token', refreshToken);
        set({ user, accessToken, refreshToken, isAuthenticated: true });
      },

      logout: () => {
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        set({ user: null, accessToken: null, refreshToken: null, isAuthenticated: false });
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);

// ─── Market Store (realtime price via SignalR) ─────────────

interface MarketState {
  livePrice: number | null;
  liveChange: number | null;
  livePercentChange: number | null;
  liveHigh: number | null;
  liveLow: number | null;
  liveVolume: number | null;
  lastUpdated: Date | null;
  priceDirection: 'up' | 'down' | null;
  updateLiveData: (data: {
    price: number;
    change?: number;
    percentChange?: number;
    high?: number;
    low?: number;
    volume?: number;
  }) => void;
}

export const useMarketStore = create<MarketState>((set, get) => ({
  livePrice: null,
  liveChange: null,
  livePercentChange: null,
  liveHigh: null,
  liveLow: null,
  liveVolume: null,
  lastUpdated: null,
  priceDirection: null,

  updateLiveData: (data) => {
    const prev = get().livePrice;
    const direction = prev != null
      ? data.price > prev ? 'up' : data.price < prev ? 'down' : null
      : null;

    set({
      livePrice: data.price,
      liveChange: data.change ?? null,
      livePercentChange: data.percentChange ?? null,
      liveHigh: data.high ?? null,
      liveLow: data.low ?? null,
      liveVolume: data.volume ?? null,
      lastUpdated: new Date(),
      priceDirection: direction,
    });
  },
}));

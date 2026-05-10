import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useMarketStore } from '../store';

export function useSignalR(symbol = 'KC1') {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const updateLiveData = useMarketStore(s => s.updateLiveData);

  useEffect(() => {
    const hubUrl = import.meta.env.VITE_SIGNALR_URL || '/hub/market';

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => localStorage.getItem('access_token') || '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    connection.on('PriceUpdate', (data: {
      symbol: string;
      price: number;
      change?: number;
      percentChange?: number;
      high?: number;
      low?: number;
      volume?: number;
    }) => {
      if (data.symbol === symbol) {
        updateLiveData(data);
      }
    });

    const start = async () => {
      try {
        await connection.start();
        await connection.invoke('SubscribeToSymbol', symbol);
        console.log('[SignalR] Connected and subscribed to', symbol);
      } catch (err) {
        console.warn('[SignalR] Connection failed, falling back to polling', err);
      }
    };

    start();

    return () => {
      connection.stop();
    };
  }, [symbol, updateLiveData]);

  return connectionRef;
}

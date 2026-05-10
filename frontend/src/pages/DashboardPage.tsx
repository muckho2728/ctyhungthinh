import { useState, useEffect } from 'react';
import CandlestickChart from '../components/charts/CandlestickChart';
import { TrendingUp, TrendingDown, Activity, Globe } from 'lucide-react';

interface PriceData {
  time: string;
  open: number;
  high: number;
  low: number;
  close: number;
}

interface InternationalPrice {
  exchange: string;
  contract: string;
  price: number;
  change: number;
  changePercent: number;
  currency: string;
}

export default function DashboardPage() {
  const [priceData, setPriceData] = useState<PriceData[]>([]);
  const [internationalPrices, setInternationalPrices] = useState<InternationalPrice[]>([]);

  useEffect(() => {
    // Generate sample price data
    const data: PriceData[] = [];
    let price = 87000;
    const now = new Date();
    
    for (let i = 30; i >= 0; i--) {
      const date = new Date(now);
      date.setDate(date.getDate() - i);
      
      const open = price;
      const change = (Math.random() - 0.5) * 1000;
      const close = price + change;
      const high = Math.max(open, close) + Math.random() * 200;
      const low = Math.min(open, close) - Math.random() * 200;
      
      data.push({
        time: date.toISOString().split('T')[0],
        open: Math.round(open),
        high: Math.round(high),
        low: Math.round(low),
        close: Math.round(close)
      });
      
      price = close;
    }
    
    setPriceData(data);

    // Sample international prices
    setInternationalPrices([
      { exchange: 'ICE', contract: 'Arabica KC', price: 195.50, change: 2.30, changePercent: 1.19, currency: 'US cents/lb' },
      { exchange: 'ICE', contract: 'Robusta RC', price: 3120, change: -45, changePercent: -1.42, currency: 'USD/ton' },
      { exchange: 'Liffe', contract: 'Robusta', price: 3185, change: -30, changePercent: -0.93, currency: 'USD/ton' },
    ]);
  }, []);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
      {/* Page Header */}
      <div>
        <h1 style={{ fontSize: 24, fontWeight: 800, color: 'var(--text-primary)', margin: 0 }}>
          Biểu Đồ Giá Cà Phê
        </h1>
        <p style={{ fontSize: 13, color: 'var(--text-muted)', margin: '4px 0 0' }}>
          Theo dõi biến động giá và so sánh dự đoán AI
        </p>
      </div>

      {/* Price Chart */}
      <div className="glass-card" style={{ padding: '24px' }}>
        <h2 style={{ fontSize: 16, fontWeight: 700, color: 'var(--text-secondary)', marginBottom: 16 }}>
          Biểu Đồ Giá (30 ngày)
        </h2>
        <div style={{ height: '400px' }}>
          <CandlestickChart data={priceData} />
        </div>
      </div>

      {/* International Prices */}
      <div className="glass-card" style={{ padding: '24px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 16 }}>
          <Globe size={20} style={{ color: 'var(--accent-blue)' }} />
          <h2 style={{ fontSize: 16, fontWeight: 700, color: 'var(--text-secondary)', margin: 0 }}>
            Giá Quốc Tế
          </h2>
        </div>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border)' }}>
                <th style={{ textAlign: 'left', padding: '12px', fontSize: 12, fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>Sàn Giao Dịch</th>
                <th style={{ textAlign: 'left', padding: '12px', fontSize: 12, fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>Hợp Đồng</th>
                <th style={{ textAlign: 'right', padding: '12px', fontSize: 12, fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>Giá</th>
                <th style={{ textAlign: 'right', padding: '12px', fontSize: 12, fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>Thay Đổi</th>
                <th style={{ textAlign: 'right', padding: '12px', fontSize: 12, fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>%</th>
                <th style={{ textAlign: 'right', padding: '12px', fontSize: 12, fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>Đơn Vị</th>
              </tr>
            </thead>
            <tbody>
              {internationalPrices.map((item, index) => (
                <tr key={index} style={{ borderBottom: '1px solid var(--border)' }}>
                  <td style={{ padding: '12px', color: 'var(--text-primary)', fontWeight: 600 }}>{item.exchange}</td>
                  <td style={{ padding: '12px', color: 'var(--text-secondary)' }}>{item.contract}</td>
                  <td style={{ padding: '12px', textAlign: 'right', color: 'var(--text-primary)', fontWeight: 700 }}>
                    {item.price.toLocaleString('en-US')}
                  </td>
                  <td style={{ padding: '12px', textAlign: 'right' }}>
                    <span style={{ color: item.change >= 0 ? 'var(--accent-green)' : 'var(--accent-red)', fontWeight: 600 }}>
                      {item.change >= 0 ? '+' : ''}{item.change.toLocaleString('en-US')}
                    </span>
                  </td>
                  <td style={{ padding: '12px', textAlign: 'right' }}>
                    <span style={{ color: item.changePercent >= 0 ? 'var(--accent-green)' : 'var(--accent-red)', fontWeight: 600 }}>
                      {item.changePercent >= 0 ? '+' : ''}{item.changePercent.toFixed(2)}%
                    </span>
                  </td>
                  <td style={{ padding: '12px', textAlign: 'right', color: 'var(--text-muted)' }}>{item.currency}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

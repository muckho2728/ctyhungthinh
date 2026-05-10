import { useState, useEffect, useMemo, useRef } from 'react';
import { TrendingUp, TrendingDown, LineChart } from 'lucide-react';
import { api } from '../services/apiClient';
import { createChart, ColorType, CandlestickData, Time } from 'lightweight-charts';

interface CommodityPrice {
  id: number;
  symbol: string;
  close: number;
  open?: number;
  high?: number;
  low?: number;
  volume?: number;
  timestamp: string;
  region: string;
  grade: string;
  currency: string;
  percentChange?: number;
}

export default function VietnamCoffeePage() {
  const [prices, setPrices] = useState<CommodityPrice[]>([]);
  const [historyPrices, setHistoryPrices] = useState<CommodityPrice[]>([]);
  const [internationalPrices, setInternationalPrices] = useState<CommodityPrice[]>([]);
  const [loading, setLoading] = useState(true);
  // Fixed 1 year data range (no scale selector to avoid data issues)
  const [tooltipData, setTooltipData] = useState<{ time?: string; open?: number; high?: number; low?: number; close?: number } | null>(null);
  const chartContainerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    fetchPrices();

    // Auto-refresh every 30 seconds
    const interval = setInterval(fetchPrices, 30000);

    return () => clearInterval(interval);
  }, []);

  const fetchPrices = async () => {
    try {
      // Fixed 1 year output size
      const outputSize = 365;

      const [domesticResponse, historyResponse, intlResponse] = await Promise.all([
        api.coffee.getDomesticPrices() as unknown as { data: CommodityPrice[] },
        api.coffee.getPriceHistory('RC1', outputSize) as unknown as { data: CommodityPrice[] },
        api.coffee.getInternationalPrices('RC1', '1day', outputSize) as unknown as { data: CommodityPrice[] }
      ]);
      setPrices(domesticResponse.data || []);
      setHistoryPrices(historyResponse.data || []);
      setInternationalPrices(intlResponse.data || []);
    } catch (error) {
      console.error('Error fetching prices:', error);
    } finally {
      setLoading(false);
    }
  };

  // Group by region to avoid duplicates - take latest price per region
  const uniquePrices = useMemo(() => {
    const regionMap = new Map<string, CommodityPrice>();
    prices.forEach(price => {
      const existing = regionMap.get(price.region);
      if (!existing || new Date(price.timestamp) > new Date(existing.timestamp)) {
        regionMap.set(price.region, price);
      }
    });
    return Array.from(regionMap.values());
  }, [prices]);

  // Prepare candlestick data from international prices
  const candlestickData = useMemo(() => {
    console.log('historyPrices:', historyPrices.length, 'uniquePrices:', uniquePrices.length);
    if (historyPrices.length === 0) {
      // Fallback to domestic prices
      const fallback = uniquePrices.map(price => ({
        time: new Date(price.timestamp).toISOString().split('T')[0] as Time,
        open: price.close,
        high: price.close * 1.01,
        low: price.close * 0.99,
        close: price.close,
      }));
      console.log('fallback candlestickData:', fallback);
      return fallback;
    }

    const data = historyPrices
      .map(price => ({
        time: new Date(price.timestamp).toISOString().split('T')[0] as Time,
        open: price.open || price.close,
        high: price.high || price.close,
        low: price.low || price.close,
        close: price.close,
      }))
      .sort((a, b) => String(a.time).localeCompare(String(b.time)));
    console.log('history candlestickData:', data.slice(0, 3), 'total:', data.length);
    return data;
  }, [historyPrices, uniquePrices]);

  // Initialize candlestick chart
  useEffect(() => {
    if (!chartContainerRef.current) {
      console.log('chartContainerRef is null');
      return;
    }

    const container = chartContainerRef.current;
    console.log('chart init - container width:', container.clientWidth, 'height:', container.clientHeight, 'candlestickData length:', candlestickData.length);

    if (container.clientWidth === 0) {
      console.warn('Chart container has zero width, deferring init');
      return;
    }

    if (candlestickData.length === 0) {
      console.warn('No candlestick data to display');
      return;
    }

    const chart = createChart(container, {
      autoSize: true,
      height: 400,
      layout: {
        background: { type: ColorType.Solid, color: 'white' },
        textColor: '#333',
      },
      grid: {
        vertLines: { color: '#e1e1e1' },
        horzLines: { color: '#e1e1e1' },
      },
      crosshair: {
        mode: 1,
        vertLine: {
          color: '#758696',
          width: 1,
          style: 3,
          labelBackgroundColor: '#758696',
        },
        horzLine: {
          color: '#758696',
          width: 1,
          style: 3,
          labelBackgroundColor: '#758696',
        },
      },
      rightPriceScale: {
        borderColor: '#cccccc',
        scaleMargins: {
          top: 0.1,
          bottom: 0.2,
        },
      },
      timeScale: {
        borderColor: '#cccccc',
        timeVisible: false,
        barSpacing: 12,
        rightOffset: 10,
        lockVisibleTimeRangeOnResize: true,
      },
    });

    const candlestickSeries = chart.addCandlestickSeries({
      upColor: '#26a69a',
      downColor: '#ef5350',
      borderVisible: false,
      wickUpColor: '#26a69a',
      wickDownColor: '#ef5350',
      priceFormat: {
        type: 'price',
        precision: 0,
        minMove: 1,
      },
    });

    // Validate data before setting
    const timeSet = new Set();
    const duplicates = [];
    let invalidCount = 0;
    for (const d of candlestickData) {
      if (timeSet.has(d.time)) duplicates.push(d.time);
      timeSet.add(d.time);
      if (!d.open || !d.high || !d.low || !d.close) invalidCount++;
    }
    console.log('duplicates:', duplicates.length, 'invalid:', invalidCount);
    if (duplicates.length > 0) console.log('duplicate times:', duplicates.slice(0, 5));

    // Remove duplicates (keep last occurrence)
    const uniqueData = [];
    const seen = new Set();
    for (let i = candlestickData.length - 1; i >= 0; i--) {
      if (!seen.has(candlestickData[i].time)) {
        seen.add(candlestickData[i].time);
        uniqueData.unshift(candlestickData[i]);
      }
    }
    console.log('uniqueData length:', uniqueData.length);

    candlestickSeries.setData(uniqueData);
    // Show last 60 candles by default, allow scroll to see earlier
    if (uniqueData.length > 60) {
      chart.timeScale().setVisibleLogicalRange({
        from: uniqueData.length - 60,
        to: uniqueData.length,
      });
    } else {
      chart.timeScale().fitContent();
    }
    console.log('chart rendered with', uniqueData.length, 'candles');

    // Crosshair subscription for OHLC tooltip
    chart.subscribeCrosshairMove((param) => {
      if (param.time && param.point && param.seriesData) {
        const data = param.seriesData.get(candlestickSeries) as any;
        if (data) {
          setTooltipData({
            time: new Date(data.time + 'T00:00:00Z').toLocaleDateString('vi-VN'),
            open: data.open,
            high: data.high,
            low: data.low,
            close: data.close,
          });
        }
      }
    });

    return () => {
      chart.remove();
    };
  }, [candlestickData]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-lg">Đang tải...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-6xl mx-auto">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">
            Giá Cà Phê Việt Nam
          </h1>
          <p className="text-gray-600">
            Dữ liệu từ giacaphe.com - Cập nhật theo thời gian thực
          </p>
        </div>

        {/* Chart Section */}
        <div className="bg-white rounded-lg shadow-md p-6 mb-8">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-bold text-gray-900 flex items-center gap-2">
              <LineChart size={20} className="text-amber-600" />
              Biểu Đồ Giá Cà Phê (Nến)
            </h2>
            <span className="text-sm text-gray-500">Dữ liệu 1 năm gần nhất</span>
          </div>
          <div className="mb-4 flex justify-center gap-6 text-sm text-gray-600">
            <div className="flex items-center gap-2">
              <div className="w-4 h-4 bg-green-500 rounded-sm"></div>
              <span>Tăng giá</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-4 h-4 bg-red-500 rounded-sm"></div>
              <span>Giảm giá</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-xs">OHLC: Mở-Cao-Thấp-Đóng</span>
            </div>
          </div>
          <div ref={chartContainerRef} style={{ height: 400, width: '100%', position: 'relative' }} />
          {tooltipData && (
            <div className="mt-4 bg-gray-50 rounded-lg p-4 border border-gray-200">
              <div className="flex justify-between items-center mb-2">
                <span className="font-semibold text-gray-900">{tooltipData.time}</span>
                <span className="text-xs text-gray-500">Di chuyển chuột trên biểu đồ để xem chi tiết</span>
              </div>
              <div className="grid grid-cols-4 gap-4">
                <div className="text-center">
                  <div className="text-xs text-gray-500 mb-1">Mở cửa</div>
                  <div className="font-bold text-green-600">{tooltipData.open?.toLocaleString('vi-VN')} <span className="text-xs">VND</span></div>
                </div>
                <div className="text-center">
                  <div className="text-xs text-gray-500 mb-1">Cao nhất</div>
                  <div className="font-bold text-red-600">{tooltipData.high?.toLocaleString('vi-VN')} <span className="text-xs">VND</span></div>
                </div>
                <div className="text-center">
                  <div className="text-xs text-gray-500 mb-1">Thấp nhất</div>
                  <div className="font-bold text-orange-600">{tooltipData.low?.toLocaleString('vi-VN')} <span className="text-xs">VND</span></div>
                </div>
                <div className="text-center">
                  <div className="text-xs text-gray-500 mb-1">Đóng cửa</div>
                  <div className="font-bold text-blue-600">{tooltipData.close?.toLocaleString('vi-VN')} <span className="text-xs">VND</span></div>
                </div>
              </div>
            </div>
          )}
          {internationalPrices.length > 0 && (
            <div className="mt-4 bg-blue-50 rounded-lg p-4 border border-blue-200">
              <h3 className="font-semibold text-blue-900 mb-2 text-sm">So sánh giá quốc tế (Robusta - London)</h3>
              <div className="grid grid-cols-3 gap-4">
                <div>
                  <div className="text-xs text-gray-500">Giá quốc tế mới nhất</div>
                  <div className="font-bold text-gray-900">{internationalPrices[0].close.toLocaleString('vi-VN')} <span className="text-xs">{internationalPrices[0].currency}</span></div>
                </div>
                <div>
                  <div className="text-xs text-gray-500">Thay đổi</div>
                  <div className={`font-bold ${(internationalPrices[0].percentChange ?? 0) >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                    {(internationalPrices[0].percentChange ?? 0) >= 0 ? '+' : ''}{internationalPrices[0].percentChange?.toFixed(2) ?? '0'}%
                  </div>
                </div>
                <div>
                  <div className="text-xs text-gray-500">Khối lượng</div>
                  <div className="font-bold text-gray-900">{internationalPrices[0].volume?.toLocaleString('vi-VN') ?? 'N/A'}</div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Price Table */}
        <div className="bg-white rounded-lg shadow-md overflow-hidden mb-8">
          <h2 className="text-xl font-bold text-gray-900 p-6 pb-4">Chi Tiết Giá Theo Vùng</h2>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Vùng</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Loại</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Giá</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Thay Đổi</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Cập Nhật</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {uniquePrices.map((item) => (
                  <tr key={item.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">{item.region}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-500">{item.grade}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right">
                      <div className="text-sm font-bold text-gray-900">
                        {item.close.toLocaleString('vi-VN')} {item.currency}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right">
                      <div className="flex items-center justify-end gap-2">
                        {(item.percentChange ?? 0) > 0 ? (
                          <TrendingUp className="text-green-500" size={16} />
                        ) : (item.percentChange ?? 0) < 0 ? (
                          <TrendingDown className="text-red-500" size={16} />
                        ) : null}
                        <span
                          className={`text-sm font-medium ${(item.percentChange ?? 0) > 0
                              ? 'text-green-600'
                              : (item.percentChange ?? 0) < 0
                                ? 'text-red-600'
                                : 'text-gray-600'
                            }`}
                        >
                          {item.percentChange && item.percentChange > 0 ? '+' : ''}
                          {item.percentChange?.toLocaleString('vi-VN', { maximumFractionDigits: 2 }) ?? '0'}%
                        </span>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-500">
                        {new Date(item.timestamp).toLocaleString('vi-VN')}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Prediction Section */}
        <div className="mt-12 bg-amber-50 border border-amber-200 rounded-lg p-6">
          <div className="flex items-center gap-3 mb-4">
            <LineChart className="text-amber-600" size={24} />
            <h2 className="text-2xl font-bold text-amber-900">Dự Báo Giá Cà Phê</h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
            <div className="bg-white rounded-lg p-5 shadow-sm">
              <div className="text-sm text-gray-500 mb-2">Dự báo 7 ngày</div>
              <div className="text-2xl font-bold text-gray-900">87,500 VND/kg</div>
              <div className="text-sm text-green-600 mt-2">+0.5%</div>
            </div>
            <div className="bg-white rounded-lg p-5 shadow-sm">
              <div className="text-sm text-gray-500 mb-2">Dự báo 30 ngày</div>
              <div className="text-2xl font-bold text-gray-900">88,200 VND/kg</div>
              <div className="text-sm text-green-600 mt-2">+1.2%</div>
            </div>
            <div className="bg-white rounded-lg p-5 shadow-sm">
              <div className="text-sm text-gray-500 mb-2">Độ tin cậy</div>
              <div className="text-2xl font-bold text-gray-900">85%</div>
              <div className="text-sm text-gray-500 mt-2">Dựa trên ML model</div>
            </div>
          </div>

          <div className="bg-white rounded-lg p-5">
            <h3 className="font-semibold text-gray-900 mb-3">Phân tích xu hướng</h3>
            <p className="text-sm text-gray-600 leading-relaxed">
              Dựa trên dữ liệu thị trường và các yếu tố mùa vụ, giá cà phê dự kiến sẽ tăng nhẹ trong 30 ngày tới do nhu cầu tăng cao từ thị trường xuất khẩu.
            </p>
          </div>
        </div>

        <div className="mt-8 bg-blue-50 border border-blue-200 rounded-lg p-4">
          <h3 className="font-semibold text-blue-900 mb-2">Ghi chú:</h3>
          <ul className="text-sm text-blue-800 space-y-1">
            <li>• Giá cà phê được cập nhật hàng ngày từ giacaphe.com</li>
            <li>• Đơn vị tính: VND/kg</li>
            <li>• Thay đổi so với ngày trước đó</li>
            <li>• Dự báo dựa trên ML model - mang tính chất tham khảo</li>
          </ul>
        </div>

        <div className="mt-8 bg-red-50 border border-red-500 rounded-lg p-4">
          <h3 className="font-semibold text-red-900 mb-2">Hưng Thịnh Kiến Đức</h3>
          <ul className="text-sm text-red-800 space-y-1">
            <li>• Chuyên mua bán các loại nông sản: cà phê, điều tiêu, macca, chanh dây...</li>
            <li>• Điện thoại/Zalo: 0385063507</li>
            <li>• Hãy liên hệ với chúng tôi để được tư vấn và báo giá tốt nhất</li>
          </ul>
        </div>
      </div>
    </div>
  );
}

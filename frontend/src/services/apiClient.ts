import axios, { AxiosError } from 'axios';
import type { AxiosInstance, InternalAxiosRequestConfig } from 'axios';

// Types
export interface ApiError {
  error: string;
  message: string;
  traceId?: string;
  timestamp: string;
  stackTrace?: string;
}

export interface ApiResponse<T> {
  data: T;
  error?: ApiError;
}

// API Client class
class ApiClient {
  private client: AxiosInstance;
  private static instance: ApiClient;

  private constructor() {
    const baseURL = import.meta.env.VITE_API_BASE_URL || '/api';

    this.client = axios.create({
      baseURL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor - add auth token
    this.client.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        const token = localStorage.getItem('access_token');
        if (token && config.headers) {
          config.headers.Authorization = `Bearer ${token}`;
        }

        // Add correlation ID if not present
        if (!config.headers['X-Correlation-ID']) {
          config.headers['X-Correlation-ID'] = crypto.randomUUID();
        }

        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor - handle errors
    this.client.interceptors.response.use(
      (response) => response,
      (error: AxiosError<ApiError>) => {
        if (error.response?.status === 401) {
          // Unauthorized - clear token and redirect to login
          localStorage.removeItem('access_token');
          localStorage.removeItem('refresh_token');
          window.location.href = '/login';
        }

        // Extract error details
        const apiError: ApiError = error.response?.data || {
          error: 'Network Error',
          message: error.message || 'An unexpected error occurred',
          timestamp: new Date().toISOString(),
        };

        return Promise.reject(apiError);
      }
    );
  }

  public static getInstance(): ApiClient {
    if (!ApiClient.instance) {
      ApiClient.instance = new ApiClient();
    }
    return ApiClient.instance;
  }

  // HTTP methods
  public async get<T>(url: string, params?: Record<string, unknown>): Promise<T> {
    const response = await this.client.get<T>(url, { params });
    return response.data;
  }

  public async post<T>(url: string, data?: unknown): Promise<T> {
    const response = await this.client.post<T>(url, data);
    return response.data;
  }

  public async put<T>(url: string, data?: unknown): Promise<T> {
    const response = await this.client.put<T>(url, data);
    return response.data;
  }

  public async patch<T>(url: string, data?: unknown): Promise<T> {
    const response = await this.client.patch<T>(url, data);
    return response.data;
  }

  public async delete<T>(url: string): Promise<T> {
    const response = await this.client.delete<T>(url);
    return response.data;
  }

  // Auth methods
  public setAuthToken(token: string): void {
    localStorage.setItem('access_token', token);
  }

  public clearAuthToken(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  }

  public getAuthToken(): string | null {
    return localStorage.getItem('access_token');
  }
}

// Export singleton instance
export const apiClient = ApiClient.getInstance();

// Export typed API methods
export const api = {
  auth: {
    login: (email: string, password: string) =>
      apiClient.post('/auth/login', { email, password }),
    register: (email: string, password: string, fullName: string) =>
      apiClient.post('/auth/register', { email, password, fullName }),
    refreshToken: (refreshToken: string) =>
      apiClient.post('/auth/refresh-token', { refreshToken }),
    logout: (refreshToken: string) =>
      apiClient.post('/auth/logout', { refreshToken }),
  },
  market: {
    getRealtimePrice: (symbol: string = 'KC1') =>
      apiClient.get('/market/realtime', { symbol }),
    getQuote: (symbol: string = 'KC1') =>
      apiClient.get('/market/quote', { symbol }),
    getChart: (symbol: string = 'KC1', interval: string = '1day', outputSize: number = 100) =>
      apiClient.get('/market/chart', { symbol, interval, outputSize }),
    getIndicators: (symbol: string = 'KC1', interval: string = '1day') =>
      apiClient.get('/market/indicators', { symbol, interval }),
    getHistory: (symbol: string = 'KC1', days: number = 365) =>
      apiClient.get('/market/history', { symbol, days }),
  },
  predictions: {
    getPredictions: (symbol: string = 'KC1') =>
      apiClient.get('/predictions', { symbol }),
    createPrediction: (symbol: string, method: string) =>
      apiClient.post('/predictions', { symbol, method }),
  },
  alerts: {
    getAlerts: () => apiClient.get('/alerts'),
    createAlert: (data: unknown) => apiClient.post('/alerts', data),
    updateAlert: (id: string, data: unknown) => apiClient.put(`/alerts/${id}`, data),
    deleteAlert: (id: string) => apiClient.delete(`/alerts/${id}`),
  },
  coffee: {
    getDomesticPrices: () =>
      apiClient.get('/coffee/prices/domestic'),
    getInternationalPrices: (symbol: string = 'RC1', interval: string = '1day', outputSize: number = 30) =>
      apiClient.get('/coffee/prices/international', { symbol, interval, outputSize }),
    getPriceHistory: (symbol: string = 'RC1', days: number = 90) =>
      apiClient.get('/coffee/prices/history', { symbol, days }),
  },
  pepper: {
    getDomesticPrices: (refresh = false) =>
      apiClient.get('/pepper/prices/domestic', { refresh }),
    getSummary: (refresh = false) =>
      apiClient.get('/pepper/prices/summary', { refresh }),
    getInternationalPrices: (symbol: string = 'VPA', outputSize: number = 30) =>
      apiClient.get('/pepper/prices/international', { symbol, outputSize }),
    getPriceHistory: (symbol: string = 'VPA', days: number = 90) =>
      apiClient.get('/pepper/prices/history', { symbol, days }),
  },
};

export default apiClient;

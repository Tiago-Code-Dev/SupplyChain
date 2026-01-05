import axios from 'axios';
import type { AxiosError, AxiosInstance } from 'axios';
import { API_CONFIG, API_ENDPOINTS } from '../config/api';
import type { ApiError } from '../types/api';

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: API_CONFIG.baseURL,
      timeout: API_CONFIG.timeout,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    // Request interceptor - adiciona token
    this.client.interceptors.request.use(
      (config) => {
        const token = this.getToken();
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor - trata erros
    this.client.interceptors.response.use(
      (response) => response,
      async (error: AxiosError<ApiError>) => {
        if (error.response?.status === 401) {
          // Token expirado - tentar refresh
          const refreshToken = this.getRefreshToken();
          if (refreshToken && !error.config?.url?.includes('/refresh-token')) {
            try {
              const newTokens = await this.refreshAccessToken(refreshToken);
              this.setTokens(newTokens.accessToken, newTokens.refreshToken);
              
              // Retry original request
              if (error.config) {
                error.config.headers.Authorization = `Bearer ${newTokens.accessToken}`;
                return this.client.request(error.config);
              }
            } catch (refreshError) {
              // Refresh falhou - limpar tokens e redirecionar para login
              this.clearTokens();
              window.location.href = '/login';
              return Promise.reject(refreshError);
            }
          } else {
            this.clearTokens();
            window.location.href = '/login';
          }
        }

        return Promise.reject(this.formatError(error));
      }
    );
  }

  private async refreshAccessToken(refreshToken: string) {
    const response = await axios.post<{
      accessToken: string;
      refreshToken: string;
    }>(`${API_CONFIG.baseURL}${API_ENDPOINTS.auth.refreshToken}`, {
      refreshToken,
    });
    return response.data;
  }

  private formatError(error: AxiosError<ApiError>): ApiError {
    if (error.response?.data) {
      return error.response.data;
    }
    return {
      error: error.message || 'An unexpected error occurred',
      statusCode: error.response?.status,
    };
  }

  private getToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  private getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  private setTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
  }

  private clearTokens(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }

  public setAuthToken(token: string): void {
    localStorage.setItem('accessToken', token);
  }

  public setRefreshToken(token: string): void {
    localStorage.setItem('refreshToken', token);
  }

  public clearAuth(): void {
    this.clearTokens();
  }

  get instance(): AxiosInstance {
    return this.client;
  }
}

// Export singleton instance
export const apiClient = new ApiClient();
export default apiClient.instance;

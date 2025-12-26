import apiClient from '../../../shared/services/api-client';
import { API_ENDPOINTS } from '../../../shared/config/api';
import {
  LoginRequest,
  AuthResponse,
  UserInfo,
  RefreshTokenRequest,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from '../../../shared/types/api';

export const authService = {
  async login(credentials: LoginRequest): Promise<AuthResponse> {
    const response = await apiClient.post<AuthResponse>(
      API_ENDPOINTS.auth.login,
      credentials
    );
    return response.data;
  },

  async refreshToken(refreshToken: string): Promise<AuthResponse> {
    const response = await apiClient.post<AuthResponse>(
      API_ENDPOINTS.auth.refreshToken,
      { refreshToken } as RefreshTokenRequest
    );
    return response.data;
  },

  async getCurrentUser(): Promise<UserInfo> {
    const response = await apiClient.get<UserInfo>(API_ENDPOINTS.auth.me);
    return response.data;
  },

  async changePassword(data: ChangePasswordRequest): Promise<void> {
    await apiClient.post(API_ENDPOINTS.auth.changePassword, data);
  },

  async forgotPassword(data: ForgotPasswordRequest): Promise<void> {
    await apiClient.post(API_ENDPOINTS.auth.forgotPassword, data);
  },

  async resetPassword(data: ResetPasswordRequest): Promise<void> {
    await apiClient.post(API_ENDPOINTS.auth.resetPassword, data);
  },

  async revokeToken(refreshToken: string): Promise<void> {
    await apiClient.post(API_ENDPOINTS.auth.revokeToken, { refreshToken });
  },

  async revokeAllTokens(): Promise<void> {
    await apiClient.post(API_ENDPOINTS.auth.revokeAllTokens);
  },

  async logout(): Promise<void> {
    const refreshToken = localStorage.getItem('refreshToken');
    if (refreshToken) {
      try {
        await this.revokeToken(refreshToken);
      } catch (error) {
        console.error('Error revoking token:', error);
      }
    }
    apiClient.clearAuth();
  },
};



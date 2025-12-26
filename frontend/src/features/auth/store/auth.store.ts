import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { authService } from '../services/auth.service';
import { UserInfo, AuthResponse } from '../../../shared/types/api';
import { apiClient } from '../../../shared/services/api-client';

interface AuthState {
  user: UserInfo | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  setUser: (user: UserInfo) => void;
  clearError: () => void;
  checkAuth: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,

      login: async (email: string, password: string) => {
        set({ isLoading: true, error: null });
        try {
          const response: AuthResponse = await authService.login({ email, password });
          
          // Salvar tokens
          apiClient.setAuthToken(response.accessToken);
          apiClient.setRefreshToken(response.refreshToken);
          
          // Buscar informações completas do usuário
          const userInfo = await authService.getCurrentUser();
          
          set({
            user: userInfo,
            isAuthenticated: true,
            isLoading: false,
            error: null,
          });
        } catch (error: any) {
          let errorMessage = 'Login failed';
          
          // Tratar erro 429 (Rate Limiting) especificamente
          if (error.response?.status === 429) {
            const retryAfter = error.response?.data?.tentarNovamenteEm || '60 segundos';
            errorMessage = `Muitas tentativas de login. ${error.response?.data?.detalhe || 'Por favor, aguarde ' + retryAfter + ' antes de tentar novamente.'}`;
          } else if (error.response?.data?.error) {
            errorMessage = error.response.data.error;
          } else if (error.message) {
            errorMessage = error.message;
          }
          
          set({
            isLoading: false,
            error: errorMessage,
            isAuthenticated: false,
          });
          throw error;
        }
      },

      logout: async () => {
        try {
          await authService.logout();
        } catch (error) {
          console.error('Error during logout:', error);
        } finally {
          set({
            user: null,
            isAuthenticated: false,
            error: null,
          });
        }
      },

      setUser: (user: UserInfo) => {
        set({ user, isAuthenticated: true });
      },

      clearError: () => {
        set({ error: null });
      },

      checkAuth: async () => {
        const token = localStorage.getItem('accessToken');
        if (!token) {
          set({ isAuthenticated: false, user: null });
          return;
        }

        set({ isLoading: true });
        try {
          const userInfo = await authService.getCurrentUser();
          set({
            user: userInfo,
            isAuthenticated: true,
            isLoading: false,
          });
        } catch (error) {
          set({
            isAuthenticated: false,
            user: null,
            isLoading: false,
          });
          apiClient.clearAuth();
        }
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);



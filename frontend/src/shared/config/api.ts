export const API_CONFIG = {
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  apiVersion: 'v1',
  timeout: 30000,
} as const;

export const API_ENDPOINTS = {
  auth: {
    login: '/api/auth/login',
    refreshToken: '/api/auth/refresh-token',
    revokeToken: '/api/auth/revoke-token',
    revokeAllTokens: '/api/auth/revoke-all-tokens',
    me: '/api/auth/me',
    changePassword: '/api/auth/change-password',
    forgotPassword: '/api/auth/forgot-password',
    resetPassword: '/api/auth/reset-password',
    register: '/api/auth/register',
    roles: '/api/auth/roles',
  },
  employees: {
    list: '/api/employees',
    detail: (id: string) => `/api/employees/${id}`,
    create: '/api/employees',
    update: (id: string) => `/api/employees/${id}`,
    delete: (id: string) => `/api/employees/${id}`,
  },
} as const;



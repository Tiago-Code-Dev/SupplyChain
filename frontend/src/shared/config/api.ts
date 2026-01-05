export const API_CONFIG = {
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  apiVersion: 'v1',
  timeout: 30000,
} as const;

// Helper para construir URLs com versionamento
const apiUrl = (path: string) => `/api/${API_CONFIG.apiVersion}${path}`;

export const API_ENDPOINTS = {
  auth: {
    login: apiUrl('/auth/login'),
    refreshToken: apiUrl('/auth/refresh-token'),
    revokeToken: apiUrl('/auth/revoke-token'),
    revokeAllTokens: apiUrl('/auth/revoke-all-tokens'),
    me: apiUrl('/auth/me'),
    changePassword: apiUrl('/auth/change-password'),
    forgotPassword: apiUrl('/auth/forgot-password'),
    resetPassword: apiUrl('/auth/reset-password'),
    register: apiUrl('/auth/register'),
    roles: apiUrl('/auth/roles'),
  },
  employees: {
    list: apiUrl('/employees'),
    detail: (id: string) => apiUrl(`/employees/${id}`),
    create: apiUrl('/employees'),
    update: (id: string) => apiUrl(`/employees/${id}`),
    delete: (id: string) => apiUrl(`/employees/${id}`),
  },
  roles: {
    list: apiUrl('/roles'),
    detail: (id: string) => apiUrl(`/roles/${id}`),
    parents: apiUrl('/roles/parents'),
    hierarchy: apiUrl('/roles/hierarchy'),
    create: apiUrl('/roles'),
    update: (id: string) => apiUrl(`/roles/${id}`),
    delete: (id: string) => apiUrl(`/roles/${id}`),
  },
} as const;



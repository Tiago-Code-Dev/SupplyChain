// API Response Types
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  isFirstPage: boolean;
  isLastPage: boolean;
  firstItemIndex: number;
  lastItemIndex: number;
}

export type ApiError = {
  error?: string;
  errors?: Record<string, string[]>;
  message?: string;
  statusCode?: number;
};

// Auth Types
export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface UserInfo {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
  firstName?: string;
  lastName?: string;
  employeeId?: string;
  isActive?: boolean;
  claims?: Record<string, string>;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

// Employee Types - Usando const object em vez de enum
export const Role = {
  Employee: 1,
  Leader: 2,
  Director: 3,
  Admin: 4,
} as const;

export type Role = (typeof Role)[keyof typeof Role];

// Custom Role Types (Cargos Customizados)
export interface CustomRole {
  id: string;
  name: string;
  displayName: string;
  hierarchyLevel: number;
  isSystemRole: boolean;
}

export interface CreateCustomRoleRequest {
  name: string;
  displayName: string;
  parentRoleId?: string;
  hierarchyLevel?: number;
}

export interface UpdateCustomRoleRequest {
  displayName: string;
  hierarchyLevel: number;
}

// Hierarchy Types (resposta da API de hierarquia)
export interface HierarchyItem {
  id: string;
  name: string;
  displayName: string;
  hierarchyLevel: number;
  isSystemRole: boolean;
  canManage: string[]; // Lista de displayNames dos cargos que pode gerenciar
}

export interface HierarchyResponse {
  roles: HierarchyItem[];
}

// Mantém RoleHierarchy para compatibilidade se necessário
export interface RoleHierarchy {
  role: CustomRole;
  canManage: CustomRole[];
}

export interface Employee {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  documentNumber: string;
  birthDate: string;
  role: Role;
  managerId: string | null;
  managerName: string | null;
  phoneNumbers: string[];
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
}

export interface CreateEmployeeRequest {
  firstName: string;
  lastName: string;
  email: string;
  documentNumber: string;
  birthDate: string;
  password: string;
  role: Role;
  managerId?: string | null;
  phoneNumbers?: string[];
}

export interface UpdateEmployeeRequest {
  firstName: string;
  lastName: string;
  email: string;
  birthDate: string;
  managerId?: string | null;
  phoneNumbers?: string[];
  role?: Role;
}

export interface EmployeeQueryParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  filterByName?: string;
  filterByEmail?: string;
  filterByRole?: Role;
  filterByManagerId?: string;
  sortBy?: string;
  sortDescending?: boolean;
}



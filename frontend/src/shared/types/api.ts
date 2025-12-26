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

// Employee Types
export enum Role {
  Employee = 1,
  Leader = 2,
  Director = 3,
  Admin = 4,
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

// Auth Types
export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: UserResponse;
}

export interface UserResponse {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface UserInfo {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  employeeId: string | null;
  isActive: boolean;
  roles: string[];
  claims: Record<string, string>;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}



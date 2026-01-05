import apiClient from '../../../shared/services/api-client';
import { API_ENDPOINTS } from '../../../shared/config/api';
import {
  CustomRole,
  CreateCustomRoleRequest,
  UpdateCustomRoleRequest,
  HierarchyResponse,
  HierarchyItem,
} from '../../../shared/types/api';

export const rolesService = {
  async getAllRoles(): Promise<CustomRole[]> {
    const response = await apiClient.get<CustomRole[]>(API_ENDPOINTS.roles.list);
    return response.data;
  },

  async getRoleById(id: string): Promise<CustomRole> {
    const response = await apiClient.get<CustomRole>(API_ENDPOINTS.roles.detail(id));
    return response.data;
  },

  async getParentRoles(): Promise<CustomRole[]> {
    const response = await apiClient.get<CustomRole[]>(API_ENDPOINTS.roles.parents);
    return response.data;
  },

  async getRolesHierarchy(): Promise<HierarchyItem[]> {
    const response = await apiClient.get<HierarchyResponse>(API_ENDPOINTS.roles.hierarchy);
    return response.data.roles;
  },

  async createRole(data: CreateCustomRoleRequest): Promise<CustomRole> {
    const response = await apiClient.post<CustomRole>(
      API_ENDPOINTS.roles.create,
      data
    );
    return response.data;
  },

  async updateRole(id: string, data: UpdateCustomRoleRequest): Promise<CustomRole> {
    const response = await apiClient.put<CustomRole>(
      API_ENDPOINTS.roles.update(id),
      data
    );
    return response.data;
  },

  async deleteRole(id: string): Promise<void> {
    const url = API_ENDPOINTS.roles.delete(id);
    console.log('DELETE request URL:', url);
    await apiClient.delete(url);
  },
};
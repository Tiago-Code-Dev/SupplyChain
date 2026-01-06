import apiClient from '../../../shared/services/api-client';
import { API_ENDPOINTS } from '../../../shared/config/api';
import { fixEncodingInObject } from '../../../shared/utils/encoding.utils';
import {
  Employee,
  PagedResult,
  EmployeeQueryParams,
  CreateEmployeeRequest,
  UpdateEmployeeRequest,
} from '../../../shared/types/api';

export const employeesService = {
  async getEmployees(params: EmployeeQueryParams = {}): Promise<PagedResult<Employee>> {
    const response = await apiClient.get<PagedResult<Employee>>(
      API_ENDPOINTS.employees.list,
      { params }
    );
    return fixEncodingInObject(response.data);
  },

  async getEmployeeById(id: string): Promise<Employee> {
    const response = await apiClient.get<Employee>(API_ENDPOINTS.employees.detail(id));
    return fixEncodingInObject(response.data);
  },

  async createEmployee(data: CreateEmployeeRequest): Promise<Employee> {
    const response = await apiClient.post<Employee>(
      API_ENDPOINTS.employees.create,
      data
    );
    return fixEncodingInObject(response.data);
  },

  async updateEmployee(id: string, data: UpdateEmployeeRequest): Promise<Employee> {
    const response = await apiClient.put<Employee>(
      API_ENDPOINTS.employees.update(id),
      data
    );
    return fixEncodingInObject(response.data);
  },

  async deleteEmployee(id: string): Promise<void> {
    await apiClient.delete(API_ENDPOINTS.employees.delete(id));
  },
};



import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../features/auth/store/auth.store';
import { employeesService } from '../features/employees/services/employees.service';
import { Employee, EmployeeQueryParams, Role } from '../shared/types/api';
import { PagedResult } from '../shared/types/api';
import { Button } from '../shared/components/Button';
import { Input } from '../shared/components/Input';
import { Select } from '../shared/components/Select';
import { LoadingSpinner } from '../shared/components/LoadingSpinner';
import { ErrorAlert } from '../shared/components/ErrorAlert';
import { formatDate, calculateAge } from '../shared/utils/date.utils';
import { getRoleLabel, getRoleColor, canCreateEmployee } from '../shared/utils/role.utils';
import { PlusIcon, MagnifyingGlassIcon } from '@heroicons/react/24/outline';

export const EmployeesPage = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();
  const [employees, setEmployees] = useState<PagedResult<Employee> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterRole, setFilterRole] = useState<Role | ''>('');
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 10;

  const loadEmployees = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const params: EmployeeQueryParams = {
        pageNumber: currentPage,
        pageSize,
        searchTerm: searchTerm || undefined,
        filterByRole: filterRole || undefined,
      };
      const data = await employeesService.getEmployees(params);
      setEmployees(data);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Erro ao carregar funcionários');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadEmployees();
  }, [currentPage, searchTerm, filterRole]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setCurrentPage(1);
    loadEmployees();
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Tem certeza que deseja excluir este funcionário?')) return;
    
    try {
      await employeesService.deleteEmployee(id);
      loadEmployees();
    } catch (err: any) {
      alert(err.response?.data?.error || 'Erro ao excluir funcionário');
    }
  };

  const roleOptions = [
    { value: '', label: 'Todas as funções' },
    { value: Role.Employee, label: 'Funcionário' },
    { value: Role.Leader, label: 'Líder' },
    { value: Role.Director, label: 'Diretor' },
    { value: Role.Admin, label: 'Administrador' },
  ];

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-gray-900">Funcionários</h1>
          <div className="flex items-center gap-4">
            <span className="text-sm text-gray-600">
              {user?.fullName} ({user?.roles?.[0] || 'N/A'})
            </span>
            <Button variant="secondary" onClick={logout}>
              Sair
            </Button>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header com botão de adicionar */}
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-xl font-semibold text-gray-900">Lista de Funcionários</h2>
          {user?.roles && user.roles.length > 0 && (() => {
            // Verificar se o usuário tem permissão
            const userRoleString = user.roles[0]?.toLowerCase();
            
            // Verificar permissão diretamente pela string (mais simples)
            const allowedRoles = ['leader', 'director', 'admin'];
            const hasPermission = allowedRoles.includes(userRoleString || '');
            
            // Mostrar botão se tiver permissão
            if (hasPermission) {
              return (
                <Button
                  type="button"
                  variant="primary"
                  onClick={() => navigate('/employees/new')}
                  className="flex items-center gap-2 whitespace-nowrap"
                >
                  <PlusIcon className="h-5 w-5" />
                  <span>Adicionar Funcionário</span>
                </Button>
              );
            }
            
            return null;
          })()}
        </div>

        {/* Filters */}
        <div className="card mb-6">
          <form onSubmit={handleSearch} className="flex gap-4">
            <div className="flex-1">
              <Input
                type="text"
                placeholder="Buscar por nome, email ou documento..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
            </div>
            <div className="w-48">
              <Select
                options={roleOptions}
                value={filterRole}
                onChange={(e) => setFilterRole(e.target.value as Role | '')}
              />
            </div>
            <Button type="submit" variant="primary">
              <MagnifyingGlassIcon className="h-5 w-5" />
            </Button>
          </form>
        </div>

        {error && <ErrorAlert message={error} />}

        {/* Table */}
        {isLoading ? (
          <div className="flex justify-center py-12">
            <LoadingSpinner size="lg" />
          </div>
        ) : employees && employees.items.length > 0 ? (
          <>
            <div className="card overflow-hidden">
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Nome
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Email
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Função
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Idade
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Gerente
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Ações
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {employees.items.map((employee) => (
                      <tr key={employee.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm font-medium text-gray-900">
                            {employee.fullName}
                          </div>
                          <div className="text-sm text-gray-500">
                            {employee.documentNumber}
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                          {employee.email}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span
                            className={`px-2 py-1 text-xs font-semibold rounded-full ${getRoleColor(
                              employee.role
                            )}`}
                          >
                            {getRoleLabel(employee.role)}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                          {calculateAge(employee.birthDate)} anos
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {employee.managerName || '-'}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                          <div className="flex gap-2">
                            <button
                              onClick={() => navigate(`/employees/${employee.id}`)}
                              className="text-primary-600 hover:text-primary-900"
                            >
                              Ver
                            </button>
                            <button
                              onClick={() => navigate(`/employees/${employee.id}/edit`)}
                              className="text-blue-600 hover:text-blue-900"
                            >
                              Editar
                            </button>
                            <button
                              onClick={() => handleDelete(employee.id)}
                              className="text-red-600 hover:text-red-900"
                            >
                              Excluir
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Pagination */}
            {employees.totalPages > 1 && (
              <div className="mt-4 flex items-center justify-between">
                <div className="text-sm text-gray-700">
                  Mostrando {employees.firstItemIndex} a {employees.lastItemIndex} de{' '}
                  {employees.totalCount} resultados
                </div>
                <div className="flex gap-2">
                  <Button
                    variant="secondary"
                    onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                    disabled={!employees.hasPreviousPage}
                  >
                    Anterior
                  </Button>
                  <span className="px-4 py-2 text-sm">
                    Página {employees.pageNumber} de {employees.totalPages}
                  </span>
                  <Button
                    variant="secondary"
                    onClick={() => setCurrentPage((p) => p + 1)}
                    disabled={!employees.hasNextPage}
                  >
                    Próxima
                  </Button>
                </div>
              </div>
            )}
          </>
        ) : (
          <div className="card text-center py-12">
            <p className="text-gray-500">Nenhum funcionário encontrado</p>
          </div>
        )}
      </main>
    </div>
  );
};



import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuthStore } from '../features/auth/store/auth.store';
import { employeesService } from '../features/employees/services/employees.service';
import { Employee, EmployeeQueryParams, Role } from '../shared/types/api';
import { PagedResult } from '../shared/types/api';
import { Button } from '../shared/components/Button';
import { Input } from '../shared/components/Input';
import { Select } from '../shared/components/Select';
import { LoadingSpinner } from '../shared/components/LoadingSpinner';
import { ErrorAlert } from '../shared/components/ErrorAlert';
import { calculateAge } from '../shared/utils/date.utils';
import { getRoleLabel, getRoleColor, getHighestRole } from '../shared/utils/role.utils';
import { PlusIcon, MagnifyingGlassIcon, FunnelIcon, ChevronUpIcon, ChevronDownIcon, ArrowDownTrayIcon } from '@heroicons/react/24/outline';
import { exportToCSV, exportToExcel, fetchAllEmployeesForExport } from '../shared/utils/export.utils';

export const EmployeesPage = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();
  const [employees, setEmployees] = useState<PagedResult<Employee> | null>(null);
  const [allEmployees, setAllEmployees] = useState<Employee[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Filtros
  const [searchTerm, setSearchTerm] = useState('');
  const [filterName, setFilterName] = useState('');
  const [filterEmail, setFilterEmail] = useState('');
  const [filterRole, setFilterRole] = useState<Role | ''>('');
  const [filterManagerId, setFilterManagerId] = useState<string>('');
  const [sortBy, setSortBy] = useState<string>('');
  const [sortDescending, setSortDescending] = useState(false);
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false);
  
  const [currentPage, setCurrentPage] = useState(1);
  const [isExporting, setIsExporting] = useState(false);
  const pageSize = 10;

  // Carregar lista de funcionários para filtro de gerente
  useEffect(() => {
    const loadAllEmployees = async () => {
      try {
        const data = await employeesService.getEmployees({ pageSize: 1000 });
        setAllEmployees(data.items);
      } catch (err) {
        console.error('Erro ao carregar lista de funcionários:', err);
      }
    };
    loadAllEmployees();
  }, []);

  const loadEmployees = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const params: EmployeeQueryParams = {
        pageNumber: currentPage,
        pageSize,
        searchTerm: searchTerm || undefined,
        filterByName: filterName || undefined,
        filterByEmail: filterEmail || undefined,
        filterByRole: filterRole || undefined,
        filterByManagerId: filterManagerId || undefined,
        sortBy: sortBy || undefined,
        sortDescending: sortDescending || undefined,
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
  }, [currentPage]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setCurrentPage(1);
    loadEmployees();
  };

  const handleClearFilters = () => {
    setSearchTerm('');
    setFilterName('');
    setFilterEmail('');
    setFilterRole('');
    setFilterManagerId('');
    setSortBy('');
    setSortDescending(false);
    setCurrentPage(1);
  };

  const handleSort = (field: string) => {
    if (sortBy === field) {
      setSortDescending(!sortDescending);
    } else {
      setSortBy(field);
      setSortDescending(false);
    }
    setCurrentPage(1);
  };

  useEffect(() => {
    if (sortBy) {
      loadEmployees();
    }
  }, [sortBy, sortDescending]);

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

  const sortOptions = [
    { value: '', label: 'Sem ordenação' },
    { value: 'firstname', label: 'Nome' },
    { value: 'lastname', label: 'Sobrenome' },
    { value: 'email', label: 'Email' },
    { value: 'role', label: 'Função' },
    { value: 'createdat', label: 'Data de criação' },
  ];

  const managerOptions = [
    { value: '', label: 'Todos os gerentes' },
    ...allEmployees
      .filter(emp => emp.managerId === null)
      .map(emp => ({ value: emp.id, label: emp.fullName })),
  ];

  const getSortIcon = (field: string) => {
    if (sortBy !== field) return null;
    return sortDescending ? (
      <ChevronDownIcon className="h-4 w-4 inline ml-1" />
    ) : (
      <ChevronUpIcon className="h-4 w-4 inline ml-1" />
    );
  };

  const handleExportCSV = async () => {
    setIsExporting(true);
    try {
      const params: EmployeeQueryParams = {
        pageSize: 1000, // Buscar muitos registros
        searchTerm: searchTerm || undefined,
        filterByName: filterName || undefined,
        filterByEmail: filterEmail || undefined,
        filterByRole: filterRole || undefined,
        filterByManagerId: filterManagerId || undefined,
        sortBy: sortBy || undefined,
        sortDescending: sortDescending || undefined,
      };
      
      const allEmployees = await fetchAllEmployeesForExport(employeesService, params);
      exportToCSV(allEmployees, 'funcionarios');
    } catch (err: any) {
      alert(err.response?.data?.error || 'Erro ao exportar dados');
    } finally {
      setIsExporting(false);
    }
  };

  const handleExportExcel = async () => {
    setIsExporting(true);
    try {
      const params: EmployeeQueryParams = {
        pageSize: 1000,
        searchTerm: searchTerm || undefined,
        filterByName: filterName || undefined,
        filterByEmail: filterEmail || undefined,
        filterByRole: filterRole || undefined,
        filterByManagerId: filterManagerId || undefined,
        sortBy: sortBy || undefined,
        sortDescending: sortDescending || undefined,
      };
      
      const allEmployees = await fetchAllEmployeesForExport(employeesService, params);
      exportToExcel(allEmployees, 'funcionarios');
    } catch (err: any) {
      alert(err.response?.data?.error || 'Erro ao exportar dados');
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
          <div className="flex items-center gap-6">
            <h1 className="text-2xl font-bold text-gray-900">Funcionários</h1>
            {/* Link para Cargos - apenas para Admin */}
            {user?.roles?.some(r => r.toLowerCase() === 'admin') && (
              <Link
                to="/roles"
                className="text-blue-600 hover:text-blue-800 font-medium text-sm"
              >
                Gerenciar Cargos
              </Link>
            )}
          </div>
          <div className="flex items-center gap-4">
            <span className="text-sm text-gray-600">
              {user?.fullName} ({getHighestRole(user?.roles || [])})
            </span>
            <Button
              variant="secondary"
              onClick={() => navigate('/profile')}
            >
              Meu Perfil
            </Button>
            <Button variant="secondary" onClick={logout}>
              Sair
            </Button>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header com botão de adicionar e exportação */}
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-xl font-semibold text-gray-900">Lista de Funcionários</h2>
          <div className="flex items-center gap-3">
            {/* Botões de Exportação */}
            {employees && employees.items.length > 0 && (
              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={handleExportCSV}
                  disabled={isExporting}
                  className="flex items-center gap-2"
                >
                  <ArrowDownTrayIcon className="h-4 w-4" />
                  {isExporting ? 'Exportando...' : 'CSV'}
                </Button>
                <Button
                  type="button"
                  variant="secondary"
                  onClick={handleExportExcel}
                  disabled={isExporting}
                  className="flex items-center gap-2"
                >
                  <ArrowDownTrayIcon className="h-4 w-4" />
                  {isExporting ? 'Exportando...' : 'Excel'}
                </Button>
              </div>
            )}
            
            {/* Botão Adicionar */}
            {user?.roles && user.roles.length > 0 && (() => {
              const userRoleString = user.roles[0]?.toLowerCase();
              const allowedRoles = ['leader', 'director', 'admin'];
              const hasPermission = allowedRoles.includes(userRoleString || '');
              
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
        </div>

        {/* Filters */}
        <div className="card mb-6">
          <form onSubmit={handleSearch} className="space-y-4">
            {/* Filtros básicos */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="md:col-span-2">
                <Input
                  type="text"
                  placeholder="Buscar por nome, email ou documento..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
              </div>
              <div>
                <Select
                  options={roleOptions}
                  value={filterRole}
                  onChange={(e) => setFilterRole(e.target.value as Role | '')}
                />
              </div>
            </div>

            {/* Botão para mostrar filtros avançados */}
            <div className="flex items-center justify-between">
              <Button
                type="button"
                variant="secondary"
                onClick={() => setShowAdvancedFilters(!showAdvancedFilters)}
                className="flex items-center gap-2"
              >
                <FunnelIcon className="h-4 w-4" />
                {showAdvancedFilters ? 'Ocultar' : 'Mostrar'} Filtros Avançados
              </Button>
              <div className="flex gap-2">
                <Button type="button" variant="secondary" onClick={handleClearFilters}>
                  Limpar Filtros
                </Button>
                <Button type="submit" variant="primary" className="flex items-center gap-2">
                  <MagnifyingGlassIcon className="h-5 w-5" />
                  Buscar
                </Button>
              </div>
            </div>

            {/* Filtros avançados */}
            {showAdvancedFilters && (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 pt-4 border-t">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Filtrar por Nome
                  </label>
                  <Input
                    type="text"
                    placeholder="Nome específico..."
                    value={filterName}
                    onChange={(e) => setFilterName(e.target.value)}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Filtrar por Email
                  </label>
                  <Input
                    type="email"
                    placeholder="Email específico..."
                    value={filterEmail}
                    onChange={(e) => setFilterEmail(e.target.value)}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Filtrar por Gerente
                  </label>
                  <Select
                    options={managerOptions}
                    value={filterManagerId}
                    onChange={(e) => setFilterManagerId(e.target.value)}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Ordenar por
                  </label>
                  <Select
                    options={sortOptions}
                    value={sortBy}
                    onChange={(e) => setSortBy(e.target.value)}
                  />
                </div>
              </div>
            )}
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
                      <th
                        className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                        onClick={() => handleSort('firstname')}
                      >
                        Nome {getSortIcon('firstname')}
                      </th>
                      <th
                        className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                        onClick={() => handleSort('email')}
                      >
                        Email {getSortIcon('email')}
                      </th>
                      <th
                        className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                        onClick={() => handleSort('role')}
                      >
                        Função {getSortIcon('role')}
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

import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { employeesService } from '../features/employees/services/employees.service';
import { Employee } from '../shared/types/api';
import { Button } from '../shared/components/Button';
import { LoadingSpinner } from '../shared/components/LoadingSpinner';
import { ErrorAlert } from '../shared/components/ErrorAlert';
import { formatDate, calculateAge } from '../shared/utils/date.utils';
import { getRoleLabel, getRoleColor } from '../shared/utils/role.utils';

export const EmployeeDetailPage = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [employee, setEmployee] = useState<Employee | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (id) {
      loadEmployee(id);
    }
  }, [id]);

  const loadEmployee = async (employeeId: string) => {
    setIsLoading(true);
    try {
      const data = await employeesService.getEmployeeById(employeeId);
      setEmployee(data);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Erro ao carregar funcionário');
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center min-h-screen">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  if (error || !employee) {
    return (
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8">
          <ErrorAlert message={error || 'Funcionário não encontrado'} />
          <Button variant="secondary" onClick={() => navigate('/employees')} className="mt-4">
            Voltar
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="mb-6">
          <button
            onClick={() => navigate('/employees')}
            className="text-primary-600 hover:text-primary-800 mb-4"
          >
            ← Voltar
          </button>
          <h1 className="text-2xl font-bold text-gray-900">Detalhes do Funcionário</h1>
        </div>

        <div className="card">
          <div className="space-y-6">
            <div className="grid grid-cols-2 gap-6">
              <div>
                <label className="text-sm font-medium text-gray-500">Nome Completo</label>
                <p className="mt-1 text-lg text-gray-900">{employee.fullName}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Email</label>
                <p className="mt-1 text-lg text-gray-900">{employee.email}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">CPF</label>
                <p className="mt-1 text-lg text-gray-900">{employee.documentNumber}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Data de Nascimento</label>
                <p className="mt-1 text-lg text-gray-900">
                  {formatDate(employee.birthDate)} ({calculateAge(employee.birthDate)} anos)
                </p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Função</label>
                <p className="mt-1">
                  <span
                    className={`px-3 py-1 text-sm font-semibold rounded-full ${getRoleColor(
                      employee.role
                    )}`}
                  >
                    {getRoleLabel(employee.role)}
                  </span>
                </p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Gerente</label>
                <p className="mt-1 text-lg text-gray-900">
                  {employee.managerName || '-'}
                </p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Telefones</label>
                <p className="mt-1 text-lg text-gray-900">
                  {employee.phoneNumbers.length > 0
                    ? employee.phoneNumbers.join(', ')
                    : '-'}
                </p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Data de Criação</label>
                <p className="mt-1 text-lg text-gray-900">
                  {formatDate(employee.createdAt, 'dd/MM/yyyy HH:mm')}
                </p>
              </div>
            </div>

            <div className="pt-6 border-t">
              <div className="flex gap-4">
                <Button
                  variant="primary"
                  onClick={() => navigate(`/employees/${employee.id}/edit`)}
                >
                  Editar
                </Button>
                <Button variant="secondary" onClick={() => navigate('/employees')}>
                  Voltar
                </Button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};



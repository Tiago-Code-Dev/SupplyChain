import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { employeesService } from '../features/employees/services/employees.service';
import { rolesService } from '../features/roles/services/roles.service';
import { CreateEmployeeRequest, UpdateEmployeeRequest, Role, CustomRole } from '../shared/types/api';
import { Button } from '../shared/components/Button';
import { Input } from '../shared/components/Input';
import { MaskedInput } from '../shared/components/MaskedInput';
import { Select } from '../shared/components/Select';
import { ErrorAlert } from '../shared/components/ErrorAlert';
import { SuccessAlert } from '../shared/components/SuccessAlert';
import { LoadingSpinner } from '../shared/components/LoadingSpinner';
import { unformatCPF, formatPhone, unformatPhoneList } from '../shared/utils/format.utils';
import { useAuthStore } from '../features/auth/store/auth.store';
import { getHighestRoleKey } from '../shared/utils/role.utils';

const createEmployeeSchema = z.object({
  firstName: z.string().min(2, 'Nome deve ter pelo menos 2 caracteres'),
  lastName: z.string().min(2, 'Sobrenome deve ter pelo menos 2 caracteres'),
  email: z.string().email('Email inválido'),
  documentNumber: z
    .string()
    .min(1, 'CPF é obrigatório')
    .refine(
      (val) => {
        const numbers = val.replace(/\D/g, '');
        return numbers.length === 11;
      },
      { message: 'CPF deve ter 11 dígitos' }
    ),
  birthDate: z
    .string()
    .min(1, 'Data de nascimento é obrigatória')
    .refine(
      (date) => {
        const birthDate = new Date(date);
        const today = new Date();
        const age = today.getFullYear() - birthDate.getFullYear();
        const monthDiff = today.getMonth() - birthDate.getMonth();
        const actualAge = monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate()) ? age - 1 : age;
        return actualAge >= 18;
      },
      { message: 'O funcionário deve ter pelo menos 18 anos' }
    ),
  password: z
    .string()
    .min(8, 'Senha deve ter pelo menos 8 caracteres')
    .regex(/[A-Z]/, 'Senha deve conter pelo menos uma letra maiúscula')
    .regex(/[a-z]/, 'Senha deve conter pelo menos uma letra minúscula')
    .regex(/[0-9]/, 'Senha deve conter pelo menos um número')
    .regex(/[^A-Za-z0-9]/, 'Senha deve conter pelo menos um caractere especial'),
  customRoleId: z.string().min(1, 'Função é obrigatória'),
  managerId: z.string().optional().nullable(),
  phoneNumbers: z.string().optional(),
});

const updateEmployeeSchema = z.object({
  firstName: z.string().min(2, 'Nome deve ter pelo menos 2 caracteres'),
  lastName: z.string().min(2, 'Sobrenome deve ter pelo menos 2 caracteres'),
  email: z.string().email('Email inválido'),
  birthDate: z.string().min(1, 'Data de nascimento é obrigatória'),
  customRoleId: z.string().min(1, 'Função é obrigatória'),
  managerId: z.string().optional().nullable(),
  phoneNumbers: z.string().optional(),
});

type CreateEmployeeFormData = z.infer<typeof createEmployeeSchema>;
type UpdateEmployeeFormData = z.infer<typeof updateEmployeeSchema>;

export const EmployeeFormPage = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [customRoles, setCustomRoles] = useState<CustomRole[]>([]);
  const [allEmployees, setAllEmployees] = useState<{ id: string; fullName: string; roleDisplayName: string; role: Role }[]>([]);
  const [currentEmployeeRole, setCurrentEmployeeRole] = useState<Role | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
  } = useForm<CreateEmployeeFormData>({
    resolver: zodResolver(isEdit ? updateEmployeeSchema : createEmployeeSchema) as any,
    defaultValues: {
      customRoleId: '',
      managerId: '',
    },
  });

  const { user, checkAuth } = useAuthStore();

    // Carregar roles customizados e lista de funcionários para superior
  useEffect(() => {
    const loadInitialData = async () => {
      try {
        const [roles, employees] = await Promise.all([
          rolesService.getAllRoles(),
          employeesService.getEmployees({ pageSize: 1000 })
        ]);
        setCustomRoles(roles);
        setAllEmployees(employees.items.map(e => ({ 
          id: e.id, 
          fullName: e.fullName, 
          roleDisplayName: e.roleDisplayName,
          role: e.role
        })));
      } catch (err) {
        console.error('Erro ao carregar dados:', err);
      }
    };
    loadInitialData();
  }, []);

  useEffect(() => {
    if (isEdit && id) {
      loadEmployee(id);
    }
  }, [isEdit, id]);

  const loadEmployee = async (employeeId: string) => {
    setIsLoading(true);
    try {
      const employee = await employeesService.getEmployeeById(employeeId);
      setValue('firstName', employee.firstName);
      setValue('lastName', employee.lastName);
      setValue('email', employee.email);
      setValue('birthDate', employee.birthDate.split('T')[0]);
      setValue('customRoleId', employee.customRoleId || '');
      setValue('managerId', employee.managerId || '');
      // Guardar o role do funcionário para filtrar superiores válidos
      setCurrentEmployeeRole(employee.role);
      // Formatar telefones ao carregar
      if (employee.phoneNumbers && employee.phoneNumbers.length > 0) {
        const formattedPhones = employee.phoneNumbers
          .map(phone => formatPhone(phone))
          .join(', ');
        setValue('phoneNumbers', formattedPhones);
      }
    } catch (err: unknown) {
      let message = 'Erro ao carregar funcionário';
      if (
        typeof err === 'object' &&
        err !== null &&
        'response' in err &&
        typeof (err as { response?: unknown }).response === 'object' &&
        (err as { response?: { data?: { error?: string } } }).response &&
        (err as { response: { data?: { error?: string } } }).response.data &&
        typeof (err as { response: { data: { error?: string } } }).response.data.error === 'string'
      ) {
        message = (err as { response: { data: { error: string } } }).response.data.error;
      }
      setError(message);
    } finally {
      setIsLoading(false);
    }
  };

  // Filtrar funcionários que podem ser superiores hierárquicos
  // Só mostra quem tem role MAIOR que o funcionário sendo editado
  const getValidManagers = () => {
    if (!currentEmployeeRole) return [];

    return allEmployees
      .filter(emp => {
        // Não pode ser superior de si mesmo
        if (emp.id === id) return false;
        // Só pode ter como superior quem tem role MAIOR
        // Role: Employee=1, Leader=2, Director=3, Admin=4
        return emp.role > currentEmployeeRole;
      })
      .map(emp => ({ 
        value: emp.id, 
        label: `${emp.fullName} (${emp.roleDisplayName})` 
      }));
  };

  // Converter hierarchyLevel para o enum Role correspondente
  const hierarchyToRole = (hierarchyLevel: number): Role => {
    if (hierarchyLevel >= 100) return Role.Admin;
    if (hierarchyLevel >= 30) return Role.Director;
    if (hierarchyLevel >= 20) return Role.Leader;
    return Role.Employee;
  };

  // Obter o role legado baseado no customRoleId selecionado
  const getRoleFromCustomRoleId = (customRoleId: string): Role => {
    const selectedRole = customRoles.find(r => r.id === customRoleId);
    if (!selectedRole) return Role.Employee;
    return hierarchyToRole(selectedRole.hierarchyLevel);
  };

  const onSubmit = async (data: CreateEmployeeFormData | UpdateEmployeeFormData) => {
    setIsLoading(true);
    setError(null);
    setSuccess(false);

    try {
      // Remover formatação dos telefones
      const phoneNumbers = data.phoneNumbers
        ? unformatPhoneList(data.phoneNumbers)
        : [];

      // Obter o role legado baseado no customRoleId selecionado
      const legacyRole = getRoleFromCustomRoleId(data.customRoleId);

      if (isEdit && id) {
        const updateData: UpdateEmployeeRequest = {
          firstName: data.firstName,
          lastName: data.lastName,
          email: data.email,
          birthDate: data.birthDate,
          role: legacyRole,
          managerId: data.managerId || null,
          phoneNumbers: phoneNumbers.length > 0 ? phoneNumbers : undefined,
          customRoleId: data.customRoleId,
        };
        await employeesService.updateEmployee(id, updateData);

        // Se o funcionário editado é o próprio usuário logado, atualizar o estado de autenticação
        if (user?.employeeId === id) {
          await checkAuth();
        }
      } else {
        const createData = data as CreateEmployeeFormData;
        // Remover formatação do CPF
        const documentNumber = unformatCPF(createData.documentNumber);

        const createRequest: CreateEmployeeRequest = {
          firstName: createData.firstName,
          lastName: createData.lastName,
          email: createData.email,
          documentNumber: documentNumber,
          birthDate: createData.birthDate,
          password: createData.password,
          role: legacyRole,
          managerId: createData.managerId || null,
          phoneNumbers: phoneNumbers.length > 0 ? phoneNumbers : undefined,
          customRoleId: createData.customRoleId,
        };
        await employeesService.createEmployee(createRequest);
      }

      setSuccess(true);
      setTimeout(() => {
        navigate('/employees');
      }, 1500);
    } catch (err: unknown) {
      let message = 'Erro ao salvar funcionário';
      if (
        typeof err === 'object' &&
        err !== null &&
        'response' in err &&
        typeof (err as { response?: unknown }).response === 'object' &&
        (err as { response?: { data?: { error?: string } } }).response &&
        (err as { response: { data?: { error?: string } } }).response.data &&
        typeof (err as { response: { data: { error?: string } } }).response.data.error === 'string'
      ) {
        message = (err as { response: { data: { error: string } } }).response.data.error;
      }
      setError(message);
    } finally {
      setIsLoading(false);
    }
  };

  // Filtrar opções baseado no role do usuário, usando CustomRoles da API
  const getRoleOptions = () => {
    // Converter customRoles para opções de select usando o ID como value
    const allRoles = customRoles
      .sort((a, b) => b.hierarchyLevel - a.hierarchyLevel)
      .map(role => ({
        value: role.id,
        label: role.displayName,
        hierarchyLevel: role.hierarchyLevel
      }));

    // Se não há customRoles carregados ainda, retornar vazio
    if (allRoles.length === 0) {
      return [];
    }

    const userRoleString = getHighestRoleKey(user?.roles || []);

    // Admin pode criar qualquer role
    if (userRoleString === 'Admin') {
      return allRoles;
    }

    // Director pode criar roles com hierarchyLevel <= 30
    if (userRoleString === 'Director') {
      return allRoles.filter(r => r.hierarchyLevel <= 30);
    }

    // Leader só pode criar roles com hierarchyLevel <= 10
    if (userRoleString === 'Leader') {
      return allRoles.filter(r => r.hierarchyLevel <= 10);
    }

    // Employee não pode criar ninguém
    return [];
  };

  const roleOptions = getRoleOptions();

  if (isLoading && isEdit) {
    return (
      <div className="flex justify-center items-center min-h-screen">
        <LoadingSpinner size="lg" />
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
          <h1 className="text-2xl font-bold text-gray-900">
            {isEdit ? 'Editar Funcionário' : 'Novo Funcionário'}
          </h1>
        </div>

        <div className="card">
          {error && <ErrorAlert message={error} />}
          {success && <SuccessAlert message="Funcionário salvo com sucesso!" />}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
            <div className="grid grid-cols-2 gap-4">
              <Input
                {...register('firstName')}
                label="Nome"
                error={errors.firstName?.message}
              />
              <Input
                {...register('lastName')}
                label="Sobrenome"
                error={errors.lastName?.message}
              />
            </div>

            <Input
              {...register('email')}
              type="email"
              label="Email"
              error={errors.email?.message}
            />

            {/* Campo documentNumber - só aparece no create */}
            {!isEdit && (
              <MaskedInput
                label="CPF"
                mask="cpf"
                {...register('documentNumber')}
                error={(errors as any).documentNumber?.message}
                required
              />
            )}

            <div>
              <Input
                {...register('birthDate')}
                type="date"
                label="Data de Nascimento"
                error={errors.birthDate?.message}
                max={new Date(new Date().setFullYear(new Date().getFullYear() - 18)).toISOString().split('T')[0]}
              />
              <p className="mt-1 text-xs text-gray-500">
                O funcionário deve ter pelo menos 18 anos
              </p>
            </div>

            {/* Campo password - só aparece no create */}
            {!isEdit && (
              <Input
                label="Senha"
                type="password"
                {...register('password')}
                error={(errors as any).password?.message}
                required
              />
            )}

            <Select
              {...register('customRoleId')}
              label="Função"
              options={roleOptions}
              error={(errors as any).customRoleId?.message}
            />

            {/* Campo Superior Hierárquico - só aparece no edit */}
            {isEdit && (
              <div>
                <Select
                  {...register('managerId')}
                  label="Superior Hierárquico"
                  options={[
                    { value: '', label: 'Nenhum (sem superior)' },
                    ...getValidManagers()
                  ]}
                />
                <p className="mt-1 text-xs text-gray-500">
                  {getValidManagers().length > 0 
                    ? 'Selecione quem será o superior hierárquico deste funcionário'
                    : 'Não há funcionários com cargo superior disponíveis'}
                </p>
              </div>
            )}

            <div>
              <MaskedInput
                {...register('phoneNumbers')}
                mask="phoneList"
                label="Telefones (separados por vírgula)"
                placeholder="(11) 99999-9999, (11) 88888-8888"
              />
              <p className="mt-1 text-xs text-gray-500">
                Digite os números separados por vírgula (ex: (11) 99999-9999, (11) 88888-8888)
              </p>
            </div>

            <div className="flex justify-end gap-4">
              <Button
                type="button"
                variant="secondary"
                onClick={() => navigate('/employees')}
              >
                Cancelar
              </Button>
              <Button type="submit" isLoading={isLoading}>
                Salvar
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};



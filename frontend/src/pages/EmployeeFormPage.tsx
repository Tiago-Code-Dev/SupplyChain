import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { employeesService } from '../features/employees/services/employees.service';
import { CreateEmployeeRequest, UpdateEmployeeRequest, Role } from '../shared/types/api';
import { Button } from '../shared/components/Button';
import { Input } from '../shared/components/Input';
import { MaskedInput } from '../shared/components/MaskedInput';
import { Select } from '../shared/components/Select';
import { ErrorAlert } from '../shared/components/ErrorAlert';
import { SuccessAlert } from '../shared/components/SuccessAlert';
import { LoadingSpinner } from '../shared/components/LoadingSpinner';
import { unformatCPF, formatPhone, unformatPhoneList } from '../shared/utils/format.utils';
import { useAuthStore } from '../features/auth/store/auth.store';
import { getHighestRole } from '../shared/utils/role.utils';  // ✅ Adicionar esta linha

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
  role: z.nativeEnum(Role),
  managerId: z.string().optional().nullable(),
  phoneNumbers: z.string().optional(),
});

const updateEmployeeSchema = z.object({
  firstName: z.string().min(2, 'Nome deve ter pelo menos 2 caracteres'),
  lastName: z.string().min(2, 'Sobrenome deve ter pelo menos 2 caracteres'),
  email: z.string().email('Email inválido'),
  birthDate: z.string().min(1, 'Data de nascimento é obrigatória'),
  role: z.nativeEnum(Role),
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

  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
  } = useForm<CreateEmployeeFormData>({
    resolver: zodResolver(isEdit ? updateEmployeeSchema : createEmployeeSchema) as any,
    defaultValues: {
      role: Role.Employee,
    },
  });

  const { user } = useAuthStore();

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
      setValue('role', employee.role);
      setValue('managerId', employee.managerId || '');
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

  const onSubmit = async (data: CreateEmployeeFormData | UpdateEmployeeFormData) => {
    setIsLoading(true);
    setError(null);
    setSuccess(false);

    try {
      // Remover formatação dos telefones
      const phoneNumbers = data.phoneNumbers
        ? unformatPhoneList(data.phoneNumbers)
        : [];

      if (isEdit && id) {
        const updateData: UpdateEmployeeRequest = {
          firstName: data.firstName,
          lastName: data.lastName,
          email: data.email,
          birthDate: data.birthDate,
          role: data.role,
          managerId: data.managerId || null,
          phoneNumbers: phoneNumbers.length > 0 ? phoneNumbers : undefined,
        };
        await employeesService.updateEmployee(id, updateData);
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
          role: createData.role,
          managerId: createData.managerId || null,
          phoneNumbers: phoneNumbers.length > 0 ? phoneNumbers : undefined,
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

  // Filtrar opções baseado no role do usuário
  const getRoleOptions = () => {
    const allRoles = [
      { value: Role.Employee, label: 'Funcionário' },
      { value: Role.Leader, label: 'Líder' },
      { value: Role.Director, label: 'Diretor' },
      { value: Role.Admin, label: 'Administrador' },
    ];
    
    const userRoleString = getHighestRole(user?.roles || []);
    
    // Admin pode criar qualquer role
    if (userRoleString === 'Admin') {
      return allRoles;
    }
    
    // Director pode criar Director, Leader, Employee
    if (userRoleString === 'Director') {
      return allRoles.filter(r => r.value <= Role.Director);
    }
    
    // Leader só pode criar Employee
    if (userRoleString === 'Leader') {
      return allRoles.filter(r => r.value === Role.Employee);
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
              {...register('role', { valueAsNumber: true })}
              label="Função"
              options={roleOptions}
              error={errors.role?.message}
            />

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



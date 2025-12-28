import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuthStore } from '../features/auth/store/auth.store';
import { authService } from '../features/auth/services/auth.service';
import { Button } from '../shared/components/Button';
import { Input } from '../shared/components/Input';
import { ErrorAlert } from '../shared/components/ErrorAlert';
import { SuccessAlert } from '../shared/components/SuccessAlert';

const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Senha atual é obrigatória'),
    newPassword: z
      .string()
      .min(8, 'Senha deve ter pelo menos 8 caracteres')
      .regex(/[A-Z]/, 'Senha deve conter pelo menos uma letra maiúscula')
      .regex(/[a-z]/, 'Senha deve conter pelo menos uma letra minúscula')
      .regex(/[0-9]/, 'Senha deve conter pelo menos um número')
      .regex(/[^a-zA-Z0-9]/, 'Senha deve conter pelo menos um caractere especial'),
    confirmPassword: z.string(),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'As senhas não coincidem',
    path: ['confirmPassword'],
  })
  .refine((data) => data.currentPassword !== data.newPassword, {
    message: 'A nova senha deve ser diferente da senha atual',
    path: ['newPassword'],
  });

type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;

export const ChangePasswordPage = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ChangePasswordFormData>({
    resolver: zodResolver(changePasswordSchema),
  });

  const onSubmit = async (data: ChangePasswordFormData) => {
    setIsLoading(true);
    setError(null);
    setSuccess(false);

    try {
      await authService.changePassword({
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
        confirmPassword: data.confirmPassword,
      });
      setSuccess(true);
      setTimeout(() => {
        logout();
        navigate('/login');
      }, 2000);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Erro ao alterar senha');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-gray-900">Alterar Senha</h1>
          <Button variant="secondary" onClick={() => navigate('/employees')}>
            Voltar
          </Button>
        </div>
      </header>

      <main className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="card">
          {success ? (
            <div className="space-y-4">
              <SuccessAlert message="Senha alterada com sucesso! Você será redirecionado para fazer login novamente." />
            </div>
          ) : (
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
              {error && <ErrorAlert message={error} />}

              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4">
                  Informações da conta
                </h2>
                <div className="space-y-2 mb-4">
                  <p className="text-sm text-gray-600">
                    <span className="font-medium">Email:</span> {user?.email}
                  </p>
                  <p className="text-sm text-gray-600">
                    <span className="font-medium">Nome:</span> {user?.fullName}
                  </p>
                </div>
              </div>

              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4">
                  Alterar senha
                </h2>
                <div className="space-y-4">
                  <Input
                    {...register('currentPassword')}
                    type="password"
                    label="Senha atual"
                    placeholder="••••••••"
                    error={errors.currentPassword?.message}
                    autoFocus
                  />

                  <Input
                    {...register('newPassword')}
                    type="password"
                    label="Nova senha"
                    placeholder="••••••••"
                    error={errors.newPassword?.message}
                  />

                  <div>
                    <Input
                      {...register('confirmPassword')}
                      type="password"
                      label="Confirmar nova senha"
                      placeholder="••••••••"
                      error={errors.confirmPassword?.message}
                    />
                    <p className="mt-1 text-xs text-gray-500">
                      A senha deve ter pelo menos 8 caracteres, incluindo letras maiúsculas, minúsculas, números e caracteres especiais.
                    </p>
                  </div>
                </div>
              </div>

              <div className="flex gap-4">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => navigate('/employees')}
                  className="flex-1"
                >
                  Cancelar
                </Button>
                <Button type="submit" isLoading={isLoading} className="flex-1">
                  Alterar senha
                </Button>
              </div>
            </form>
          )}
        </div>
      </main>
    </div>
  );
};


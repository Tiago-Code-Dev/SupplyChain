import { useState, useEffect } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { authService } from '../features/auth/services/auth.service';
import { Button } from '../shared/components/Button';
import { Input } from '../shared/components/Input';
import { ErrorAlert } from '../shared/components/ErrorAlert';
import { SuccessAlert } from '../shared/components/SuccessAlert';

const resetPasswordSchema = z
  .object({
    email: z.string().email('Email inválido'),
    token: z.string().min(1, 'Token é obrigatório'),
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
  });

type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

export const ResetPasswordPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const email = searchParams.get('email') || '';
  const token = searchParams.get('token') || '';

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      email: email,
      token: token,
    },
  });

  useEffect(() => {
    if (email) setValue('email', email);
    if (token) setValue('token', token);
  }, [email, token, setValue]);

  const onSubmit = async (data: ResetPasswordFormData) => {
    setIsLoading(true);
    setError(null);
    setSuccess(false);

    try {
      await authService.resetPassword({
        email: data.email,
        token: data.token,
        newPassword: data.newPassword,
      });
      setSuccess(true);
      setTimeout(() => {
        navigate('/login');
      }, 2000);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Erro ao redefinir senha');
    } finally {
      setIsLoading(false);
    }
  };

  if (success) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8">
          <SuccessAlert message="Senha redefinida com sucesso! Redirecionando para o login..." />
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Redefinir senha
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Digite sua nova senha
          </p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit(onSubmit)}>
          {error && <ErrorAlert message={error} />}

          <div className="space-y-4">
            <Input
              {...register('email')}
              type="email"
              label="Email"
              placeholder="seu@email.com"
              error={errors.email?.message}
              disabled={!!email}
            />

            <Input
              {...register('token')}
              type="text"
              label="Token"
              placeholder="Token de redefinição"
              error={errors.token?.message}
              disabled={!!token}
            />

            <Input
              {...register('newPassword')}
              type="password"
              label="Nova senha"
              placeholder="••••••••"
              error={errors.newPassword?.message}
              autoFocus
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

          <div>
            <Button type="submit" isLoading={isLoading} className="w-full">
              Redefinir senha
            </Button>
          </div>

          <div className="text-center">
            <Link
              to="/login"
              className="text-primary-600 hover:text-primary-900 text-sm font-medium"
            >
              Voltar para o login
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
};


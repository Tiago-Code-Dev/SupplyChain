import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { authService } from '../features/auth/services/auth.service';
import { Button } from '../shared/components/Button';
import { Input } from '../shared/components/Input';
import { ErrorAlert } from '../shared/components/ErrorAlert';
import { SuccessAlert } from '../shared/components/SuccessAlert';

const forgotPasswordSchema = z.object({
  email: z.string().email('Email inválido'),
});

type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;

export const ForgotPasswordPage = () => {
  const navigate = useNavigate();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
  });

  const onSubmit = async (data: ForgotPasswordFormData) => {
    setIsLoading(true);
    setError(null);
    setSuccess(false);

    try {
      await authService.forgotPassword({ email: data.email });
      setSuccess(true);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Erro ao solicitar redefinição de senha');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Esqueci minha senha
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Digite seu email para receber instruções de redefinição de senha
          </p>
        </div>

        {success ? (
          <div className="space-y-4">
            <SuccessAlert message="Se o email existir, um link de redefinição de senha será enviado." />
            <div className="text-center">
              <Link
                to="/login"
                className="text-primary-600 hover:text-primary-900 text-sm font-medium"
              >
                Voltar para o login
              </Link>
            </div>
          </div>
        ) : (
          <form className="mt-8 space-y-6" onSubmit={handleSubmit(onSubmit)}>
            {error && <ErrorAlert message={error} />}

            <div className="space-y-4">
              <Input
                {...register('email')}
                type="email"
                label="Email"
                placeholder="seu@email.com"
                error={errors.email?.message}
                autoFocus
              />
            </div>

            <div>
              <Button type="submit" isLoading={isLoading} className="w-full">
                Enviar instruções
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
        )}
      </div>
    </div>
  );
};


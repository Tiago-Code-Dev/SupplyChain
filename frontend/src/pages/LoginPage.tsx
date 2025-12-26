import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuthStore } from '../features/auth/store/auth.store';
import { Button } from '../shared/components/Button';
import { Input } from '../shared/components/Input';
import { ErrorAlert } from '../shared/components/ErrorAlert';

const loginSchema = z.object({
  email: z.string().email('Email inválido'),
  password: z.string().min(1, 'Senha é obrigatória'),
});

type LoginFormData = z.infer<typeof loginSchema>;

export const LoginPage = () => {
  const navigate = useNavigate();
  const { login, isAuthenticated, error, clearError } = useAuthStore();
  const [isLoading, setIsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  useEffect(() => {
    if (isAuthenticated) {
      navigate('/employees');
    }
  }, [isAuthenticated, navigate]);

  const onSubmit = async (data: LoginFormData) => {
    setIsLoading(true);
    clearError();
    try {
      await login(data.email, data.password);
      navigate('/employees');
    } catch (error: any) {
      console.error('Login error:', error);
      // Tratar erro 429 (Rate Limiting) especificamente
      if (error?.response?.status === 429) {
        const retryAfter = error.response?.data?.tentarNovamenteEm || '60 segundos';
        const errorMessage = error.response?.data?.detalhe || 'Muitas tentativas de login. Por favor, aguarde antes de tentar novamente.';
        // Não definir erro aqui, pois o auth.store já trata
        // Mas podemos melhorar a mensagem se necessário
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Employee Management
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Faça login para acessar o sistema
          </p>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit(onSubmit)}>
          {error && (
            <ErrorAlert message={error} onDismiss={clearError} />
          )}
          <div className="space-y-4">
            <Input
              {...register('email')}
              type="email"
              label="Email"
              placeholder="seu@email.com"
              error={errors.email?.message}
            />
            <Input
              {...register('password')}
              type="password"
              label="Senha"
              placeholder="••••••••"
              error={errors.password?.message}
            />
          </div>
          <div>
            <Button type="submit" isLoading={isLoading} className="w-full">
              Entrar
            </Button>
          </div>
          <div className="text-sm text-center space-y-2">
            <div>
              <Link
                to="/forgot-password"
                className="text-primary-600 hover:text-primary-900 font-medium"
              >
                Esqueci minha senha
              </Link>
            </div>
            <div className="text-gray-600">
              <p>Credenciais padrão:</p>
              <p className="font-mono text-xs mt-1">admin@empresa.com / Admin@123</p>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
};



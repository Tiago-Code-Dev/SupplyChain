import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../features/auth/store/auth.store';
import { authService } from '../features/auth/services/auth.service';
import { UserInfo } from '../shared/types/api';
import { Button } from '../shared/components/Button';
import { LoadingSpinner } from '../shared/components/LoadingSpinner';
import { ErrorAlert } from '../shared/components/ErrorAlert';
import { getRoleLabel } from '../shared/utils/role.utils';
import { UserIcon, KeyIcon, EnvelopeIcon, IdentificationIcon } from '@heroicons/react/24/outline';

export const ProfilePage = () => {
  const navigate = useNavigate();
  const { user } = useAuthStore();
  const [userInfo, setUserInfo] = useState<UserInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadUserInfo = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const info = await authService.getCurrentUser();
        setUserInfo(info);
      } catch (err: unknown) {
        if (
          typeof err === 'object' &&
          err !== null &&
          'response' in err &&
          typeof (err as { response?: { data?: { error?: string } } }).response === 'object' &&
          (err as { response?: { data?: { error?: string } } }).response !== null &&
          'data' in (err as { response?: { data?: { error?: string } } }).response!
        ) {
          setError(
            ((err as { response?: { data?: { error?: string } } }).response as { data?: { error?: string } }).data?.error ||
              'Erro ao carregar informações do usuário'
          );
        } else {
          setError('Erro ao carregar informações do usuário');
        }
      } finally {
        setIsLoading(false);
      }
    };

    loadUserInfo();
  }, []);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  if (error && !userInfo) {
    return (
      <div className="min-h-screen bg-gray-50">
        <header className="bg-white shadow">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
            <Button variant="secondary" onClick={() => navigate('/employees')}>
              Voltar
            </Button>
          </div>
        </header>
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <ErrorAlert message={error} />
        </main>
      </div>
    );
  }

  const currentUser = userInfo || user;

  // Formata os roles para exibição
  const formatRoles = (roles?: string[]): string => {
    if (!roles || roles.length === 0) return '-';
    return roles.map(role => getRoleLabel(role) || role).join(', ');
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-gray-900">Meu Perfil</h1>
          <Button variant="secondary" onClick={() => navigate('/employees')}>
            Voltar
          </Button>
        </div>
      </header>

      <main className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {error && <ErrorAlert message={error} className="mb-6" />}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Informações Pessoais */}
          <div className="lg:col-span-2 space-y-6">
            {/* Card de Informações Básicas */}
            <div className="card">
              <div className="flex items-center gap-3 mb-6">
                <div className="p-3 bg-primary-100 rounded-lg">
                  <UserIcon className="h-6 w-6 text-primary-600" />
                </div>
                <h2 className="text-xl font-semibold text-gray-900">Informações Pessoais</h2>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Nome
                  </label>
                  <div className="flex items-center gap-2 text-gray-900">
                    <IdentificationIcon className="h-5 w-5 text-gray-400" />
                    <span>{currentUser?.firstName || '-'}</span>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Sobrenome
                  </label>
                  <div className="flex items-center gap-2 text-gray-900">
                    <IdentificationIcon className="h-5 w-5 text-gray-400" />
                    <span>{currentUser?.lastName || '-'}</span>
                  </div>
                </div>

                <div className="md:col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Nome Completo
                  </label>
                  <div className="flex items-center gap-2 text-gray-900">
                    <IdentificationIcon className="h-5 w-5 text-gray-400" />
                    <span>{currentUser?.fullName || `${currentUser?.firstName || ''} ${currentUser?.lastName || ''}`.trim() || '-'}</span>
                  </div>
                </div>

                <div className="md:col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    E-mail
                  </label>
                  <div className="flex items-center gap-2 text-gray-900">
                    <EnvelopeIcon className="h-5 w-5 text-gray-400" />
                    <span>{currentUser?.email || '-'}</span>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Status
                  </label>
                  <div className="flex items-center gap-2 text-gray-900">
                    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                      currentUser?.isActive 
                        ? 'bg-green-100 text-green-800' 
                        : 'bg-red-100 text-red-800'
                    }`}>
                      {currentUser?.isActive ? 'Ativo' : 'Inativo'}
                    </span>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Cargo(s)
                  </label>
                  <div className="text-gray-900">
                    {formatRoles(currentUser?.roles)}
                  </div>
                </div>
              </div>
            </div>

            {/* Card de Informações Adicionais */}
            <div className="card">
              <div className="flex items-center gap-3 mb-6">
                <div className="p-3 bg-primary-100 rounded-lg">
                  <KeyIcon className="h-6 w-6 text-primary-600" />
                </div>
                <h2 className="text-xl font-semibold text-gray-900">Informações Adicionais</h2>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    ID do Usuário
                  </label>
                  <div className="text-gray-900 text-sm font-mono">
                    {currentUser?.id || '-'}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    ID do Funcionário
                  </label>
                  <div className="text-gray-900 text-sm font-mono">
                    {currentUser?.employeeId || '-'}
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Atalhos Rápidos */}
          <div className="hidden lg:block lg:col-span-1">
            <div className="card h-full">
              <div className="flex flex-col gap-4 h-full p-6">
                <h2 className="text-xl font-semibold text-gray-900">Atalhos Rápidos</h2>

                <div className="flex-grow">
                  <div className="grid grid-cols-2 gap-4">
                    <Button variant="primary" onClick={() => navigate('/change-password')}>
                      Alterar Senha
                    </Button>
                    <Button variant="secondary" onClick={() => navigate('/logout')}>
                      Sair
                    </Button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};


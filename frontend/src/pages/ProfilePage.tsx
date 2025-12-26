import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../features/auth/store/auth.store';
import { authService } from '../features/auth/services/auth.service';
import { UserInfo } from '../shared/types/api';
import { Button } from '../shared/components/Button';
import { LoadingSpinner } from '../shared/components/LoadingSpinner';
import { ErrorAlert } from '../shared/components/ErrorAlert';
import { formatDate } from '../shared/utils/date.utils';
import { getRoleLabel } from '../shared/utils/role.utils';
import { UserIcon, KeyIcon, EnvelopeIcon, IdentificationIcon } from '@heroicons/react/24/outline';

export const ProfilePage = () => {
  const navigate = useNavigate();
  const { user, checkAuth } = useAuthStore();
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
      } catch (err: any) {
        setError(err.response?.data?.error || 'Erro ao carregar informações do usuário');
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
                  <div className="text-gray-900">
                    <span>{currentUser?.fullName || '-'}</span>
                  </div>
                </div>

                <div className="md:col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Email
                  </label>
                  <div className="flex items-center gap-2 text-gray-900">
                    <EnvelopeIcon className="h-5 w-5 text-gray-400" />
                    <span>{currentUser?.email || '-'}</span>
                  </div>
                </div>
              </div>
            </div>

            {/* Card de Informações da Conta */}
            <div className="card">
              <div className="flex items-center gap-3 mb-6">
                <div className="p-3 bg-blue-100 rounded-lg">
                  <KeyIcon className="h-6 w-6 text-blue-600" />
                </div>
                <h2 className="text-xl font-semibold text-gray-900">Informações da Conta</h2>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    ID do Usuário
                  </label>
                  <div className="text-sm text-gray-600 font-mono">
                    {currentUser?.id || '-'}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    ID do Funcionário
                  </label>
                  <div className="text-sm text-gray-600 font-mono">
                    {currentUser?.employeeId || 'Não vinculado'}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Status
                  </label>
                  <div>
                    <span
                      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                        currentUser?.isActive
                          ? 'bg-green-100 text-green-800'
                          : 'bg-red-100 text-red-800'
                      }`}
                    >
                      {currentUser?.isActive ? 'Ativo' : 'Inativo'}
                    </span>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Funções (Roles)
                  </label>
                  <div className="flex flex-wrap gap-2">
                    {currentUser?.roles && currentUser.roles.length > 0 ? (
                      currentUser.roles.map((role, index) => (
                        <span
                          key={index}
                          className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-primary-100 text-primary-800"
                        >
                          {getRoleLabel(role)}
                        </span>
                      ))
                    ) : (
                      <span className="text-sm text-gray-500">Nenhuma função atribuída</span>
                    )}
                  </div>
                </div>
              </div>
            </div>

            {/* Card de Claims (se houver) */}
            {currentUser?.claims && Object.keys(currentUser.claims).length > 0 && (
              <div className="card">
                <h2 className="text-xl font-semibold text-gray-900 mb-4">Permissões Adicionais</h2>
                <div className="space-y-2">
                  {Object.entries(currentUser.claims).map(([key, value]) => (
                    <div key={key} className="flex justify-between items-center py-2 border-b border-gray-200 last:border-0">
                      <span className="text-sm font-medium text-gray-700">{key}:</span>
                      <span className="text-sm text-gray-600">{value}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Sidebar - Ações Rápidas */}
          <div className="space-y-6">
            <div className="card">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Ações Rápidas</h3>
              <div className="space-y-3">
                <Button
                  variant="primary"
                  onClick={() => navigate('/profile/change-password')}
                  className="w-full flex items-center justify-center gap-2"
                >
                  <KeyIcon className="h-5 w-5" />
                  Alterar Senha
                </Button>
              </div>
            </div>

            <div className="card">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Estatísticas</h3>
              <div className="space-y-3 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-600">Funções:</span>
                  <span className="font-medium text-gray-900">
                    {currentUser?.roles?.length || 0}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Permissões:</span>
                  <span className="font-medium text-gray-900">
                    {currentUser?.claims ? Object.keys(currentUser.claims).length : 0}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Status:</span>
                  <span className="font-medium text-gray-900">
                    {currentUser?.isActive ? 'Ativo' : 'Inativo'}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};


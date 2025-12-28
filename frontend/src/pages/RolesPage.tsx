import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { rolesService } from '../features/roles/services/roles.service';
import { CustomRole, CreateCustomRoleRequest, UpdateCustomRoleRequest, HierarchyItem } from '../shared/types/api';
import { LoadingSpinner } from '../shared/components/LoadingSpinner';
import { ErrorAlert } from '../shared/components/ErrorAlert';
import { TrashIcon, PencilIcon, XMarkIcon } from '@heroicons/react/24/outline';

export const RolesPage = () => {
  const [roles, setRoles] = useState<CustomRole[]>([]);
  const [parentRoles, setParentRoles] = useState<CustomRole[]>([]);
  const [hierarchy, setHierarchy] = useState<HierarchyItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [isAdmin, setIsAdmin] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  // Estado para edição
  const [editingRole, setEditingRole] = useState<CustomRole | null>(null);
  const [isUpdating, setIsUpdating] = useState(false);
  const [updateError, setUpdateError] = useState<string | null>(null);
  const [editFormData, setEditFormData] = useState<UpdateCustomRoleRequest>({
    displayName: '',
    hierarchyLevel: 1,
  });

  // Form state para criação
  const [formData, setFormData] = useState<CreateCustomRoleRequest>({
    name: '',
    displayName: '',
    parentRoleId: '',
    hierarchyLevel: undefined,
  });

  const [createMode, setCreateMode] = useState<'parent' | 'level'>('level');

  const loadData = async () => {
    try {
      setIsLoading(true);
      setError(null);
      
      const rolesData = await rolesService.getAllRoles();
      setRoles(rolesData);
      
      try {
        const hierarchyData = await rolesService.getRolesHierarchy();
        setHierarchy(hierarchyData);
      } catch {
        console.warn('Não foi possível carregar hierarquia');
      }
      
      try {
        const parentsData = await rolesService.getParentRoles();
        setParentRoles(parentsData);
        setIsAdmin(true);
      } catch {
        setIsAdmin(false);
        console.warn('Não foi possível carregar parent roles (requer admin)');
      }
    } catch (err) {
      setError('Erro ao carregar cargos');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleCreateRole = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreateError(null);
    setIsCreating(true);

    try {
      const requestData: CreateCustomRoleRequest = {
        name: formData.name,
        displayName: formData.displayName,
      };

      if (createMode === 'level' && formData.hierarchyLevel) {
        requestData.hierarchyLevel = formData.hierarchyLevel;
      } else if (createMode === 'parent' && formData.parentRoleId) {
        requestData.parentRoleId = formData.parentRoleId;
      } else {
        setCreateError('Selecione o nível de hierarquia ou o cargo superior');
        setIsCreating(false);
        return;
      }

      await rolesService.createRole(requestData);
      setFormData({ name: '', displayName: '', parentRoleId: '', hierarchyLevel: undefined });
      setShowCreateForm(false);
      await loadData();
    } catch (err: unknown) {
      console.error('Erro ao criar cargo:', err);
      if (typeof err === 'object' && err !== null) {
        const apiErr = err as { error?: string; response?: { data?: { error?: string } } };
        setCreateError(apiErr.error || apiErr.response?.data?.error || 'Erro ao criar cargo');
      } else {
        setCreateError('Erro ao criar cargo');
      }
    } finally {
      setIsCreating(false);
    }
  };

  const handleStartEdit = (role: CustomRole) => {
    setEditingRole(role);
    setEditFormData({
      displayName: role.displayName,
      hierarchyLevel: role.hierarchyLevel,
    });
    setUpdateError(null);
  };

  const handleCancelEdit = () => {
    setEditingRole(null);
    setEditFormData({ displayName: '', hierarchyLevel: 1 });
    setUpdateError(null);
  };

  const handleUpdateRole = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingRole) return;

    setUpdateError(null);
    setIsUpdating(true);

    try {
      await rolesService.updateRole(editingRole.id, editFormData);
      setEditingRole(null);
      await loadData();
    } catch (err: unknown) {
      console.error('Erro ao atualizar cargo:', err);
      if (typeof err === 'object' && err !== null) {
        const apiErr = err as { error?: string; response?: { data?: { error?: string } } };
        setUpdateError(apiErr.error || apiErr.response?.data?.error || 'Erro ao atualizar cargo');
      } else {
        setUpdateError('Erro ao atualizar cargo');
      }
    } finally {
      setIsUpdating(false);
    }
  };

  const handleDeleteRole = async (roleId: string, roleName: string) => {
    console.log('handleDeleteRole chamado:', { roleId, roleName });

    if (!window.confirm(`Tem certeza que deseja excluir o cargo "${roleName}"?`)) {
      console.log('Usuário cancelou a exclusão');
      return;
    }

    console.log('Usuário confirmou a exclusão, iniciando delete...');
    setDeletingId(roleId);
    setError(null);

    try {   
      console.log('Chamando rolesService.deleteRole...');
      await rolesService.deleteRole(roleId);
      console.log('Delete bem-sucedido, recarregando dados...');
      await loadData();
      console.log('Dados recarregados');
    } catch (err: unknown) {
      console.error('Erro ao excluir cargo:', err);
      // O api-client já formata o erro, então err pode ser o ApiError diretamente
      if (typeof err === 'object' && err !== null) {        
        const apiErr = err as { error?: string; response?: { data?: { error?: string } } };
        setError(apiErr.error || apiErr.response?.data?.error || 'Erro ao excluir cargo');
      } else {
        setError('Erro ao excluir cargo');
      }
    } finally {
      setDeletingId(null);
      console.log('Delete finalizado');
    }
  };

  const getAvailableLevels = (excludeLevel?: number): number[] => {
    const existingLevels = new Set(roles.map(r => r.hierarchyLevel));
    if (excludeLevel) {
      existingLevels.delete(excludeLevel);
    }
    const levels: number[] = [];
    const maxLevel = Math.max(...roles.map(r => r.hierarchyLevel), 100);
    for (let i = 1; i <= maxLevel; i++) {
      if (!existingLevels.has(i)) {
        levels.push(i);
      }
    }
    return levels;
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center min-h-screen">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <div className="flex justify-between items-center">
            <div className="flex items-center gap-4">
              <Link to="/employees" className="text-gray-600 hover:text-gray-900">
                ← Voltar
              </Link>
              <h1 className="text-2xl font-bold text-gray-900">
                Gerenciamento de Cargos
              </h1>
            </div>
            {isAdmin && (
              <button
                onClick={() => setShowCreateForm(!showCreateForm)}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition"
              >
                {showCreateForm ? 'Cancelar' : '+ Novo Cargo'}
              </button>
            )}
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {error && <ErrorAlert message={error} className="mb-6" />}

        {/* Modal de Edição */}
        {editingRole && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg shadow-xl p-6 w-full max-w-md mx-4">
              <div className="flex justify-between items-center mb-4">
                <h2 className="text-lg font-semibold">Editar Cargo</h2>
                <button onClick={handleCancelEdit} className="text-gray-500 hover:text-gray-700">
                  <XMarkIcon className="h-6 w-6" />
                </button>
              </div>
              
              {updateError && <ErrorAlert message={updateError} className="mb-4" />}
              
              <form onSubmit={handleUpdateRole} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Nome (identificador)
                  </label>
                  <input
                    type="text"
                    value={editingRole.name}
                    disabled
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 bg-gray-100 text-gray-500"
                  />
                  <p className="text-xs text-gray-500 mt-1">O nome não pode ser alterado</p>
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Nome de Exibição
                  </label>
                  <input
                    type="text"
                    value={editFormData.displayName}
                    onChange={(e) => setEditFormData({ ...editFormData, displayName: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    required
                  />
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Nível de Hierarquia
                  </label>
                  <select
                    value={editFormData.hierarchyLevel}
                    onChange={(e) => setEditFormData({ ...editFormData, hierarchyLevel: Number(e.target.value) })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    required
                  >
                    <option value={editingRole.hierarchyLevel}>
                      Nível {editingRole.hierarchyLevel} (atual)
                    </option>
                    {getAvailableLevels(editingRole.hierarchyLevel).map((level) => (
                      <option key={level} value={level}>
                        Nível {level}
                      </option>
                    ))}
                  </select>
                </div>
                
                <div className="flex gap-2 pt-4">
                  <button
                    type="submit"
                    disabled={isUpdating}
                    className="flex-1 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition disabled:opacity-50"
                  >
                    {isUpdating ? 'Salvando...' : 'Salvar'}
                  </button>
                  <button
                    type="button"
                    onClick={handleCancelEdit}
                    className="flex-1 bg-gray-200 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-300 transition"
                  >
                    Cancelar
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Formulário de criação */}
        {showCreateForm && (
          <div className="bg-white rounded-lg shadow p-6 mb-6">
            <h2 className="text-lg font-semibold mb-4">Criar Novo Cargo</h2>
            {createError && <ErrorAlert message={createError} className="mb-4" />}
            
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Como deseja definir a hierarquia?
              </label>
              <div className="flex gap-4">
                <label className="flex items-center">
                  <input
                    type="radio"
                    name="createMode"
                    value="level"
                    checked={createMode === 'level'}
                    onChange={() => setCreateMode('level')}
                    className="mr-2"
                  />
                  <span className="text-sm">Escolher nível manualmente</span>
                </label>
                <label className="flex items-center">
                  <input
                    type="radio"
                    name="createMode"
                    value="parent"
                    checked={createMode === 'parent'}
                    onChange={() => setCreateMode('parent')}
                    className="mr-2"
                  />
                  <span className="text-sm">Baseado no cargo superior</span>
                </label>
              </div>
            </div>

            <form onSubmit={handleCreateRole} className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Nome (identificador)
                  </label>
                  <input
                    type="text"
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    placeholder="Ex: Supervisor"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Nome de Exibição
                  </label>
                  <input
                    type="text"
                    value={formData.displayName}
                    onChange={(e) => setFormData({ ...formData, displayName: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    placeholder="Ex: Supervisor de Equipe"
                    required
                  />
                </div>
              </div>

              {createMode === 'level' ? (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Nível de Hierarquia
                  </label>
                  <select
                    value={formData.hierarchyLevel || ''}
                    onChange={(e) => setFormData({ ...formData, hierarchyLevel: Number(e.target.value) })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    required
                  >
                    <option value="">Selecione o nível...</option>
                    {getAvailableLevels().map((level) => (
                      <option key={level} value={level}>
                        Nível {level}
                      </option>
                    ))}
                  </select>
                  <p className="text-xs text-gray-500 mt-1">
                    Níveis mais altos têm mais permissões. Admin = 100, Director = 30, Leader = 20, Employee = 10.
                  </p>
                </div>
              ) : (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Cargo Superior (quem gerencia este cargo)
                  </label>
                  <select
                    value={formData.parentRoleId || ''}
                    onChange={(e) => setFormData({ ...formData, parentRoleId: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    required
                  >
                    <option value="">Selecione o cargo superior...</option>
                    {parentRoles.map((role) => (
                      <option key={role.id} value={role.id}>
                        {role.displayName} (Nível {role.hierarchyLevel})
                      </option>
                    ))}
                  </select>
                  <p className="text-xs text-gray-500 mt-1">
                    O novo cargo será criado abaixo do cargo selecionado na hierarquia.
                  </p>
                </div>
              )}

              <div className="flex gap-2">
                <button
                  type="submit"
                  disabled={isCreating}
                  className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition disabled:opacity-50"
                >
                  {isCreating ? 'Criando...' : 'Criar Cargo'}
                </button>
                <button
                  type="button"
                  onClick={() => setShowCreateForm(false)}
                  className="bg-gray-200 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-300 transition"
                >
                  Cancelar
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Lista de Cargos */}
        <div className="bg-white rounded-lg shadow">
          <div className="px-6 py-4 border-b border-gray-200">
            <h2 className="text-lg font-semibold text-gray-900">Cargos Disponíveis</h2>
          </div>
          
          {roles.length === 0 ? (
            <div className="p-6 text-center text-gray-500">
              Nenhum cargo encontrado.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Nome
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Nome de Exibição
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Nível
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Tipo
                    </th>
                    {isAdmin && (
                      <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Ações
                      </th>
                    )}
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {roles.map((role) => (
                    <tr key={role.id} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                        {role.name}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {role.displayName}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                          {role.hierarchyLevel}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span
                          className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                            role.isSystemRole
                              ? 'bg-blue-100 text-blue-800'
                              : 'bg-green-100 text-green-800'
                          }`}
                        >
                          {role.isSystemRole ? 'Sistema' : 'Customizado'}
                        </span>
                      </td>
                      {isAdmin && (
                        <td className="px-6 py-4 whitespace-nowrap text-center">
                          {!role.isSystemRole && (
                            <div className="flex justify-center gap-2">
                              <button
                                onClick={() => handleStartEdit(role)}
                                className="text-blue-600 hover:text-blue-800 p-1"
                                title="Editar cargo"
                              >
                                <PencilIcon className="h-5 w-5" />
                              </button>
                              <button
                                onClick={() => handleDeleteRole(role.id, role.displayName)}
                                disabled={deletingId === role.id}
                                className="text-red-600 hover:text-red-800 p-1 disabled:opacity-50"
                                title="Excluir cargo"
                              >
                                {deletingId === role.id ? (
                                  <LoadingSpinner size="sm" />
                                ) : (
                                  <TrashIcon className="h-5 w-5" />
                                )}
                              </button>
                            </div>
                          )}
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* Hierarquia de Cargos */}
        {hierarchy.length > 0 && (
          <div className="bg-white rounded-lg shadow mt-6">
            <div className="px-6 py-4 border-b border-gray-200">
              <h2 className="text-lg font-semibold text-gray-900">Hierarquia de Permissões</h2>
            </div>
            <div className="p-6">
              <div className="space-y-3">
                {hierarchy.map((item) => (
                  <div key={item.id} className="flex items-start gap-3 p-3 bg-gray-50 rounded-lg">
                    <div className="flex-shrink-0 w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center text-blue-600 font-bold text-sm">
                      {item.hierarchyLevel}
                    </div>
                    <div className="flex-grow">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-gray-900">{item.displayName}</span>
                        {item.isSystemRole && (
                          <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
                            Sistema
                          </span>
                        )}
                      </div>
                      {item.canManage.length > 0 && (
                        <div className="text-sm text-gray-500 mt-1">
                          Gerencia: {item.canManage.join(', ')}
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}
      </main>
    </div>
  );
};
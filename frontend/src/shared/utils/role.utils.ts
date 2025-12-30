import { Role } from '../types/api';

export const roleLabels: Record<Role, string> = {
  [Role.Employee]: 'Funcionário',
  [Role.Leader]: 'Líder',
  [Role.Director]: 'Diretor',
  [Role.Admin]: 'Administrador',
};

export const roleColors: Record<Role, string> = {
  [Role.Employee]: 'bg-gray-100 text-gray-800',
  [Role.Leader]: 'bg-blue-100 text-blue-800',
  [Role.Director]: 'bg-purple-100 text-purple-800',
  [Role.Admin]: 'bg-red-100 text-red-800',
};

export const getRoleLabel = (role: Role | string | number): string => {
  // Se for string, tentar converter para Role enum
  if (typeof role === 'string') {
    const roleMap: Record<string, Role> = {
      'Employee': Role.Employee,
      'Leader': Role.Leader,
      'Director': Role.Director,
      'Admin': Role.Admin,
      'employee': Role.Employee,
      'leader': Role.Leader,
      'director': Role.Director,
      'admin': Role.Admin,
    };
    role = roleMap[role] || Role.Employee;
  }
  
  // Se for número, usar diretamente
  if (typeof role === 'number') {
    return roleLabels[role as Role] || 'Desconhecido';
  }
  
  return roleLabels[role as Role] || 'Desconhecido';
};

export const getRoleColor = (role: Role): string => {
  return roleColors[role] || 'bg-gray-100 text-gray-800';
};

export const canCreateEmployee = (userRole: Role): boolean => {
  return userRole >= Role.Leader;
};

export const canUpdateEmployee = (userRole: Role, targetRole: Role): boolean => {
  return userRole >= Role.Leader && userRole >= targetRole;
};

export const canDeleteEmployee = (userRole: Role): boolean => {
  return userRole >= Role.Leader;
};

export const getHighestRole = (roles: string[]): string => {
  const roleHierarchy: Record<string, number> = {
    'Admin': 4,
    'Director': 3,
    'Leader': 2,
    'Employee': 1,
  };

  const roleTranslations: Record<string, string> = {
    'Admin': 'Administrador',
    'Director': 'Diretor',
    'Leader': 'Líder',
    'Employee': 'Funcionário',
  };

  if (!roles || roles.length === 0) return 'N/A';

  // Encontra a role com maior prioridade
  const highestRole = roles.reduce((highest, current) => {
    const currentPriority = roleHierarchy[current] || 0;
    const highestPriority = roleHierarchy[highest] || 0;
    return currentPriority > highestPriority ? current : highest;
  }, roles[0]);

  // Retorna o nome traduzido
  return roleTranslations[highestRole] || highestRole;
};



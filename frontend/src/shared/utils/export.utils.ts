import { Employee } from '../types/api';
import { getRoleLabel } from './role.utils';
import { formatDate, calculateAge } from './date.utils';

/**
 * Exporta dados para CSV
 */
export const exportToCSV = (data: Employee[], filename: string = 'funcionarios') => {
  // Cabeçalhos
  const headers = [
    'Nome',
    'Sobrenome',
    'Nome Completo',
    'Email',
    'CPF',
    'Data de Nascimento',
    'Idade',
    'Função',
    'Gerente',
    'Telefones',
    'Data de Criação',
  ];

  // Converter dados para linhas CSV
  const rows = data.map((employee) => [
    employee.firstName,
    employee.lastName,
    employee.fullName,
    employee.email,
    employee.documentNumber,
    formatDate(employee.birthDate),
    calculateAge(employee.birthDate).toString(),
    getRoleLabel(employee.role),
    employee.managerName || '-',
    employee.phoneNumbers?.join('; ') || '-',
    formatDate(employee.createdAt),
  ]);

  // Combinar cabeçalhos e linhas
  const csvContent = [
    headers.join(','),
    ...rows.map((row) => row.map((cell) => `"${String(cell).replace(/"/g, '""')}"`).join(',')),
  ].join('\n');

  // Criar blob e download
  const blob = new Blob(['\ufeff' + csvContent], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  const url = URL.createObjectURL(blob);
  
  link.setAttribute('href', url);
  link.setAttribute('download', `${filename}_${new Date().toISOString().split('T')[0]}.csv`);
  link.style.visibility = 'hidden';
  
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  
  URL.revokeObjectURL(url);
};

/**
 * Exporta dados para Excel (formato XLSX usando HTML table)
 * Nota: Esta é uma solução simples que funciona na maioria dos casos
 * Para funcionalidade completa de Excel, considere usar uma biblioteca como 'xlsx'
 */
export const exportToExcel = (data: Employee[], filename: string = 'funcionarios') => {
  // Criar tabela HTML
  const table = document.createElement('table');
  
  // Cabeçalhos
  const thead = document.createElement('thead');
  const headerRow = document.createElement('tr');
  const headers = [
    'Nome',
    'Sobrenome',
    'Nome Completo',
    'Email',
    'CPF',
    'Data de Nascimento',
    'Idade',
    'Função',
    'Superior Hierárquico',
    'Telefones',
    'Data de Criação',
  ];
  
  headers.forEach((header) => {
    const th = document.createElement('th');
    th.textContent = header;
    th.style.border = '1px solid #000';
    th.style.padding = '8px';
    th.style.backgroundColor = '#f0f0f0';
    headerRow.appendChild(th);
  });
  thead.appendChild(headerRow);
  table.appendChild(thead);
  
  // Dados
  const tbody = document.createElement('tbody');
  data.forEach((employee) => {
    const row = document.createElement('tr');
    const cells = [
      employee.firstName,
      employee.lastName,
      employee.fullName,
      employee.email,
      employee.documentNumber,
      formatDate(employee.birthDate),
      calculateAge(employee.birthDate).toString(),
      getRoleLabel(employee.role),
      employee.managerName || '-',
      employee.phoneNumbers?.join('; ') || '-',
      formatDate(employee.createdAt),
    ];
    
    cells.forEach((cell) => {
      const td = document.createElement('td');
      td.textContent = String(cell);
      td.style.border = '1px solid #ccc';
      td.style.padding = '8px';
      row.appendChild(td);
    });
    tbody.appendChild(row);
  });
  table.appendChild(tbody);
  
  // Criar HTML completo
  const html = `
    <html>
      <head>
        <meta charset="utf-8">
        <style>
          table { border-collapse: collapse; width: 100%; }
          th { background-color: #f0f0f0; font-weight: bold; }
        </style>
      </head>
      <body>
        ${table.outerHTML}
      </body>
    </html>
  `;
  
  // Criar blob e download
  const blob = new Blob([html], { type: 'application/vnd.ms-excel' });
  const link = document.createElement('a');
  const url = URL.createObjectURL(blob);
  
  link.setAttribute('href', url);
  link.setAttribute('download', `${filename}_${new Date().toISOString().split('T')[0]}.xls`);
  link.style.visibility = 'hidden';
  
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  
  URL.revokeObjectURL(url);
};

/**
 * Busca todos os funcionários (sem paginação) para exportação
 */
export const fetchAllEmployeesForExport = async (
  employeesService: {
    getEmployees: (params: any) => Promise<{ items: Employee[]; totalPages: number }>;
  },
  params: any = {}
): Promise<Employee[]> => {
  try {
    // Buscar primeira página para saber o total
    const firstPage = await employeesService.getEmployees({
      ...params,
      pageNumber: 1,
      pageSize: 1000, // Máximo permitido
    });
    
    // Se houver mais páginas, buscar todas
    if (firstPage.totalPages > 1) {
      const allPromises = [];
      for (let i = 1; i <= firstPage.totalPages; i++) {
        allPromises.push(
          employeesService.getEmployees({
            ...params,
            pageNumber: i,
            pageSize: 1000,
          })
        );
      }
      
      const allPages = await Promise.all(allPromises);
      return allPages.flatMap((page) => page.items);
    }
    
    return firstPage.items;
  } catch (error) {
    console.error('Erro ao buscar funcionários para exportação:', error);
    throw error;
  }
};


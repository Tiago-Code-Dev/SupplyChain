// Formatação de CPF: 000.000.000-00
export const formatCPF = (value: string): string => {
  // Remove tudo que não é dígito
  const numbers = value.replace(/\D/g, '');
  
  // Aplica a máscara
  if (numbers.length <= 3) {
    return numbers;
  } else if (numbers.length <= 6) {
    return `${numbers.slice(0, 3)}.${numbers.slice(3)}`;
  } else if (numbers.length <= 9) {
    return `${numbers.slice(0, 3)}.${numbers.slice(3, 6)}.${numbers.slice(6)}`;
  } else {
    return `${numbers.slice(0, 3)}.${numbers.slice(3, 6)}.${numbers.slice(6, 9)}-${numbers.slice(9, 11)}`;
  }
};

// Remove formatação do CPF
export const unformatCPF = (value: string): string => {
  return value.replace(/\D/g, '');
};

// Formatação de telefone: (99) 99999-9999
export const formatPhone = (value: string): string => {
  // Remove tudo que não é dígito
  const numbers = value.replace(/\D/g, '');
  
  // Aplica a máscara
  if (numbers.length <= 2) {
    return numbers.length > 0 ? `(${numbers}` : numbers;
  } else if (numbers.length <= 6) {
    return `(${numbers.slice(0, 2)}) ${numbers.slice(2)}`;
  } else if (numbers.length <= 10) {
    return `(${numbers.slice(0, 2)}) ${numbers.slice(2, 7)}-${numbers.slice(7)}`;
  } else {
    // Limita a 11 dígitos (com DDD)
    return `(${numbers.slice(0, 2)}) ${numbers.slice(2, 7)}-${numbers.slice(7, 11)}`;
  }
};

// Remove formatação do telefone
export const unformatPhone = (value: string): string => {
  return value.replace(/\D/g, '');
};

// Formata múltiplos telefones separados por vírgula
export const formatPhoneList = (value: string): string => {
  if (!value) return '';
  
  // Se já está formatado, retornar como está
  if (value.includes('(') && value.includes(')')) {
    return value;
  }
  
  // Separar por vírgula e formatar cada telefone
  const phones = value.split(',').map(phone => phone.trim()).filter(Boolean);
  return phones.map(phone => formatPhone(phone)).join(', ');
};

// Remove formatação de múltiplos telefones
export const unformatPhoneList = (value: string): string[] => {
  if (!value) return [];

  const phones = value.split(',').map(phone => unformatPhone(phone.trim()));
  return phones.filter(phone => phone.length > 0);
};

// Corrige caracteres com encoding quebrado (UTF-8 mal interpretado)
// Mapeia caracteres comuns que aparecem como "�" ou outros símbolos
const encodingFixMap: Record<string, string> = {
  // Vogais acentuadas
  'Funcion�rio': 'Funcionário',
  'L�der': 'Líder',
  'funcion�rio': 'funcionário',
  'l�der': 'líder',
  // Casos genéricos com caractere de substituição
  '�': 'í', // Fallback para casos não mapeados
};

export const fixBrokenEncoding = (text: string): string => {
  if (!text) return text;

  let result = text;

  // Primeiro tenta substituições exatas
  for (const [broken, fixed] of Object.entries(encodingFixMap)) {
    if (broken !== '�') {
      result = result.replace(new RegExp(broken, 'g'), fixed);
    }
  }

  return result;
};

// Corrige uma lista de strings com encoding quebrado
export const fixBrokenEncodingList = (items: string[]): string[] => {
  return items.map(item => fixBrokenEncoding(item));
};


import { format, parseISO, isValid } from 'date-fns';
import { ptBR } from 'date-fns/locale';

export const formatDate = (date: string | Date, pattern: string = 'dd/MM/yyyy'): string => {
  try {
    const dateObj = typeof date === 'string' ? parseISO(date) : date;
    if (!isValid(dateObj)) return '-';
    return format(dateObj, pattern, { locale: ptBR });
  } catch {
    return '-';
  }
};

export const formatDateTime = (date: string | Date): string => {
  return formatDate(date, 'dd/MM/yyyy HH:mm');
};

export const calculateAge = (birthDate: string | Date): number => {
  try {
    const birth = typeof birthDate === 'string' ? parseISO(birthDate) : birthDate;
    if (!isValid(birth)) return 0;
    
    const today = new Date();
    let age = today.getFullYear() - birth.getFullYear();
    const monthDiff = today.getMonth() - birth.getMonth();
    
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())) {
      age--;
    }
    
    return age;
  } catch {
    return 0;
  }
};



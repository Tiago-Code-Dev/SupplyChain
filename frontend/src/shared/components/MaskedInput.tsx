import { InputHTMLAttributes, forwardRef, useState, useEffect } from 'react';
import { formatCPF, formatPhone, formatPhoneList } from '../utils/format.utils';

interface MaskedInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'onChange' | 'value'> {
  label?: string;
  error?: string;
  mask?: 'cpf' | 'phone' | 'phoneList';
  value?: string;
  onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

export const MaskedInput = forwardRef<HTMLInputElement, MaskedInputProps>(
  ({ label, error, mask, onChange, value = '', className = '', ...props }, ref) => {
    const [displayValue, setDisplayValue] = useState<string>('');

    useEffect(() => {
      if (value !== undefined && value !== null) {
        const stringValue = String(value);
        if (mask === 'cpf') {
          setDisplayValue(formatCPF(stringValue));
        } else if (mask === 'phone') {
          setDisplayValue(formatPhone(stringValue));
        } else if (mask === 'phoneList') {
          setDisplayValue(formatPhoneList(stringValue));
        } else {
          setDisplayValue(stringValue);
        }
      } else {
        setDisplayValue('');
      }
    }, [value, mask]);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const inputValue = e.target.value;
      let formattedValue = inputValue;

      if (mask === 'cpf') {
        formattedValue = formatCPF(inputValue);
      } else if (mask === 'phone') {
        formattedValue = formatPhone(inputValue);
      } else if (mask === 'phoneList') {
        // Para lista de telefones, formatar cada um separadamente
        const phones = inputValue.split(',').map(p => p.trim());
        formattedValue = phones.map(p => formatPhone(p)).join(', ');
      }

      setDisplayValue(formattedValue);
      
      // Criar um novo evento com o valor formatado para react-hook-form
      if (onChange) {
        const syntheticEvent = {
          ...e,
          target: {
            ...e.target,
            value: formattedValue,
          },
          currentTarget: {
            ...e.currentTarget,
            value: formattedValue,
          },
        } as React.ChangeEvent<HTMLInputElement>;
        onChange(syntheticEvent);
      }
    };

    return (
      <div className="w-full">
        {label && (
          <label htmlFor={props.id} className="label">
            {label}
          </label>
        )}
        <input
          ref={ref}
          {...props}
          value={displayValue}
          onChange={handleChange}
          className={`input ${error ? 'border-red-500' : ''} ${className}`}
        />
        {error && (
          <p className="mt-1 text-sm text-red-600">{error}</p>
        )}
      </div>
    );
  }
);

MaskedInput.displayName = 'MaskedInput';


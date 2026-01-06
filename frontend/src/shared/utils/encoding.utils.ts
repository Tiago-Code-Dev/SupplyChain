/**
 * Utility to fix encoding issues in texts
 * coming from API with corrupted characters
 */

// Replacement character code (U+FFFD)
const REPLACEMENT_CHAR = String.fromCharCode(65533);

// Correct accented characters using Unicode escape sequences
const CHAR_I_ACUTE = String.fromCharCode(237);  // í
const CHAR_A_ACUTE = String.fromCharCode(225);  // á
const CHAR_E_CIRCUMFLEX = String.fromCharCode(234); // ê
const CHAR_E_ACUTE = String.fromCharCode(233);  // é
const CHAR_A_TILDE = String.fromCharCode(227);  // ã
const CHAR_O_ACUTE = String.fromCharCode(243);  // ó
const CHAR_C_CEDILLA = String.fromCharCode(231); // ç

/**
 * Fixes encoding issues in a string
 */
export function fixEncoding(text: string | null | undefined): string {
  if (!text) return text || '';

  // Only process if text contains the replacement character
  if (!text.includes(REPLACEMENT_CHAR)) {
    return text;
  }

  let result = text;

  // Direct replacements for known corrupted words
  // L + replacement + der = Líder
  result = result.split('L' + REPLACEMENT_CHAR + 'der').join('L' + CHAR_I_ACUTE + 'der');
  result = result.split('l' + REPLACEMENT_CHAR + 'der').join('l' + CHAR_I_ACUTE + 'der');

  // Funcion + replacement + rio = Funcionário
  result = result.split('Funcion' + REPLACEMENT_CHAR + 'rio').join('Funcion' + CHAR_A_ACUTE + 'rio');
  result = result.split('funcion' + REPLACEMENT_CHAR + 'rio').join('funcion' + CHAR_A_ACUTE + 'rio');

  // Ger + replacement + ncia = Gerência
  result = result.split('Ger' + REPLACEMENT_CHAR + 'ncia').join('Ger' + CHAR_E_CIRCUMFLEX + 'ncia');
  result = result.split('ger' + REPLACEMENT_CHAR + 'ncia').join('ger' + CHAR_E_CIRCUMFLEX + 'ncia');

  // T + replacement + cnico = Técnico
  result = result.split('T' + REPLACEMENT_CHAR + 'cnico').join('T' + CHAR_E_ACUTE + 'cnico');
  result = result.split('t' + REPLACEMENT_CHAR + 'cnico').join('t' + CHAR_E_ACUTE + 'cnico');

  // Ger + replacement + nte = Gerente (if corrupted)
  result = result.split('Ger' + REPLACEMENT_CHAR + 'nte').join('Gerente');

  // Secret + replacement + rio/ria
  result = result.split('Secret' + REPLACEMENT_CHAR + 'rio').join('Secret' + CHAR_A_ACUTE + 'rio');
  result = result.split('Secret' + REPLACEMENT_CHAR + 'ria').join('Secret' + CHAR_A_ACUTE + 'ria');

  // Estagi + replacement + rio/ria
  result = result.split('Estagi' + REPLACEMENT_CHAR + 'rio').join('Estagi' + CHAR_A_ACUTE + 'rio');
  result = result.split('Estagi' + REPLACEMENT_CHAR + 'ria').join('Estagi' + CHAR_A_ACUTE + 'ria');

  // Aux + replacement + liar = Auxiliar
  result = result.split('Aux' + REPLACEMENT_CHAR + 'liar').join('Auxiliar');

  // Log + replacement + stica = Logística
  result = result.split('Log' + REPLACEMENT_CHAR + 'stica').join('Log' + CHAR_I_ACUTE + 'stica');

  // Produ + replacement + replacement + o = Produção
  result = result.split('Produ' + REPLACEMENT_CHAR + REPLACEMENT_CHAR + 'o').join('Produ' + CHAR_C_CEDILLA + CHAR_A_TILDE + 'o');

  // Administra + replacement + replacement + o = Administração
  result = result.split('Administra' + REPLACEMENT_CHAR + REPLACEMENT_CHAR + 'o').join('Administra' + CHAR_C_CEDILLA + CHAR_A_TILDE + 'o');

  // Dire + replacement + replacement + o = Direção
  result = result.split('Dire' + REPLACEMENT_CHAR + REPLACEMENT_CHAR + 'o').join('Dire' + CHAR_C_CEDILLA + CHAR_A_TILDE + 'o');

  // Recep + replacement + replacement + o = Recepção
  result = result.split('Recep' + REPLACEMENT_CHAR + REPLACEMENT_CHAR + 'o').join('Recep' + CHAR_C_CEDILLA + CHAR_A_TILDE + 'o');

  // Supervis + replacement + o = Supervisão
  result = result.split('Supervis' + REPLACEMENT_CHAR + 'o').join('Supervis' + CHAR_A_TILDE + 'o');

  // If still has replacement chars, remove them as last resort
  if (result.includes(REPLACEMENT_CHAR)) {
    result = result.split(REPLACEMENT_CHAR).join('');
  }

  return result;
}

/**
 * Fixes encoding in an object, applying the fix to all string properties recursively
 */
export function fixEncodingInObject<T>(obj: T): T {
  if (obj === null || obj === undefined) return obj;

  if (typeof obj === 'string') {
    return fixEncoding(obj) as T;
  }

  if (Array.isArray(obj)) {
    return obj.map((item) => fixEncodingInObject(item)) as T;
  }

  if (typeof obj === 'object') {
    const result: Record<string, unknown> = {};
    for (const [key, value] of Object.entries(obj)) {
      result[key] = fixEncodingInObject(value);
    }
    return result as T;
  }

  return obj;
}

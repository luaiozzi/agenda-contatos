/**
 * Utilitários de telefone (formato brasileiro).
 * As regras de validação espelham as do backend (PhoneNormalizer.cs).
 */

export function onlyDigits(value) {
  return String(value ?? '').replace(/\D/g, '');
}

/**
 * Formata para exibição / digitação: (19) 99999-8888 ou (19) 3333-4444.
 * Aceita colagem com +55.
 */
export function formatPhone(value) {
  const raw = String(value ?? '').trim();
  let digits = onlyDigits(raw);

  if (raw.startsWith('+55')) {
    digits = digits.slice(2);
  }

  digits = digits.slice(0, 11);

  if (digits.length === 0) {
    return '';
  }
  if (digits.length <= 2) {
    return `(${digits}`;
  }
  if (digits.length <= 6) {
    return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
  }
  if (digits.length <= 10) {
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
  }
  return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
}

/**
 * 11 dígitos: celular (número começa com 9).
 * 10 dígitos: fixo (número começa com 2 a 5).
 * DDD sem zero à esquerda e sem terminar em zero.
 */
export function isValidPhone(value) {
  const digits = onlyDigits(value);

  if (digits.length !== 10 && digits.length !== 11) {
    return false;
  }
  if (digits[0] === '0' || digits[1] === '0') {
    return false;
  }
  if (digits.length === 11) {
    return digits[2] === '9';
  }
  return digits[2] >= '2' && digits[2] <= '5';
}

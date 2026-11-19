import { isValidPhone, onlyDigits } from './phone';

const EMAIL_REGEX = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

/**
 * Valida os dados do formulário de contato.
 * Retorna um objeto { campo: 'mensagem' } (vazio quando tudo é válido).
 */
export function validateContact({ name, email, phone }) {
  const errors = {};

  const trimmedName = String(name ?? '').trim();
  if (!trimmedName) {
    errors.name = 'O nome é obrigatório.';
  } else if (trimmedName.length < 2) {
    errors.name = 'O nome deve ter pelo menos 2 caracteres.';
  } else if (trimmedName.length > 100) {
    errors.name = 'O nome deve ter no máximo 100 caracteres.';
  }

  const trimmedEmail = String(email ?? '').trim();
  if (!trimmedEmail) {
    errors.email = 'O e-mail é obrigatório.';
  } else if (trimmedEmail.length > 150) {
    errors.email = 'O e-mail deve ter no máximo 150 caracteres.';
  } else if (!EMAIL_REGEX.test(trimmedEmail)) {
    errors.email = 'Informe um e-mail válido.';
  }

  if (!onlyDigits(phone)) {
    errors.phone = 'O telefone é obrigatório.';
  } else if (!isValidPhone(phone)) {
    errors.phone = 'Telefone inválido. Use DDD + número, ex.: (19) 99999-8888.';
  }

  return errors;
}

/**
 * Valida os dados do formulário de login.
 * Retorna um objeto { campo: 'mensagem' } (vazio quando tudo é válido).
 */
export function validateLogin({ email, password }) {
  const errors = {};

  const trimmedEmail = String(email ?? '').trim();
  if (!trimmedEmail) {
    errors.email = 'O e-mail é obrigatório.';
  } else if (!EMAIL_REGEX.test(trimmedEmail)) {
    errors.email = 'Informe um e-mail válido.';
  }

  if (!password) {
    errors.password = 'A senha é obrigatória.';
  }

  return errors;
}

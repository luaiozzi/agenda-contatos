import { clearSession, getToken } from './session';

const API_URL = import.meta.env.VITE_API_URL ?? '';

/**
 * Erro lançado quando a API responde com status diferente de 2xx.
 * Para respostas 400 de validação, `errors` traz { campo: [mensagens] }.
 */
export class ApiError extends Error {
  constructor(status, data, fallbackMessage) {
    super(data?.detail || data?.title || fallbackMessage || 'Erro inesperado.');
    this.name = 'ApiError';
    this.status = status;
    this.errors = data?.errors ?? null;
  }
}

let unauthorizedHandler = null;

/**
 * Registra a função chamada quando a API responde 401 a uma requisição que enviou token
 * (sessão expirada ou token inválido). A aplicação a usa para voltar à tela de login.
 */
export function setUnauthorizedHandler(handler) {
  unauthorizedHandler = handler;
}

export async function request(path, options = {}) {
  const token = getToken();
  let response;

  try {
    response = await fetch(`${API_URL}${path}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(options.headers ?? {})
      }
    });
  } catch {
    throw new ApiError(0, null, 'Não foi possível conectar ao servidor. Verifique se a API está em execução.');
  }

  const text = await response.text();
  let data = null;

  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = null;
    }
  }

  if (!response.ok) {
    // 401 com token enviado = sessão inválida. (Um 401 no login, sem token, é só senha errada.)
    if (response.status === 401 && token) {
      clearSession();
      unauthorizedHandler?.();
    }
    throw new ApiError(response.status, data);
  }

  return data;
}

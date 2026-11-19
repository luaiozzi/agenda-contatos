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

export async function request(path, options = {}) {
  let response;

  try {
    response = await fetch(`${API_URL}${path}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
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
    throw new ApiError(response.status, data);
  }

  return data;
}

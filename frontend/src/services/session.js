/**
 * Guarda a sessão (token JWT, e-mail e validade) no localStorage.
 * Todas as operações são protegidas com try/catch: se o navegador bloquear o armazenamento,
 * a aplicação continua funcionando, apenas sem lembrar o login ao recarregar a página.
 */
const KEY = 'agenda.session';

export function loadSession() {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) {
      return null;
    }

    const session = JSON.parse(raw);
    if (!session?.token) {
      return null;
    }

    if (session.expiresAt && new Date(session.expiresAt) <= new Date()) {
      localStorage.removeItem(KEY);
      return null;
    }

    return session;
  } catch {
    return null;
  }
}

export function saveSession(session) {
  try {
    localStorage.setItem(KEY, JSON.stringify(session));
  } catch {
    // Sem armazenamento disponível: a sessão vale apenas enquanto a página estiver aberta.
  }
}

export function clearSession() {
  try {
    localStorage.removeItem(KEY);
  } catch {
    // Nada a fazer.
  }
}

export function getToken() {
  return loadSession()?.token ?? null;
}

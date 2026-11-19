import { computed, ref } from 'vue';
import { authService } from '../services/authService';
import { clearSession, loadSession, saveSession } from '../services/session';

// Estado compartilhado por toda a aplicação (módulo singleton).
const session = ref(loadSession());

export function useAuth() {
  const isAuthenticated = computed(() => session.value !== null);
  const email = computed(() => session.value?.email ?? '');

  async function login(emailValue, password) {
    const result = await authService.login({ email: emailValue, password });

    const newSession = {
      token: result.token,
      expiresAt: result.expiresAt,
      email: result.email
    };

    saveSession(newSession);
    session.value = newSession;
  }

  function logout() {
    clearSession();
    session.value = null;
  }

  return { isAuthenticated, email, login, logout };
}

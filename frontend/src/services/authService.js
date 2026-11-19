import { request } from './api';

export const authService = {
  /** Retorna { token, expiresAt, email }. */
  login({ email, password }) {
    return request('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password })
    });
  }
};

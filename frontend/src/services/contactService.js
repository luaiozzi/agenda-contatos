import { request } from './api';

const BASE = '/api/contacts';

export const contactService = {
  list({ search = '', page = 1, pageSize = 10 } = {}) {
    const params = new URLSearchParams({
      page: String(page),
      pageSize: String(pageSize)
    });

    if (search) {
      params.set('search', search);
    }

    return request(`${BASE}?${params.toString()}`);
  },

  get(id) {
    return request(`${BASE}/${id}`);
  },

  create(payload) {
    return request(BASE, { method: 'POST', body: JSON.stringify(payload) });
  },

  update(id, payload) {
    return request(`${BASE}/${id}`, { method: 'PUT', body: JSON.stringify(payload) });
  },

  remove(id) {
    return request(`${BASE}/${id}`, { method: 'DELETE' });
  }
};

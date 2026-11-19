<script setup>
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import { useToast } from 'primevue/usetoast';
import ContactsView from './views/ContactsView.vue';
import LoginView from './views/LoginView.vue';
import { useAuth } from './composables/useAuth';
import { setUnauthorizedHandler } from './services/api';

const toast = useToast();
const { isAuthenticated, logout } = useAuth();

// Se a API recusar o token (expirado ou inválido), volta para a tela de login.
setUnauthorizedHandler(() => {
  logout();
  toast.add({
    severity: 'warn',
    summary: 'Sessão expirada',
    detail: 'Faça login novamente.',
    life: 4000
  });
});
</script>

<template>
  <Toast />
  <ConfirmDialog />
  <ContactsView v-if="isAuthenticated" />
  <LoginView v-else />
</template>

<script setup>
import { computed, reactive, ref } from 'vue';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import { useAuth } from '../composables/useAuth';
import { ApiError } from '../services/api';
import { validateLogin } from '../utils/validation';

const { login } = useAuth();

const form = reactive({ email: '', password: '' });
const submitted = ref(false);
const loading = ref(false);
const formError = ref('');
const serverErrors = ref({});

// Depois da primeira tentativa de envio, valida em tempo real.
const clientErrors = computed(() => (submitted.value ? validateLogin(form) : {}));

function fieldError(field) {
  return clientErrors.value[field] ?? serverErrors.value?.[field]?.[0] ?? '';
}

async function submit() {
  submitted.value = true;
  formError.value = '';
  serverErrors.value = {};

  if (Object.keys(validateLogin(form)).length > 0) {
    return;
  }

  loading.value = true;
  try {
    await login(form.email.trim(), form.password);
  } catch (error) {
    if (error instanceof ApiError && error.status === 400 && error.errors) {
      serverErrors.value = error.errors;
    } else if (error instanceof ApiError) {
      formError.value = error.message;
    } else {
      formError.value = 'Não foi possível entrar. Tente novamente.';
    }
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <main class="login-page">
    <section class="card login-card">
      <h1>Agenda de Contatos</h1>
      <p class="login-subtitle">Entre para gerenciar seus contatos.</p>

      <Message v-if="formError" severity="error" :closable="false" class="login-message">
        {{ formError }}
      </Message>

      <form novalidate @submit.prevent="submit">
        <div class="form-field">
          <label for="login-email">E-mail</label>
          <InputText
            id="login-email"
            v-model="form.email"
            class="full-width"
            type="email"
            autocomplete="username"
            :invalid="!!fieldError('email')"
          />
          <small v-if="fieldError('email')" class="field-error">{{ fieldError('email') }}</small>
        </div>

        <div class="form-field">
          <label for="login-password">Senha</label>
          <InputText
            id="login-password"
            v-model="form.password"
            class="full-width"
            type="password"
            autocomplete="current-password"
            :invalid="!!fieldError('password')"
          />
          <small v-if="fieldError('password')" class="field-error">{{ fieldError('password') }}</small>
        </div>

        <Button type="submit" label="Entrar" icon="pi pi-sign-in" class="full-width" :loading="loading" />
      </form>
    </section>
  </main>
</template>

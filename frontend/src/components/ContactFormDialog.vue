<script setup>
import { computed, reactive, ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';
import { formatPhone, onlyDigits } from '../utils/phone';
import { validateContact } from '../utils/validation';

const visible = defineModel('visible', { type: Boolean, default: false });

const props = defineProps({
  // Contato em edição; null quando for um novo contato.
  contact: { type: Object, default: null },
  saving: { type: Boolean, default: false },
  // Erros devolvidos pela API: { campo: [mensagens] }
  serverErrors: { type: Object, default: () => ({}) }
});

const emit = defineEmits(['save']);

const form = reactive({ name: '', email: '', phone: '' });
const submitted = ref(false);

const isEdit = computed(() => props.contact !== null);
const title = computed(() => (isEdit.value ? 'Editar contato' : 'Novo contato'));

// Depois da primeira tentativa de envio, valida em tempo real.
const clientErrors = computed(() => (submitted.value ? validateContact(form) : {}));

watch(visible, (isVisible) => {
  if (!isVisible) {
    return;
  }
  form.name = props.contact?.name ?? '';
  form.email = props.contact?.email ?? '';
  form.phone = formatPhone(props.contact?.phone ?? '');
  submitted.value = false;
});

function fieldError(field) {
  return clientErrors.value[field] ?? props.serverErrors?.[field]?.[0] ?? '';
}

// Input nativo: garante que o texto exibido seja sempre o formatado.
function onPhoneInput(event) {
  const formatted = formatPhone(event.target.value);
  form.phone = formatted;
  event.target.value = formatted;
}

function submit() {
  submitted.value = true;

  if (Object.keys(validateContact(form)).length > 0) {
    return;
  }

  emit('save', {
    name: form.name.trim(),
    email: form.email.trim(),
    phone: onlyDigits(form.phone)
  });
}
</script>

<template>
  <Dialog
    v-model:visible="visible"
    modal
    :header="title"
    :closable="!saving"
    :style="{ width: '28rem', maxWidth: '95vw' }"
  >
    <form novalidate @submit.prevent="submit">
      <div class="form-field">
        <label for="contact-name">Nome</label>
        <InputText
          id="contact-name"
          v-model="form.name"
          class="full-width"
          maxlength="100"
          autocomplete="off"
          :invalid="!!fieldError('name')"
        />
        <small v-if="fieldError('name')" class="field-error">{{ fieldError('name') }}</small>
      </div>

      <div class="form-field">
        <label for="contact-email">E-mail</label>
        <InputText
          id="contact-email"
          v-model="form.email"
          class="full-width"
          type="email"
          maxlength="150"
          autocomplete="off"
          :invalid="!!fieldError('email')"
        />
        <small v-if="fieldError('email')" class="field-error">{{ fieldError('email') }}</small>
      </div>

      <div class="form-field">
        <label for="contact-phone">Telefone</label>
        <input
          id="contact-phone"
          class="p-inputtext p-component full-width"
          type="tel"
          inputmode="tel"
          autocomplete="off"
          placeholder="(19) 99999-8888"
          :value="form.phone"
          @input="onPhoneInput"
        />
        <small v-if="fieldError('phone')" class="field-error">{{ fieldError('phone') }}</small>
      </div>

      <div class="form-actions">
        <Button
          type="button"
          label="Cancelar"
          severity="secondary"
          outlined
          :disabled="saving"
          @click="visible = false"
        />
        <Button type="submit" label="Salvar" icon="pi pi-check" :loading="saving" />
      </div>
    </form>
  </Dialog>
</template>

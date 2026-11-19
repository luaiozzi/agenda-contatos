<script setup>
import { computed, onMounted, reactive, ref } from 'vue';
import Button from 'primevue/button';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import ContactTable from '../components/ContactTable.vue';
import ContactFormDialog from '../components/ContactFormDialog.vue';
import SearchBox from '../components/SearchBox.vue';
import { contactService } from '../services/contactService';
import { ApiError } from '../services/api';
import { useAuth } from '../composables/useAuth';

const toast = useToast();
const confirm = useConfirm();
const { email: userEmail, logout } = useAuth();

const contacts = ref([]);
const total = ref(0);
const loading = ref(false);
const query = reactive({ search: '', page: 1, pageSize: 10 });

const dialogVisible = ref(false);
const editing = ref(null);
const saving = ref(false);
const serverErrors = ref({});

const first = computed(() => (query.page - 1) * query.pageSize);

function notify(severity, summary, detail) {
  toast.add({ severity, summary, detail, life: 3500 });
}

function notifyError(error, fallback) {
  // 401: a sessão expirou. O aviso e a volta ao login são feitos pelo App.vue.
  if (error instanceof ApiError && error.status === 401) {
    return;
  }

  const message = error instanceof ApiError ? error.message : fallback;
  notify('error', 'Erro', message);
}

async function load() {
  loading.value = true;
  try {
    const result = await contactService.list({
      search: query.search,
      page: query.page,
      pageSize: query.pageSize
    });
    contacts.value = result.items;
    total.value = result.total;
  } catch (error) {
    notifyError(error, 'Não foi possível carregar os contatos.');
  } finally {
    loading.value = false;
  }
}

function onSearch(text) {
  query.search = text;
  query.page = 1;
  load();
}

function onPage(event) {
  query.page = event.page + 1;
  query.pageSize = event.rows;
  load();
}

function openCreate() {
  editing.value = null;
  serverErrors.value = {};
  dialogVisible.value = true;
}

function openEdit(contact) {
  editing.value = contact;
  serverErrors.value = {};
  dialogVisible.value = true;
}

async function onSave(payload) {
  saving.value = true;
  serverErrors.value = {};

  try {
    if (editing.value) {
      await contactService.update(editing.value.id, payload);
      notify('success', 'Sucesso', 'Contato atualizado.');
    } else {
      await contactService.create(payload);
      notify('success', 'Sucesso', 'Contato criado.');
    }
    dialogVisible.value = false;
    await load();
  } catch (error) {
    if (error instanceof ApiError && error.status === 400 && error.errors) {
      serverErrors.value = error.errors;
    } else if (error instanceof ApiError && error.status === 409) {
      serverErrors.value = { email: [error.message] };
    } else if (error instanceof ApiError && error.status === 404) {
      notify('warn', 'Atenção', 'Este contato não existe mais.');
      dialogVisible.value = false;
      await load();
    } else {
      notifyError(error, 'Não foi possível salvar o contato.');
    }
  } finally {
    saving.value = false;
  }
}

function confirmDelete(contact) {
  confirm.require({
    header: 'Confirmar exclusão',
    message: `Deseja realmente excluir "${contact.name}"?`,
    icon: 'pi pi-exclamation-triangle',
    rejectProps: { label: 'Cancelar', severity: 'secondary', outlined: true },
    acceptProps: { label: 'Excluir', severity: 'danger' },
    accept: () => remove(contact)
  });
}

async function remove(contact) {
  try {
    await contactService.remove(contact.id);
    notify('success', 'Sucesso', 'Contato excluído.');

    // Se era o último item da página, volta para a anterior.
    if (contacts.value.length === 1 && query.page > 1) {
      query.page -= 1;
    }
    await load();
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      await load();
    } else {
      notifyError(error, 'Não foi possível excluir o contato.');
    }
  }
}

onMounted(load);
</script>

<template>
  <main class="page">
    <header class="page-header">
      <div>
        <h1>Agenda de Contatos</h1>
        <p>Cadastre, consulte, edite e exclua seus contatos.</p>
      </div>
      <div class="header-actions">
        <span class="user-email" :title="userEmail">{{ userEmail }}</span>
        <Button label="Novo contato" icon="pi pi-plus" @click="openCreate" />
        <Button label="Sair" icon="pi pi-sign-out" severity="secondary" outlined @click="logout" />
      </div>
    </header>

    <div class="toolbar">
      <SearchBox placeholder="Buscar por nome, e-mail ou telefone" @search="onSearch" />
    </div>

    <section class="card">
      <ContactTable
        :contacts="contacts"
        :loading="loading"
        :total="total"
        :rows="query.pageSize"
        :first="first"
        @page="onPage"
        @edit="openEdit"
        @delete="confirmDelete"
      />
    </section>

    <ContactFormDialog
      v-model:visible="dialogVisible"
      :contact="editing"
      :saving="saving"
      :server-errors="serverErrors"
      @save="onSave"
    />
  </main>
</template>

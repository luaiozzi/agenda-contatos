<script setup>
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import { formatPhone } from '../utils/phone';

defineProps({
  contacts: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
  total: { type: Number, default: 0 },
  rows: { type: Number, default: 10 },
  first: { type: Number, default: 0 }
});

const emit = defineEmits(['page', 'edit', 'delete']);
</script>

<template>
  <DataTable
    :value="contacts"
    lazy
    paginator
    data-key="id"
    striped-rows
    :rows="rows"
    :first="first"
    :total-records="total"
    :loading="loading"
    :rows-per-page-options="[5, 10, 20, 50]"
    @page="emit('page', $event)"
  >
    <template #empty>Nenhum contato encontrado.</template>

    <Column field="name" header="Nome" />
    <Column field="email" header="E-mail" />
    <Column header="Telefone">
      <template #body="{ data }">{{ formatPhone(data.phone) }}</template>
    </Column>
    <Column header="Ações" style="width: 8rem">
      <template #body="{ data }">
        <Button
          icon="pi pi-pencil"
          text
          rounded
          severity="secondary"
          aria-label="Editar contato"
          @click="emit('edit', data)"
        />
        <Button
          icon="pi pi-trash"
          text
          rounded
          severity="danger"
          aria-label="Excluir contato"
          @click="emit('delete', data)"
        />
      </template>
    </Column>
  </DataTable>
</template>

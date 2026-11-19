<script setup>
import { onBeforeUnmount, ref, watch } from 'vue';
import IconField from 'primevue/iconfield';
import InputIcon from 'primevue/inputicon';
import InputText from 'primevue/inputtext';

const props = defineProps({
  placeholder: { type: String, default: 'Buscar...' },
  delay: { type: Number, default: 400 }
});

const emit = defineEmits(['search']);

const text = ref('');
let timer = null;

watch(text, (value) => {
  clearTimeout(timer);
  timer = setTimeout(() => emit('search', value.trim()), props.delay);
});

onBeforeUnmount(() => clearTimeout(timer));
</script>

<template>
  <IconField>
    <InputIcon class="pi pi-search" />
    <InputText v-model="text" :placeholder="placeholder" />
  </IconField>
</template>

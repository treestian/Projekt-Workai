<script setup lang="ts">
import { onMounted, ref } from 'vue'

import {
  getAccount,
  handleRedirect,
  initializeMsal,
  login,
  logout
} from '~/utils/auth'

const account = ref<any>(null)
const loading = ref(true)
const loggingIn = ref(false)
const loggingOut = ref(false)
const error = ref('')

onMounted(async () => {
  try {
    await initializeMsal()
    await handleRedirect()

    account.value = getAccount()
  } catch (err) {
    console.error('MSAL initialization error:', err)

    error.value = 'Nie udało się zainicjalizować logowania.'
  } finally {
    loading.value = false
  }
})

const handleLogin = async () => {
  if (loggingIn.value) {
    return
  }

  loggingIn.value = true
  error.value = ''

  try {
    await login()
  } catch (err) {
    console.error('Login error:', err)

    error.value = 'Logowanie nie powiodło się.'
    loggingIn.value = false
  }
}

const handleLogout = async () => {
  if (loggingOut.value) {
    return
  }

  loggingOut.value = true
  error.value = ''

  try {
    await logout()
  } catch (err) {
    console.error('Wylogowanie nie powiodło się.')
    loggingOut.value = false
  }
}
</script>

<template>
  <!-- Ładowanie -->
  <main
    v-if="loading"
    class="ml-64 min-h-screen min-w-0"
  >
    <div class="text-slate-500">
      Ładowanie...
    </div>
  </main>

  <!-- Logowanie -->
  <main
    v-else-if="!account"
    class="min-h-screen flex items-center justify-center"
  >
    <div class="w-full max-w-md p-8">
      <h1 class="mb-6 text-3xl font-bold">
        TeamsTimeBot
      </h1>

      <p class="mb-4">
        Nie jesteś zalogowany.
      </p>

      <button
        class="rounded border px-4 py-2"
        :disabled="loggingIn"
        @click="handleLogin"
      >
        {{ loggingIn ? 'Logowanie...' : 'Zaloguj przez Microsoft' }}
      </button>

      <p
        v-if="error"
        class="mt-6 text-red-600"
      >
        {{ error }}
      </p>
    </div>
  </main>

  <!-- Aplikacja -->
  <div
    v-else
    class="min-h-screen bg-slate-50"
  >
    <AppSidebar />

    <main class="ml-64 min-h-screen min-w-0">
      <NuxtPage />
    </main>
  </div>
</template>

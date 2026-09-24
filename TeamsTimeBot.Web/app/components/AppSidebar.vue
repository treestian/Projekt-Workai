<script setup lang="ts">
import { getAccessToken, getAccount, logout } from '~/utils/auth'

interface User {
  id: number
  azureId: string
  displayName?: string | null
  mail?: string | null
  userPrincipalName?: string | null
  role?: number | string | null
}

const currentUser = ref<User | null>(null)
const loadingUser = ref(true)

const userDisplayName = computed(() => {
  return currentUser.value?.displayName ||
    currentUser.value?.mail ||
    currentUser.value?.userPrincipalName ||
    'Użytkownik'
})

const userInitial = computed(() => {
  return userDisplayName.value.charAt(0).toUpperCase()
})

const userRole = computed(() => {
  const role = currentUser.value?.role

  if (role === 1 || String(role).toLowerCase() === 'admin') {
    return 'Administrator'
  }

  return 'Pracownik'
})

const loadCurrentUser = async () => {
  loadingUser.value = true

  try {
    const account = getAccount()

    if (!account) {
      return
    }

    const token = await getAccessToken()

    const users = await $fetch<User[]>(
      'http://localhost:5294/api/users',
      {
        headers: {
          Authorization: `Bearer ${token}`
        }
      }
    )

    currentUser.value =
      users.find(user => user.azureId === account.localAccountId) ?? null
  } catch (error) {
    console.error('Nie udało się pobrać danych użytkownika:', error)
  } finally {
    loadingUser.value = false
  }
}

const handleLogout = async () => {
  try {
    await logout()
  } catch (error) {
    console.error('Nie udało się wylogować:', error)
  }
}

onMounted(() => {
  loadCurrentUser()
})
</script>

<template>

  <aside class="fixed left-0 top-0 flex h-screen w-64 shrink-0 flex-col border-r border-slate-200 bg-white">

    <!-- Logo -->

    <div class="flex h-20 items-center px-6">

      <div class="flex items-center gap-3">

        <div class="flex h-9 w-9 items-center justify-center rounded-xl bg-slate-900 text-sm font-bold text-white">
          T
        </div>

        <div>

          <div class="text-sm font-semibold tracking-tight text-slate-900">
            TeamsTimeBot
          </div>

          <div class="text-xs text-slate-400">
            Work management
          </div>

        </div>

      </div>

    </div>

    <!-- Navigation -->

    <nav class="flex-1 px-3 py-4">

      <div class="mb-3 px-3 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
        Workspace
      </div>

      <div class="space-y-1">

        <NuxtLink
          to="/"
          class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-slate-500 transition hover:bg-slate-50 hover:text-slate-900"
          active-class="bg-slate-100 text-slate-900"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="1.8"
            stroke="currentColor"
            class="h-5 w-5 text-slate-700"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M3.75 3.75h6.5v6.5h-6.5v-6.5ZM13.75 3.75h6.5v6.5h-6.5v-6.5ZM3.75 13.75h6.5v6.5h-6.5v-6.5ZM13.75 13.75h6.5v6.5h-6.5v-6.5Z"
            />
          </svg>

          Dashboard
        </NuxtLink>

        <NuxtLink
          to="/reports"
          class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-slate-500 transition hover:bg-slate-50 hover:text-slate-900"
          active-class="bg-slate-100 text-slate-900"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="1.8"
            stroke="currentColor"
            class="h-5 w-5"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M12 6v6l4 2"
            />

            <circle
              cx="12"
              cy="12"
              r="8.25"
            />
          </svg>

          Raporty
        </NuxtLink>

        <NuxtLink
          to="/users"
          class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-slate-500 transition hover:bg-slate-50 hover:text-slate-900"
          active-class="bg-slate-100 text-slate-900"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="1.8"
            stroke="currentColor"
            class="h-5 w-5"
          >
            <circle
              cx="12"
              cy="8"
              r="3.25"
            />

            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M5.25 20.25c.75-3.25 3-5 6.75-5s6 1.75 6.75 5"
            />
          </svg>

          Użytkownicy
        </NuxtLink>

        <NuxtLink
          to="/tasks"
          class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-slate-500 transition hover:bg-slate-50 hover:text-slate-900"
          active-class="bg-slate-100 text-slate-900"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="1.8"
            stroke="currentColor"
            class="h-5 w-5"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M9 3.75h6M9 3.75v2.25M15 3.75v2.25M5.25 7.5h13.5v11.25a2.25 2.25 0 0 1-2.25 2.25h-9a2.25 2.25 0 0 1-2.25-2.25V7.5Z"
            />

            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M8.5 11h7M8.5 14.5h5"
            />
          </svg>

          Zadania
        </NuxtLink>

      </div>

    </nav>

    <!-- Bottom -->

    <div class="border-t border-slate-100 p-4">

      <!-- User -->

      <div class="flex items-center gap-3 rounded-xl px-2 py-2">

        <div
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-slate-200 text-xs font-semibold text-slate-700"
        >
          {{ loadingUser ? '...' : userInitial }}
        </div>

        <div class="min-w-0">

          <div class="truncate text-sm font-medium text-slate-800">
            {{ loadingUser ? 'Ładowanie...' : userDisplayName }}
          </div>

          <div class="truncate text-xs text-slate-400">
            {{ loadingUser ? '' : userRole }}
          </div>

        </div>

      </div>

      <!-- Logout -->

      <button
        type="button"
        class="mt-2 flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-slate-500 transition hover:bg-red-50 hover:text-red-600"
        @click="handleLogout"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke-width="1.8"
          stroke="currentColor"
          class="h-5 w-5"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M15.75 8.25V6A2.25 2.25 0 0 0 13.5 3.75h-6A2.25 2.25 0 0 0 5.25 6v12A2.25 2.25 0 0 0 7.5 20.25h6A2.25 2.25 0 0 0 15.75 18v-2.25"
          />

          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M12 12h6.75M16.5 8.25 20.25 12 16.5 15.75"
          />
        </svg>

        Wyloguj
      </button>

    </div>

  </aside>

</template>

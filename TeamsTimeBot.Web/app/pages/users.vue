<script setup lang="ts">
import type {
	User,
	UserSyncResult,
	UserSyncSettings
} from '~/types/users'

const {
	users,
	loading,
	error,
	fetchUsers,
	syncUsers,
	fetchSyncSettings,
	updateSyncSettings
} = useUsers()

const search = ref('')
const statusFilter = ref<'all' | 'active' | 'inactive'>('all')
const intervalHours = ref(24)
const settingsLoading = ref(true)
const settingsSaving = ref(false)
const settingsError = ref<string | null>(null)
const syncMessage = ref<string | null>(null)
const lastSyncResult = ref<UserSyncResult | null>(null)

const filteredUsers = computed(() => {
	const query = search.value.trim().toLowerCase()

	return users.value.filter((user) => {
		const matchesStatus = statusFilter.value === 'all'
			|| (statusFilter.value === 'active' && user.isActive)
			|| (statusFilter.value === 'inactive' && !user.isActive)

		const searchable = [
			user.displayName,
			user.email,
			user.userPrincipalName
		].filter(Boolean).join(' ').toLowerCase()

		return matchesStatus && (!query || searchable.includes(query))
	})
})

const activeCount = computed(() => users.value.filter(user => user.isActive).length)
const inactiveCount = computed(() => users.value.filter(user => !user.isActive).length)

const displayName = (user: User) => (
	user.displayName || user.email || user.userPrincipalName || 'Bez nazwy'
)

const email = (user: User) => user.email || user.userPrincipalName || 'Brak adresu'

const formatDate = (value?: string) => {
	if (!value) return 'Brak danych'

	return new Intl.DateTimeFormat('pl-PL', {
		dateStyle: 'medium',
		timeStyle: 'short'
	}).format(new Date(value))
}

const runSync = async () => {
	syncMessage.value = null

	try {
		lastSyncResult.value = await syncUsers()
		syncMessage.value = 'Synchronizacja zakończona pomyślnie.'
	} catch {
		syncMessage.value = null
	}
}

const saveSettings = async () => {
	settingsSaving.value = true
	settingsError.value = null

	try {
		const settings = await updateSyncSettings(intervalHours.value)
		intervalHours.value = settings.intervalHours
	} catch {
		settingsError.value = 'Nie udało się zapisać ustawień synchronizacji.'
	} finally {
		settingsSaving.value = false
	}
}

onMounted(async () => {
	settingsLoading.value = true

	try {
		await Promise.all([
			fetchUsers(),
			fetchSyncSettings().then((settings: UserSyncSettings) => {
				intervalHours.value = settings.intervalHours
			})
		])
	} catch {
		settingsError.value = 'Nie udało się pobrać ustawień synchronizacji.'
	} finally {
		settingsLoading.value = false
	}
})
</script>

<template>
	<div class="min-h-screen px-5 py-7 sm:px-8 lg:px-10">
		<div class="w-full">
			<header class="mb-8 flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
				<div>
					<p class="mb-2 text-xs font-bold uppercase tracking-[0.18em] text-teal-600">
						Workspace / Administracja
					</p>
					<h1 class="text-3xl font-semibold tracking-tight text-slate-950">
						Użytkownicy
					</h1>
					<p class="mt-2 max-w-xl text-sm leading-6 text-slate-500">
						Zarządzaj kontami synchronizowanymi z Azure Active Directory.
					</p>
				</div>

				<button
					type="button"
					:disabled="loading"
					class="inline-flex h-11 items-center justify-center gap-2 rounded-xl bg-slate-950 px-4 text-sm font-semibold text-white shadow-sm transition hover:bg-teal-700 disabled:cursor-wait disabled:opacity-60"
					@click="runSync"
				>
					<svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
						<path stroke-linecap="round" stroke-linejoin="round" d="M20 11a8.1 8.1 0 0 0-14.9-4M4 5v4h4M4 13a8.1 8.1 0 0 0 14.9 4M20 19v-4h-4" />
					</svg>
					{{ loading ? 'Synchronizuję...' : 'Synchronizuj teraz' }}
				</button>
			</header>

			<div v-if="error" class="mb-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
				{{ error }}
			</div>
			<div v-if="syncMessage" class="mb-5 rounded-xl border border-teal-200 bg-teal-50 px-4 py-3 text-sm text-teal-800">
				{{ syncMessage }}
				<span v-if="lastSyncResult" class="text-teal-700">
					Dodano {{ lastSyncResult.added }}, zaktualizowano {{ lastSyncResult.updated }}, dezaktywowano {{ lastSyncResult.deactivated }}.
				</span>
			</div>

			<section class="mb-6 grid gap-4 sm:grid-cols-3">
				<div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
					<p class="text-sm font-medium text-slate-500">Wszystkie konta</p>
					<p class="mt-3 text-3xl font-semibold tracking-tight text-slate-950">{{ users.length }}</p>
					<p class="mt-1 text-xs text-slate-400">Zapisane w lokalnej bazie</p>
				</div>
				<div class="rounded-2xl border border-teal-100 bg-teal-50 p-5 shadow-sm">
					<p class="text-sm font-medium text-teal-800">Aktywne</p>
					<p class="mt-3 text-3xl font-semibold tracking-tight text-teal-950">{{ activeCount }}</p>
					<p class="mt-1 text-xs text-teal-700/70">Konta obecne w AAD</p>
				</div>
				<div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
					<p class="text-sm font-medium text-slate-500">Usunięte z AAD</p>
					<p class="mt-3 text-3xl font-semibold tracking-tight text-slate-950">{{ inactiveCount }}</p>
					<p class="mt-1 text-xs text-slate-400">Zachowane dla historii danych</p>
				</div>
			</section>

			<section class="mb-6 grid gap-6 lg:grid-cols-[1fr_340px]">
				<div class="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
					<div class="flex flex-col gap-4 border-b border-slate-100 p-5 sm:flex-row sm:items-center sm:justify-between">
						<div>
							<h2 class="font-semibold text-slate-900">Lista użytkowników</h2>
							<p class="mt-1 text-sm text-slate-400">Dane z ostatniej synchronizacji lokalnej bazy.</p>
						</div>
						<div class="flex gap-2">
							<div class="relative min-w-0 flex-1 sm:w-52 sm:flex-none">
								<svg class="pointer-events-none absolute left-3 top-3 h-4 w-4 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><circle cx="11" cy="11" r="7" /><path stroke-linecap="round" d="m20 20-4-4" /></svg>
								<input v-model="search" type="search" placeholder="Szukaj..." class="h-10 w-full rounded-lg border border-slate-200 pl-9 pr-3 text-sm outline-none transition placeholder:text-slate-400 focus:border-teal-500 focus:ring-2 focus:ring-teal-100" />
							</div>
							<select v-model="statusFilter" class="h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm text-slate-600 outline-none focus:border-teal-500">
								<option value="all">Wszyscy</option>
								<option value="active">Aktywni</option>
								<option value="inactive">Usunięci</option>
							</select>
						</div>
					</div>

					<div class="overflow-x-auto">
						<table class="w-full min-w-[680px] text-left text-sm">
							<thead class="bg-slate-50 text-xs uppercase tracking-wider text-slate-400">
								<tr>
									<th class="px-5 py-3 font-semibold">Użytkownik</th>
									<th class="px-5 py-3 font-semibold">Status</th>
									<th class="px-5 py-3 font-semibold">Dodano</th>
									<th class="px-5 py-3 font-semibold">Ostatnia zmiana</th>
								</tr>
							</thead>
							<tbody class="divide-y divide-slate-100">
								<tr v-for="user in filteredUsers" :key="user.id" class="transition hover:bg-slate-50/70">
									<td class="px-5 py-4">
										<div class="flex items-center gap-3">
											<div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-slate-100 text-xs font-bold text-slate-600">
												{{ displayName(user).charAt(0).toUpperCase() }}
											</div>
											<div class="min-w-0">
												<p class="truncate font-semibold text-slate-800">{{ displayName(user) }}</p>
												<p class="truncate text-xs text-slate-400">{{ email(user) }}</p>
											</div>
										</div>
									</td>
									<td class="px-5 py-4">
										<span :class="user.isActive ? 'bg-teal-50 text-teal-700' : 'bg-slate-100 text-slate-500'" class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold">
											<span :class="user.isActive ? 'bg-teal-500' : 'bg-slate-400'" class="h-1.5 w-1.5 rounded-full" />
											{{ user.isActive ? 'Aktywny' : 'Usunięty' }}
										</span>
									</td>
									<td class="px-5 py-4 text-slate-500">{{ formatDate(user.createdAt) }}</td>
									<td class="px-5 py-4 text-slate-500">{{ formatDate(user.updatedAt) }}</td>
								</tr>
								<tr v-if="filteredUsers.length === 0">
									<td colspan="4" class="px-5 py-12 text-center text-sm text-slate-400">Brak użytkowników pasujących do filtrów.</td>
								</tr>
							</tbody>
						</table>
					</div>
				</div>

				<aside class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
					<div class="mb-6 flex items-start justify-between gap-4">
						<div>
							<h2 class="font-semibold text-slate-900">Synchronizacja AAD</h2>
							<p class="mt-1 text-sm leading-5 text-slate-400">Ustaw, jak często bot ma odświeżać konta.</p>
						</div>
						<div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-teal-50 text-teal-700">
							<svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><circle cx="12" cy="12" r="8.25" /><path stroke-linecap="round" d="M12 7v5l3 2" /></svg>
						</div>
					</div>
					<label class="mb-2 block text-xs font-bold uppercase tracking-wider text-slate-400" for="sync-interval">Interwał synchronizacji</label>
					<div class="flex items-center gap-2">
						<input id="sync-interval" v-model.number="intervalHours" min="1" max="168" type="number" :disabled="settingsLoading || settingsSaving" class="h-11 w-24 rounded-xl border border-slate-200 px-3 text-sm font-semibold text-slate-800 outline-none focus:border-teal-500 focus:ring-2 focus:ring-teal-100 disabled:bg-slate-50" />
						<span class="text-sm text-slate-500">godzin</span>
					</div>
					<p class="mt-2 text-xs leading-5 text-slate-400">Dozwolony zakres: od 1 do 168 godzin.</p>
					<button type="button" :disabled="settingsSaving || settingsLoading" class="mt-5 h-10 w-full rounded-xl border border-slate-200 text-sm font-semibold text-slate-700 transition hover:border-teal-300 hover:bg-teal-50 disabled:cursor-wait disabled:opacity-60" @click="saveSettings">
						{{ settingsSaving ? 'Zapisywanie...' : 'Zapisz ustawienia' }}
					</button>
					<p v-if="settingsError" class="mt-3 text-xs text-red-500">{{ settingsError }}</p>
				</aside>
			</section>
		</div>
	</div>
</template>

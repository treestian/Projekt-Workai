import type {
  User,
  UserSyncResult,
  UserSyncSettings
} from '~/types/users'

export const useUsers = () => {
  const users = ref<User[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  const fetchUsers = async () => {
    loading.value = true
    error.value = null

    try {
      users.value = await $fetch<User[]>(
        'http://localhost:5294/api/users'
      )
    }
    catch (err) {
      console.error('BŁĄD USERS:', err)
      error.value = 'Nie udało się pobrać użytkowników.'
    } finally {
      loading.value = false
    }
  }

  const syncUsers = async () => {
    loading.value = true
    error.value = null

    try {
      const result = await $fetch<UserSyncResult>(
        'http://localhost:5294/api/users/sync',
        { method: 'POST' }
      )

      await fetchUsers()
      return result
    } catch (err) {
      console.error('BŁĄD SYNCHRONIZACJI:', err)
      error.value = 'Nie udało się zsynchronizować użytkowników.'
      throw err
    } finally {
      loading.value = false
    }
  }

  const fetchSyncSettings = () => $fetch<UserSyncSettings>(
    'http://localhost:5294/api/users/sync-settings'
  )

  const updateSyncSettings = (intervalHours: number) => $fetch<UserSyncSettings>(
    'http://localhost:5294/api/users/sync-settings',
    {
      method: 'PUT',
      body: { intervalHours }
    }
  )

  return {
    users,
    loading,
    error,
    fetchUsers,
    syncUsers,
    fetchSyncSettings,
    updateSyncSettings
  }
}
import type {
  User,
  UserSyncResult,
  UserSyncSettings
} from '~/types/users'

import { getAccessToken } from '~/utils/auth'

export const useUsers = () => {
  const users = ref<User[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  const fetchUsers = async () => {
    loading.value = true
    error.value = null

    try {
      const token = await getAccessToken()

      users.value = await $fetch<User[]>(
        'http://localhost:5294/api/users',
        {
          headers: {
            Authorization: `Bearer ${token}`
          }
        }
      )
    } catch (err) {
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
      const token = await getAccessToken()

      const result = await $fetch<UserSyncResult>(
        'http://localhost:5294/api/users/sync',
        {
          method: 'POST',
          headers: {
            Authorization: `Bearer ${token}`
          }
        }
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

  const fetchSyncSettings = async () => {
    const token = await getAccessToken()

    return await $fetch<UserSyncSettings>(
      'http://localhost:5294/api/users/sync-settings',
      {
        headers: {
          Authorization: `Bearer ${token}`
        }
      }
    )
  }

  const updateSyncSettings = async (intervalHours: number) => {
    const token = await getAccessToken()

    return await $fetch<UserSyncSettings>(
      'http://localhost:5294/api/users/sync-settings',
      {
        method: 'PUT',
        headers: {
          Authorization: `Bearer ${token}`
        },
        body: {
          intervalHours
        }
      }
    )
  }

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

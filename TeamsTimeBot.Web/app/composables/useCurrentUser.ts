import type { User } from '~/types/users'

import { getAccessToken } from '~/utils/auth'

let inFlightRequest: Promise<User | null> | null = null

export const useCurrentUser = () => {
  const currentUser = useState<User | null>(
    'currentUser',
    () => null
  )

  const loading = useState<boolean>(
    'currentUserLoading',
    () => false
  )

  const error = useState<string | null>(
    'currentUserError',
    () => null
  )

  const config = useRuntimeConfig()
  const apiUrl = config.public.apiUrl

  const isAdmin = computed(
    () => currentUser.value?.role === 'Admin'
  )

  const load = async (): Promise<User | null> => {
    loading.value = true
    error.value = null

    try {
      const token = await getAccessToken()

      currentUser.value = await $fetch<User>(
        `${apiUrl}/api/users/me`,
        {
          headers: {
            Authorization: `Bearer ${token}`
          }
        }
      )

      return currentUser.value
    } catch (err: any) {
      currentUser.value = null

      const status = err?.statusCode ?? err?.response?.status

      error.value = status === 404
        ? 'Twoje konto nie zostało jeszcze zsynchronizowane z Azure AD. Skontaktuj się z administratorem.'
        : 'Nie udało się pobrać danych zalogowanego użytkownika.'

      console.error('Nie udało się pobrać /api/users/me:', err)

      return null
    } finally {
      loading.value = false
    }
  }

  const fetchCurrentUser = async (
    force = false
  ): Promise<User | null> => {
    if (currentUser.value && !force) {
      return currentUser.value
    }

    if (!inFlightRequest) {
      inFlightRequest = load().finally(() => {
        inFlightRequest = null
      })
    }

    return inFlightRequest
  }

  return {
    currentUser,
    loading,
    error,
    isAdmin,
    fetchCurrentUser
  }
}

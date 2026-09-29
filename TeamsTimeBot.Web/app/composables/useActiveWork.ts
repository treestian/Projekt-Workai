import { getAccessToken } from '~/utils/auth'

export interface ActiveWorkItem {
  id: number
  startedAt: string
  status: string
  task?: {
    id: number
    name: string
  } | null
  user?: {
    id: number
    displayName?: string | null
    email?: string | null
  } | null
}

export const useActiveWork = () => {
  const activeWork = ref<ActiveWorkItem[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  const config = useRuntimeConfig()
  const apiUrl = config.public.apiUrl

  const fetchActiveWork = async () => {
    loading.value = true
    error.value = null

    try {
      const token = await getAccessToken()

      activeWork.value = await $fetch<ActiveWorkItem[]>(
        `${apiUrl}/api/worklogs/active`,
        {
          headers: {
            Authorization: `Bearer ${token}`
          }
        }
      )
    } catch (err) {
      console.error('Błąd pobierania aktywności:', err)

      error.value = 'Nie udało się pobrać aktywności zespołu.'
    } finally {
      loading.value = false
    }
  }

  return {
    activeWork,
    loading,
    error,
    fetchActiveWork
  }
}


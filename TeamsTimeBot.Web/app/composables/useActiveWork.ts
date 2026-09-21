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

  const fetchActiveWork = async () => {
    loading.value = true
    error.value = null

    try {
      activeWork.value = await $fetch<ActiveWorkItem[]>(
        'http://localhost:5294/api/worklogs/active'
      )
    } catch {
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

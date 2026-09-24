import type { TaskItem } from '../types/tasks'
import { getAccessToken } from '~/utils/auth'

export const useTasks = () => {
  const tasks = ref<TaskItem[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  const fetchTasks = async () => {
    loading.value = true
    error.value = null

    try {
      const token = await getAccessToken()

      tasks.value = await $fetch<TaskItem[]>(
        'http://localhost:5294/api/tasks',
        {
          headers: {
            Authorization: `Bearer ${token}`
          }
        }
      )
    } catch (err) {
      console.error('Błąd pobierania zadań:', err)

      error.value = 'Nie udało się pobrać zadań.'
    } finally {
      loading.value = false
    }
  }

  return {
    tasks,
    loading,
    error,
    fetchTasks
  }
}

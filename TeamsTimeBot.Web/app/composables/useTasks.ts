import type { TaskItem } from '../types/tasks'

export const useTasks = () => {
  const tasks = ref<TaskItem[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  const fetchTasks = async () => {
    loading.value = true
    error.value = null

    try {
      tasks.value = await $fetch<TaskItem[]>(
        'http://localhost:5294/api/tasks'
      )
    } catch {
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
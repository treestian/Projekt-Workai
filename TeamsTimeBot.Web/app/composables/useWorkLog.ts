import type { WorkLogSummary } from '~/types/worklogs'

export const useWorkLogs = () => {
  const report = ref<WorkLogSummary | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const fetchSummary = async (
    userAzureId: string,
    startDate: string,
    endDate: string
  ) => {
    loading.value = true
    error.value = null

    try {
      const response = await $fetch<WorkLogSummary>(
        'http://localhost:5294/api/worklogs/summary',
        {
          query: {
            userAzureId,
            startDate,
            endDate
          }
        }
      )

      console.log('RAPORT Z API:', response)

      report.value = response
    } catch (err) {
      console.error('BŁĄD RAPORTU:', err)

      error.value = 'Nie udało się pobrać raportu.'
      report.value = null
    } finally {
      loading.value = false
    }
  }

  return {
    report,
    loading,
    error,
    fetchSummary
  }
}
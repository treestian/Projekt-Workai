import type { WorkLogSummary } from '~/types/worklogs'

import { getAccessToken } from '~/utils/auth'

export const useWorkLogs = () => {
  const report = ref<WorkLogSummary | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const config = useRuntimeConfig()
  const apiUrl = config.public.apiUrl

  const fetchSummary = async (
    userAzureId: string,
    startDate: string,
    endDate: string
  ) => {
    loading.value = true
    error.value = null

    try {
      const token = await getAccessToken()

      const response = await $fetch<WorkLogSummary>(
        `${apiUrl}/api/worklogs/summary`,
        {
          headers: {
            Authorization: `Bearer ${token}`
          },
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


<script setup lang="ts">
import type { User } from '~/types/users'

import { getAccessToken } from '~/utils/auth'

const { isAdmin, fetchCurrentUser } = useCurrentUser()

const users = ref<User[]>([])
const loadingUsers = ref(false)
const usersError = ref<string | null>(null)
const selectedUser = ref('')
const config = useRuntimeConfig()
const apiUrl = config.public.apiUrl

const formatDateInput = (date: Date) => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}

const today = new Date()

const defaultStartDate = new Date(today)
defaultStartDate.setDate(today.getDate() - 13)

const startDate = ref(formatDateInput(defaultStartDate))
const endDate = ref(formatDateInput(today))

const exportingPdf = ref(false)

const {
  report,
  loading,
  error,
  fetchSummary
} = useWorkLogs()

const fetchUsers = async () => {
  loadingUsers.value = true
  usersError.value = null

  try {
    const me = await fetchCurrentUser()

    if (!me) {
      usersError.value =
        'Nie udało się ustalić zalogowanego użytkownika.'

      return
    }

    if (!isAdmin.value) {
      users.value = [me]
      selectedUser.value = me.azureId

      return
    }

    const token = await getAccessToken()

    users.value = await $fetch<User[]>(
      `${apiUrl}/api/users`,
      {
        headers: {
          Authorization: `Bearer ${token}`
        }
      }
    )

    selectedUser.value =
      users.value.find(user => user.azureId === me.azureId)?.azureId ??
      users.value[0]?.azureId ??
      ''
  } catch (err) {
    console.error('Błąd pobierania użytkowników:', err)

    usersError.value = 'Nie udało się pobrać użytkowników.'
  } finally {
    loadingUsers.value = false
  }
}

const generateReport = async () => {
  if (!selectedUser.value || !startDate.value || !endDate.value) {
    return
  }

  if (startDate.value > endDate.value) {
    error.value =
      'Data początkowa nie może być późniejsza niż końcowa.'

    return
  }

  await fetchSummary(
    selectedUser.value,
    startDate.value,
    endDate.value
  )
}

const formatMinutes = (minutes: number) => {
  const hours = Math.floor(minutes / 60)
  const remainingMinutes = minutes % 60

  if (!hours) {
    return `${remainingMinutes}m`
  }

  return remainingMinutes
    ? `${hours}h ${remainingMinutes}m`
    : `${hours}h`
}

const tasks = computed(() => report.value?.tasks ?? [])

const totalMinutes = computed(
  () => report.value?.totalMinutes ?? 0
)

const totalHours = computed(
  () => report.value?.totalHours ?? 0
)

const selectedUserName = computed(() => {
  const user = users.value.find(
    item => item.azureId === selectedUser.value
  )

  return (
    user?.displayName ||
    user?.email ||
    user?.userPrincipalName ||
    'Nieznany użytkownik'
  )
})

const averageMinutesPerDay = computed(() => {
  if (!report.value || !totalMinutes.value) {
    return 0
  }

  const start = new Date(
    `${startDate.value}T00:00:00`
  ).getTime()

  const end = new Date(
    `${endDate.value}T00:00:00`
  ).getTime()

  const days =
    Math.floor((end - start) / 86400000) + 1

  return days > 0
    ? Math.round(totalMinutes.value / days)
    : 0
})

const maxTaskMinutes = computed(() =>
  Math.max(
    0,
    ...tasks.value.map(task => task.minutes)
  )
)

const getTaskWidth = (minutes: number) =>
  maxTaskMinutes.value
    ? Math.max(
        5,
        Math.round(
          (minutes / maxTaskMinutes.value) * 100
        )
      )
    : 0

const getTaskPercentage = (minutes: number) =>
  totalMinutes.value
    ? Math.round(
        (minutes / totalMinutes.value) * 100
      )
    : 0

const chartColors = [
  '#0f766e',
  '#f97316',
  '#2563eb',
  '#a855f7',
  '#eab308',
  '#dc2626',
  '#0891b2'
]

const chartItems = computed(() => {
  const importantTasks = tasks.value
    .slice()
    .sort(
      (firstTask, secondTask) =>
        secondTask.minutes - firstTask.minutes
    )
    .slice(0, 3)

  const otherMinutes =
    tasks.value.reduce(
      (sum, task) => sum + task.minutes,
      0
    ) -
    importantTasks.reduce(
      (sum, task) => sum + task.minutes,
      0
    )

  return otherMinutes > 0
    ? [
        ...importantTasks,
        {
          taskId: -1,
          taskName: 'Inne',
          minutes: otherMinutes,
          hours: otherMinutes / 60
        }
      ]
    : importantTasks
})

const chartSegments = computed(() => {
  let currentPercentage = 0

  const segments = chartItems.value.map(
    (task, index) => {
      const percentage = totalMinutes.value
        ? (task.minutes / totalMinutes.value) * 100
        : 0

      const endPercentage =
        index === chartItems.value.length - 1
          ? 100
          : currentPercentage + percentage

      const segment = `${chartColors[index % chartColors.length]} ${currentPercentage}% ${endPercentage}%`

      currentPercentage = endPercentage

      return segment
    }
  )

  return segments.join(', ')
})

const exportPdfUnicode = async () => {
  if (!report.value || exportingPdf.value) {
    return
  }

  exportingPdf.value = true

  try {
    const pdfMakeModule =
      await import('pdfmake/build/pdfmake')

    const pdfFontsModule =
      await import('pdfmake/build/vfs_fonts')

    const pdfMake =
      (pdfMakeModule.default ?? pdfMakeModule) as any

    const pdfFonts =
      (pdfFontsModule.default ?? pdfFontsModule) as any

    pdfMake.vfs =
      pdfFonts.pdfMake?.vfs ??
      pdfFonts.vfs

    const chartColumns =
      chartItems.value.map((task, index) => ({
        text: '',
        fillColor:
          chartColors[index % chartColors.length] ??
          '#0f766e',
        margin: [0, 0, 0, 0]
      }))

    const chartLegend =
      chartItems.value.map((task, index) => ({
        columns: [
          {
            width: 10,
            text: '',
            fillColor:
              chartColors[
                index % chartColors.length
              ] ?? '#0f766e',
            margin: [0, 2, 0, 0]
          },
          {
            width: '*',
            text: `${task.taskName}: ${getTaskPercentage(task.minutes)}%`,
            color: '#334155'
          }
        ],
        margin: [0, 3, 0, 0]
      }))

    const taskRows = tasks.value.map(task => ({
      columns: [
        {
          width: '*',
          text: task.taskName,
          color: '#334155'
        },
        {
          width: 60,
          text: formatMinutes(task.minutes),
          alignment: 'right',
          color: '#64748b'
        }
      ],
      margin: [0, 4, 0, 4]
    }))

    const documentDefinition = {
      pageSize: 'A4',

      pageMargins: [40, 40, 40, 40],

      defaultStyle: {
        font: 'Roboto',
        fontSize: 9,
        color: '#334155'
      },

      content: [
        {
          text: 'WORKSPACE / ANALITYKA',
          color: '#0f766e',
          bold: true,
          fontSize: 9,
          characterSpacing: 1.5
        },

        {
          text: 'Raport czasu pracy',
          color: '#0f172a',
          bold: true,
          fontSize: 24,
          margin: [0, 8, 0, 6]
        },

        {
          text: `${selectedUserName.value}  |  ${startDate.value} - ${endDate.value}`,
          color: '#64748b',
          margin: [0, 0, 0, 16]
        },

        {
          table: {
            widths: ['*', '*', '*'],

            body: [
              [
                {
                  text: [
                    {
                      text: 'Łączny czas\n',
                      color: '#64748b'
                    },
                    {
                      text:
                        formatMinutes(
                          totalMinutes.value
                        ),
                      bold: true,
                      fontSize: 16,
                      color: '#0f172a'
                    }
                  ],
                  fillColor: '#ffffff',
                  margin: 10
                },

                {
                  text: [
                    {
                      text: 'Zadania\n',
                      color: '#64748b'
                    },
                    {
                      text: String(
                        tasks.value.length
                      ),
                      bold: true,
                      fontSize: 16,
                      color: '#0f172a'
                    }
                  ],
                  fillColor: '#ffffff',
                  margin: 10
                },

                {
                  text: [
                    {
                      text: 'Średnio dziennie\n',
                      color: '#64748b'
                    },
                    {
                      text:
                        formatMinutes(
                          averageMinutesPerDay.value
                        ),
                      bold: true,
                      fontSize: 16,
                      color: '#0f172a'
                    }
                  ],
                  fillColor: '#ffffff',
                  margin: 10
                }
              ]
            ]
          },

          layout: {
            hLineColor: '#e2e8f0',
            vLineColor: '#e2e8f0',
            hLineWidth: () => 1,
            vLineWidth: () => 1
          }
        },

        {
          text: 'Udział czasu',
          color: '#0f172a',
          bold: true,
          fontSize: 13,
          margin: [0, 24, 0, 4]
        },

        {
          text:
            'Najważniejsze zadania oraz pozostały czas pracy.',
          color: '#64748b',
          margin: [0, 0, 0, 10]
        },

        {
          table: {
            widths: chartItems.value.map(
              task =>
                `${Math.max(
                  1,
                  getTaskPercentage(
                    task.minutes
                  )
                )}%`
            ),

            body: [chartColumns]
          },

          layout: 'noBorders',

          margin: [0, 0, 0, 10],

          heights: 14
        },

        ...chartLegend,

        {
          text: 'Czas według zadań',
          color: '#0f172a',
          bold: true,
          fontSize: 13,
          margin: [0, 24, 0, 8]
        },

        ...taskRows
      ]
    }

    pdfMake
      .createPdf(documentDefinition)
      .download(
        `raport-${selectedUserName.value
          .replace(/\s+/g, '-')
          .toLowerCase()}-${startDate.value}-${endDate.value}.pdf`
      )
  } finally {
    exportingPdf.value = false
  }
}

onMounted(fetchUsers)
</script>

<template>
  <div class="min-h-screen px-5 py-7 sm:px-8 lg:px-10">
    <div class="w-full">
      <header
        class="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-end"
      >
        <div>
          <p
            class="mb-2 text-xs font-bold uppercase tracking-[0.18em] text-teal-600"
          >
            Workspace / Analityka
          </p>

          <h1
            class="text-3xl font-semibold tracking-tight text-slate-950"
          >
            Raporty
          </h1>

          <p class="mt-2 text-sm text-slate-500">
            Analizuj czas pracy nad zadaniami w wybranym
            okresie.
          </p>
        </div>

        <p class="text-sm text-slate-400">
          Użytkownik:
          <span class="font-medium text-slate-600">
            {{ selectedUserName }}
          </span>
        </p>
      </header>

      <section
        class="mb-6 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
      >
        <div
          class="grid gap-4"
          :class="
            isAdmin
              ? 'lg:grid-cols-[1.5fr_1fr_1fr_auto]'
              : 'lg:grid-cols-[1fr_1fr_auto]'
          "
        >
          <label
            v-if="isAdmin"
            class="text-xs font-semibold uppercase tracking-wider text-slate-400"
          >
            Użytkownik

            <select
              v-model="selectedUser"
              :disabled="loadingUsers"
              class="mt-2 h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm font-medium text-slate-700 outline-none focus:border-teal-500 disabled:bg-slate-50"
            >
              <option
                value=""
                disabled
              >
                {{
                  loadingUsers
                    ? 'Ładowanie użytkowników...'
                    : 'Wybierz użytkownika'
                }}
              </option>

              <option
                v-for="user in users"
                :key="user.azureId"
                :value="user.azureId"
              >
                {{
                  user.displayName ||
                  user.email ||
                  user.userPrincipalName ||
                  'Bez nazwy'
                }}
              </option>
            </select>
          </label>

          <label
            class="text-xs font-semibold uppercase tracking-wider text-slate-400"
          >
            Od

            <input
              v-model="startDate"
              type="date"
              class="mt-2 h-11 w-full rounded-xl border border-slate-200 px-3 text-sm text-slate-700 outline-none focus:border-teal-500"
            />
          </label>

          <label
            class="text-xs font-semibold uppercase tracking-wider text-slate-400"
          >
            Do

            <input
              v-model="endDate"
              type="date"
              class="mt-2 h-11 w-full rounded-xl border border-slate-200 px-3 text-sm text-slate-700 outline-none focus:border-teal-500"
            />
          </label>

          <div
            class="flex flex-col gap-2 self-end sm:flex-row"
          >
            <button
              type="button"
              :disabled="loading || !selectedUser"
              class="h-11 rounded-xl bg-slate-900 px-5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:bg-slate-300"
              @click="generateReport"
            >
              {{
                loading
                  ? 'Generowanie...'
                  : 'Generuj raport'
              }}
            </button>

            <button
              type="button"
              :disabled="!report || exportingPdf"
              class="h-11 rounded-xl border border-slate-200 bg-white px-5 text-sm font-semibold text-slate-700 transition hover:border-teal-500 hover:text-teal-700 disabled:cursor-not-allowed disabled:opacity-50"
              @click="exportPdfUnicode"
            >
              {{
                exportingPdf
                  ? 'Tworzenie PDF...'
                  : 'Pobierz PDF'
              }}
            </button>
          </div>
        </div>

        <p
          v-if="usersError || error"
          class="mt-3 text-sm text-red-500"
        >
          {{ usersError || error }}
        </p>
      </section>

      <section class="mb-6 grid gap-4 md:grid-cols-3">
        <div
          v-for="stat in [
            {
              label: 'Łączny czas',
              value: report
                ? formatMinutes(totalMinutes)
                : '—',
              hint: 'Wybrany okres'
            },
            {
              label: 'Zadania',
              value: report
                ? tasks.length
                : '—',
              hint: 'Z zalogowanym czasem pracy'
            },
            {
              label: 'Średnio dziennie',
              value: report
                ? formatMinutes(
                    averageMinutesPerDay
                  )
                : '—',
              hint: 'Na dzień w wybranym okresie'
            }
          ]"
          :key="stat.label"
          class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
        >
          <p class="text-sm text-slate-500">
            {{ stat.label }}
          </p>

          <p
            class="mt-3 text-3xl font-semibold tracking-tight text-slate-950"
          >
            {{ stat.value }}
          </p>

          <p class="mt-2 text-xs text-slate-400">
            {{ stat.hint }}
          </p>
        </div>
      </section>

      <div
        class="grid gap-6 lg:grid-cols-[1.6fr_1fr]"
      >
        <section
          class="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
        >
          <h2 class="font-semibold text-slate-900">
            Czas według zadań
          </h2>

          <p class="mt-1 text-sm text-slate-400">
            Rozkład czasu pracy pomiędzy zadania.
          </p>

          <div
            v-if="!report || !tasks.length"
            class="flex min-h-40 items-center justify-center text-sm text-slate-400"
          >
            Wygeneruj raport, aby zobaczyć zadania.
          </div>

          <div
            v-else
            class="mt-6 space-y-5"
          >
            <div
              v-for="task in tasks"
              :key="task.taskId"
            >
              <div
                class="mb-2 flex justify-between gap-4 text-sm"
              >
                <span
                  class="truncate font-medium text-slate-700"
                >
                  {{ task.taskName }}
                </span>

                <span
                  class="shrink-0 text-slate-400"
                >
                  {{ formatMinutes(task.minutes) }}
                </span>
              </div>

              <div
                class="h-2 overflow-hidden rounded-full bg-slate-100"
              >
                <div
                  class="h-full rounded-full bg-slate-900"
                  :style="{
                    width: `${getTaskWidth(task.minutes)}%`
                  }"
                />
              </div>
            </div>
          </div>
        </section>

        <section
          class="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
        >
          <div
            class="flex items-start justify-between gap-3"
          >
            <div>
              <h2 class="font-semibold text-slate-900">
                Udział czasu
              </h2>

              <p class="mt-1 text-sm text-slate-400">
                Porównanie czasu po zadaniach.
              </p>
            </div>

            <span
              class="rounded-full bg-teal-50 px-2.5 py-1 text-xs font-semibold text-teal-700"
            >
              {{ tasks.length }} zadań
            </span>
          </div>

          <div
            v-if="!report || !tasks.length"
            class="flex min-h-40 items-center justify-center text-sm text-slate-400"
          >
            Brak danych do wyświetlenia.
          </div>

          <template v-else>
            <div
              class="flex items-center justify-center py-8"
            >
              <div
                class="relative flex h-48 w-48 items-center justify-center rounded-full"
                :style="{
                  background: `conic-gradient(${chartSegments})`
                }"
              >
                <div
                  class="flex h-32 w-32 flex-col items-center justify-center rounded-full bg-white shadow-inner"
                >
                  <div
                    class="text-2xl font-semibold text-slate-900"
                  >
                    {{ totalHours }}h
                  </div>

                  <div
                    class="text-xs text-slate-400"
                  >
                    łącznie
                  </div>
                </div>
              </div>
            </div>

            <div class="space-y-2.5 text-sm">
              <div
                v-for="(task, index) in chartItems"
                :key="task.taskId"
                class="flex items-center justify-between gap-3 rounded-lg px-2 py-1.5 hover:bg-slate-50"
              >
                <span
                  class="flex min-w-0 items-center gap-2 truncate text-slate-600"
                >
                  <span
                    class="h-2.5 w-2.5 shrink-0 rounded-full"
                    :style="{
                      backgroundColor:
                        chartColors[
                          index %
                            chartColors.length
                        ]
                    }"
                  />

                  {{ task.taskName }}
                </span>

                <span
                  class="shrink-0 font-medium text-slate-900"
                >
                  {{
                    getTaskPercentage(
                      task.minutes
                    )
                  }}%
                </span>
              </div>
            </div>
          </template>
        </section>
      </div>
    </div>
  </div>
</template>

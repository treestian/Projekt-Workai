<script setup lang="ts">
import type { TaskItem } from '~/types/tasks'

const {
  tasks,
  loading,
  error,
  fetchTasks
} = useTasks()

const search = ref('')
const statusFilter = ref<'all' | 'active' | 'completed'>('all')
const expandedTaskId = ref<number | null>(null)

const filteredTasks = computed(() => {
  const query = search.value.trim().toLowerCase()

  return tasks.value.filter((task) => {
    const matchesStatus =
      statusFilter.value === 'all' ||
      (statusFilter.value === 'active' && !task.isCompleted) ||
      (statusFilter.value === 'completed' && task.isCompleted)

    const searchableText = [
      task.name,
      task.description,
      task.createdBy?.displayName,
      task.createdBy?.email
    ]
      .filter(Boolean)
      .join(' ')
      .toLowerCase()

    return matchesStatus &&
      (!query || searchableText.includes(query))
  })
})

const activeCount = computed(() =>
  tasks.value.filter(task => !task.isCompleted).length
)

const completedCount = computed(() =>
  tasks.value.filter(task => task.isCompleted).length
)

const formatDate = (value: string) => {
  return new Intl.DateTimeFormat('pl-PL', {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(value))
}

const getUserName = (task: TaskItem) => {
  return task.createdBy?.displayName ||
    task.createdBy?.email ||
    'Nieznany użytkownik'
}

const toggleTask = (taskId: number) => {
  expandedTaskId.value =
    expandedTaskId.value === taskId ? null : taskId
}

onMounted(fetchTasks)
</script>
<template>
  <div class="min-h-screen px-5 py-7 sm:px-8 lg:px-10">
    <div class="w-full">
      <header class="mb-8">
        <p class="mb-2 text-xs font-bold uppercase tracking-[0.18em] text-teal-600">
          Workspace / Organizacja
        </p>

        <h1 class="text-3xl font-semibold tracking-tight text-slate-950">
          Zadania
        </h1>

        <p class="mt-2 text-sm text-slate-500">
          Zadania zapisane w bazie danych.
        </p>
      </header>

      <div
        v-if="error"
        class="mb-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
      >
        {{ error }}
      </div>

      <section class="mb-6 grid gap-4 sm:grid-cols-3">
        <div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <p class="text-sm text-slate-500">Wszystkie zadania</p>
          <p class="mt-3 text-3xl font-semibold text-slate-950">
            {{ tasks.length }}
          </p>
        </div>

        <div class="rounded-2xl border border-teal-100 bg-teal-50 p-5 shadow-sm">
          <p class="text-sm text-teal-700">Aktywne</p>
          <p class="mt-3 text-3xl font-semibold text-teal-950">
            {{ activeCount }}
          </p>
        </div>

        <div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <p class="text-sm text-slate-500">Zakończone</p>
          <p class="mt-3 text-3xl font-semibold text-slate-950">
            {{ completedCount }}
          </p>
        </div>
      </section>

      <section class="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div class="flex flex-col gap-4 border-b border-slate-100 p-5 sm:flex-row">
          <input
            v-model="search"
            type="search"
            placeholder="Szukaj zadania..."
            class="h-10 flex-1 rounded-lg border border-slate-200 px-3 text-sm outline-none focus:border-teal-500"
          />

          <select
            v-model="statusFilter"
            class="h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm text-slate-600 outline-none focus:border-teal-500"
          >
            <option value="all">Wszystkie</option>
            <option value="active">Aktywne</option>
            <option value="completed">Zakończone</option>
          </select>
        </div>

        <div
          v-if="loading"
          class="p-10 text-center text-sm text-slate-400"
        >
          Ładowanie zadań...
        </div>

        <div
          v-else-if="filteredTasks.length === 0"
          class="p-10 text-center text-sm text-slate-400"
        >
          Brak zadań w bazie.
        </div>

        <div v-else class="divide-y divide-slate-100">
          <article
            v-for="task in filteredTasks"
            :key="task.id"
            class="p-5"
          >
            <button
              type="button"
              class="flex w-full items-start justify-between gap-4 text-left"
              @click="toggleTask(task.id)"
            >
              <div class="min-w-0">
                <h2 class="font-semibold text-slate-900">
                  {{ task.name }}
                </h2>

                <p class="mt-1 text-sm text-slate-500">
                  {{ task.description || 'Brak opisu' }}
                </p>

                <p class="mt-3 text-xs text-slate-400">
                  Utworzone przez: {{ getUserName(task) }}
                  · {{ formatDate(task.createdAt) }}
                </p>
              </div>

              <span
                :class="
                  task.isCompleted
                    ? 'bg-slate-100 text-slate-500'
                    : 'bg-teal-50 text-teal-700'
                "
                class="shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold"
              >
                {{ task.isCompleted ? 'Zakończone' : 'Aktywne' }}
              </span>
            </button>

            <div
              v-if="expandedTaskId === task.id"
              class="mt-5 rounded-xl bg-slate-50 p-4"
            >
              <h3 class="mb-3 text-sm font-semibold text-slate-800">
                Komentarze ({{ task.comments.length }})
              </h3>

              <p
                v-if="task.comments.length === 0"
                class="text-sm text-slate-400"
              >
                Brak komentarzy.
              </p>

              <div v-else class="space-y-3">
                <div
                  v-for="comment in task.comments"
                  :key="comment.id"
                  class="rounded-lg border border-slate-200 bg-white p-3"
                >
                  <p class="text-sm text-slate-700">
                    {{ comment.text }}
                  </p>

                  <p class="mt-2 text-xs text-slate-400">
                    {{ comment.author?.displayName || 'Nieznany użytkownik' }}
                    ·
                    {{ formatDate(comment.createdAt) }}
                  </p>
                </div>
              </div>
            </div>
          </article>
        </div>
      </section>
    </div>
  </div>
</template>
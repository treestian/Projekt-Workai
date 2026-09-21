<script setup lang="ts">
import type { TaskItem, TaskComment } from '~/types/tasks'

const { tasks, loading: tasksLoading, error: tasksError, fetchTasks } = useTasks()
const { activeWork, loading: workLoading, error: workError, fetchActiveWork } = useActiveWork()

const formatDate = (value: string) => new Intl.DateTimeFormat('pl-PL', {
  dateStyle: 'medium',
  timeStyle: 'short'
}).format(new Date(value))

const formatDuration = (value: string) => {
  const minutes = Math.max(1, Math.floor((Date.now() - new Date(value).getTime()) / 60000))
  if (minutes < 60) return `${minutes} min`
  return `${Math.floor(minutes / 60)}h ${minutes % 60 ? `${minutes % 60}m` : ''}`.trim()
}

const getUserName = (user?: { displayName?: string | null, email?: string | null } | null) => user?.displayName || user?.email || 'Nieznany użytkownik'
const activeTasks = computed(() => tasks.value.filter(task => !task.isCompleted))
const recentTasks = computed(() => tasks.value.slice(0, 5))
const recentComments = computed(() => tasks.value
  .flatMap((task: TaskItem) => task.comments.map((comment: TaskComment) => ({ ...comment, taskName: task.name })))
  .sort((first, second) => new Date(second.createdAt).getTime() - new Date(first.createdAt).getTime())
  .slice(0, 5))
const hasError = computed(() => tasksError.value || workError.value)

const refreshDashboard = async () => {
  await Promise.all([fetchTasks(), fetchActiveWork()])
}

onMounted(refreshDashboard)
</script>

<template>
  <div class="min-h-screen px-5 py-7 sm:px-8 lg:px-10">
    <div class="w-full">
      <header class="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p class="mb-2 text-xs font-bold uppercase tracking-[0.18em] text-teal-600">Workspace / Przegląd</p>
          <h1 class="text-3xl font-semibold tracking-tight text-slate-950">Dashboard</h1>
          <p class="mt-2 text-sm text-slate-500">Zobacz, nad czym zespół pracuje i co wydarzyło się ostatnio.</p>
        </div>
        <button type="button" class="inline-flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 shadow-sm transition hover:border-slate-300 hover:text-slate-950" :disabled="tasksLoading || workLoading" @click="refreshDashboard">
          <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M20 11a8.1 8.1 0 0 0-14.9-3M4 5v3h3M4 13a8.1 8.1 0 0 0 14.9 3M20 19v-3h-3" /></svg>
          Odśwież
        </button>
      </header>

      <p v-if="hasError" class="mb-6 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{{ hasError }}</p>

      <section class="mb-6 grid gap-4 sm:grid-cols-3">
        <div class="rounded-2xl border border-teal-100 bg-teal-50 p-5 shadow-sm"><p class="text-sm text-teal-700">Teraz pracuje</p><p class="mt-3 text-3xl font-semibold text-teal-950">{{ activeWork.length }}</p><p class="mt-2 text-xs text-teal-700/70">aktywnych pomiarów</p></div>
        <div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"><p class="text-sm text-slate-500">Aktywne zadania</p><p class="mt-3 text-3xl font-semibold text-slate-950">{{ activeTasks.length }}</p><p class="mt-2 text-xs text-slate-400">zadań do wykonania</p></div>
        <div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"><p class="text-sm text-slate-500">Komentarze</p><p class="mt-3 text-3xl font-semibold text-slate-950">{{ recentComments.length }}</p><p class="mt-2 text-xs text-slate-400">ostatnich aktualizacji</p></div>
      </section>

      <div class="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <section class="rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div class="flex items-start justify-between border-b border-slate-100 p-6"><div><h2 class="font-semibold text-slate-900">Zespół pracuje teraz</h2><p class="mt-1 text-sm text-slate-400">Aktywne pomiary czasu z ostatnich chwil.</p></div><span class="rounded-full bg-teal-50 px-2.5 py-1 text-xs font-semibold text-teal-700">Live</span></div>
          <div v-if="workLoading" class="p-8 text-center text-sm text-slate-400">Ładowanie aktywności...</div>
          <div v-else-if="!activeWork.length" class="p-8 text-center text-sm text-slate-400">Nikt nie ma teraz aktywnego pomiaru.</div>
          <div v-else class="divide-y divide-slate-100">
            <article v-for="work in activeWork" :key="work.id" class="flex items-center gap-4 p-5"><div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-teal-100 text-sm font-semibold text-teal-800">{{ getUserName(work.user).slice(0, 1).toUpperCase() }}</div><div class="min-w-0 flex-1"><p class="truncate font-medium text-slate-900">{{ getUserName(work.user) }}</p><p class="mt-1 truncate text-sm text-slate-500">pracuje nad <span class="font-medium text-slate-700">{{ work.task?.name || 'Nieznane zadanie' }}</span></p></div><div class="shrink-0 text-right"><p class="text-sm font-semibold text-teal-700">{{ formatDuration(work.startedAt) }}</p><p class="mt-1 text-xs text-slate-400">od {{ formatDate(work.startedAt) }}</p></div></article>
          </div>
        </section>

        <section class="rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div class="border-b border-slate-100 p-6"><h2 class="font-semibold text-slate-900">Ostatnie komentarze</h2><p class="mt-1 text-sm text-slate-400">Najnowsze informacje z zadań.</p></div>
          <div v-if="!recentComments.length" class="p-8 text-center text-sm text-slate-400">Brak komentarzy.</div>
          <div v-else class="divide-y divide-slate-100">
            <article v-for="comment in recentComments" :key="comment.id" class="p-5"><div class="flex items-start justify-between gap-3"><p class="text-sm font-semibold text-slate-800">{{ getUserName(comment.author) }}</p><time class="shrink-0 text-xs text-slate-400">{{ formatDate(comment.createdAt) }}</time></div><p class="mt-2 text-sm leading-6 text-slate-600">{{ comment.text }}</p><p class="mt-2 truncate text-xs text-teal-700">{{ comment.taskName }}</p></article>
          </div>
        </section>
      </div>

      <section class="mt-6 rounded-2xl border border-slate-200 bg-white shadow-sm"><div class="border-b border-slate-100 p-6"><h2 class="font-semibold text-slate-900">Ostatnio aktualizowane zadania</h2><p class="mt-1 text-sm text-slate-400">Szybki podgląd bieżącego frontu pracy.</p></div><div v-if="tasksLoading" class="p-8 text-center text-sm text-slate-400">Ładowanie zadań...</div><div v-else class="grid gap-0 divide-y divide-slate-100 md:grid-cols-2 md:divide-x md:divide-y-0"><article v-for="task in recentTasks" :key="task.id" class="flex items-center justify-between gap-4 p-5"><div class="min-w-0"><p class="truncate font-medium text-slate-900">{{ task.name }}</p><p class="mt-1 truncate text-sm text-slate-500">{{ task.description || 'Brak opisu' }}</p><p class="mt-2 text-xs text-slate-400">{{ formatDate(task.updatedAt) }}</p></div><span class="shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold" :class="task.isCompleted ? 'bg-slate-100 text-slate-500' : 'bg-teal-50 text-teal-700'">{{ task.isCompleted ? 'Zakończone' : 'Aktywne' }}</span></article></div></section>
    </div>
  </div>
</template>

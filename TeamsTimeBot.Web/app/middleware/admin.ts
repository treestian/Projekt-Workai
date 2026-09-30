export default defineNuxtRouteMiddleware(async () => {
  if (import.meta.server) {
    return
  }

  const { fetchCurrentUser, isAdmin } = useCurrentUser()

  await fetchCurrentUser()

  if (!isAdmin.value) {
    return navigateTo('/')
  }
})

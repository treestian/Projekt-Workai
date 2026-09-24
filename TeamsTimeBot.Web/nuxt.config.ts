import tailwindcss from '@tailwindcss/vite'

export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',

  devtools: {
    enabled: true
  },

  css: [
    '/Users/sebastianborek/Desktop/Projekt Workai/TeamsTimeBot.Web/assets/css/main.css'
  ],

  vite: {
    plugins: [
      tailwindcss()
    ]
  },

  runtimeConfig: {
    public: {
      azureClientId: '',
      azureTenantId: '',
      apiUrl: 'http://localhost:5294'
    }
  }
})

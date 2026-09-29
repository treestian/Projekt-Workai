import tailwindcss from '@tailwindcss/vite'

export default defineNuxtConfig({

  compatibilityDate: '2025-07-15',

  devtools: {
    enabled: true
  },

  css: [
    '~/assets/css/main.css'
  ],

  vite: {
    plugins: [
      tailwindcss()
    ]
  },

  runtimeConfig: {
    public: {
      azureClientId: process.env.NUXT_PUBLIC_AZURE_CLIENT_ID,
      azureTenantId: process.env.NUXT_PUBLIC_AZURE_TENANT_ID,
      apiUrl: process.env.NUXT_PUBLIC_API_URL
    }
  }

})
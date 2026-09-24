import {
InteractionRequiredAuthError,
PublicClientApplication,
type AccountInfo
} from '@azure/msal-browser'

let msalInstance: PublicClientApplication | null = null

const API_SCOPE =
'api://592f5759-2d96-4aa8-916e-04d2b3a0d655/access_as_user'

const REDIRECT_URI = 'http://localhost:3000'

export const initializeMsal = async () => {
if (msalInstance) {
return msalInstance
}

const config = useRuntimeConfig()

const clientId = config.public.azureClientId
const tenantId = config.public.azureTenantId

if (!clientId) {
throw new Error('Brak NUXT_PUBLIC_AZURE_CLIENT_ID')
}

if (!tenantId) {
throw new Error('Brak NUXT_PUBLIC_AZURE_TENANT_ID')
}

msalInstance = new PublicClientApplication({
auth: {
clientId,
authority: `https://login.microsoftonline.com/${tenantId}`,
redirectUri: REDIRECT_URI
},


cache: {
  cacheLocation: 'sessionStorage'
}


})

await msalInstance.initialize()

const accounts = msalInstance.getAllAccounts()
const activeAccount = msalInstance.getActiveAccount()

if (!activeAccount && accounts.length > 0) {
const account = accounts[0]

```
if (account) {
  msalInstance.setActiveAccount(account)
}
```

}

return msalInstance
}

export const handleRedirect = async () => {
const instance = await initializeMsal()

const result = await instance.handleRedirectPromise()

if (result?.account) {
instance.setActiveAccount(result.account)
}

return result
}

export const getAccount = (): AccountInfo | null => {
return msalInstance?.getActiveAccount() ?? null
}

export const login = async (): Promise<void> => {
const instance = await initializeMsal()

await instance.loginRedirect({
scopes: [
'openid',
'profile',
'email',
API_SCOPE
],
redirectUri: REDIRECT_URI
})
}

export const logout = async (): Promise<void> => {
const instance = await initializeMsal()

await instance.logoutRedirect({
postLogoutRedirectUri: REDIRECT_URI
})
}

export const getAccessToken = async (): Promise<string> => {
const instance = await initializeMsal()

const account = instance.getActiveAccount()

if (!account) {
throw new Error('Użytkownik nie jest zalogowany.')
}

try {
const result = await instance.acquireTokenSilent({
account,
scopes: [API_SCOPE],
redirectUri: REDIRECT_URI
})


return result.accessToken


} catch (error) {
if (error instanceof InteractionRequiredAuthError) {
await instance.acquireTokenRedirect({
account,
scopes: [API_SCOPE],
redirectUri: REDIRECT_URI
})


  throw new Error('Wymagane jest ponowne logowanie.')
}

throw error


}
}

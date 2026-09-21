export interface User {
  id?: number | null
  azureId?: string | null
  displayName?: string | null
  email?: string | null
  mail?: string | null
  userPrincipalName?: string | null
  isActive?: boolean
  createdAt?: string
  updatedAt?: string
}

export interface UserSyncSettings {
  intervalHours: number
  updatedAt: string
}

export interface UserSyncResult {
  totalFromGraph: number
  added: number
  updated: number
  deactivated: number
}
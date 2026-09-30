export type UserRole = 'Employee' | 'Admin'

export interface User {
  id: number
  azureId: string
  displayName: string | null
  email: string | null
  userPrincipalName: string | null
  isActive: boolean
  role: UserRole
  createdAt: string
  updatedAt: string
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

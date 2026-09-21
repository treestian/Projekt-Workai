export interface TaskUser {
  id: number
  displayName?: string | null
  email?: string | null
}

export interface TaskComment {
  id: number
  text: string
  createdAt: string
  author?: TaskUser | null
}

export interface TaskItem {
  id: number
  name: string
  description?: string | null
  isCompleted: boolean
  createdAt: string
  updatedAt: string
  createdBy?: TaskUser | null
  comments: TaskComment[]
}
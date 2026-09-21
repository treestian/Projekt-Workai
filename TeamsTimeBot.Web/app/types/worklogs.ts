export interface WorkLogTaskSummary {
  taskId: number
  taskName: string
  minutes: number
  hours: number
}

export interface WorkLogSummary {
  startDate: string
  endDate: string
  totalMinutes: number
  totalHours: number
  tasks: WorkLogTaskSummary[]
}
export function formatDueDate(dateStr, isComplete) {
  if (!dateStr) return null
  // Parse as local date (YYYY-MM-DD or ISO)
  const due = new Date(dateStr)
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const dueDay = new Date(due)
  dueDay.setHours(0, 0, 0, 0)

  const label = due.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })

  if (isComplete) return { label, className: 'text-gray-400' }

  if (dueDay < today) return { label: `Overdue · ${label}`, className: 'text-red-600' }
  if (dueDay.getTime() === today.getTime()) return { label: `Due today · ${label}`, className: 'text-amber-600' }
  return { label, className: 'text-gray-500' }
}

export function timeAgo(dateStr) {
  const date = new Date(dateStr)
  const now = new Date()
  const seconds = Math.floor((now - date) / 1000)

  if (seconds < 60) return 'just now'

  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes}m ago`

  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`

  const days = Math.floor(hours / 24)
  if (days === 1) return 'yesterday'
  if (days < 7) return `${days}d ago`

  const weeks = Math.floor(days / 7)
  if (weeks < 5) return `${weeks}w ago`

  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

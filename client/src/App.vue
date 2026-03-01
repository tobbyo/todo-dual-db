<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import draggable from 'vuedraggable'
import ActivityLog from './components/ActivityLog.vue'
import { timeAgo, formatDueDate } from './utils/timeAgo.js'

const todos = ref([])
const newTitle = ref('')
const newDescription = ref('')
const newDueDate = ref('')
const newPriority = ref(null)
const showNewDetails = ref(false)
const editingId = ref(null)
const editTitle = ref('')
const editDescription = ref('')
const editDueDate = ref('')
const editPriority = ref(null)
const loading = ref(false)
const searchQuery = ref('')
const statusFilter = ref('all')
const priorityFilter = ref(null)
const showCompleted = ref(false)
const deletingTodo = ref(null)
const sortBy = ref('sortOrder')
const sortDir = ref('asc')

// Dark mode — init from localStorage, fallback to system preference
const isDark = ref(
  localStorage.getItem('theme') === 'dark' ||
  (!localStorage.getItem('theme') && window.matchMedia('(prefers-color-scheme: dark)').matches)
)

function toggleDark() {
  isDark.value = !isDark.value
  localStorage.setItem('theme', isDark.value ? 'dark' : 'light')
  document.documentElement.classList.toggle('dark', isDark.value)
}

const PRIORITIES = ['Low', 'Medium', 'High']

const sortOptions = [
  { value: 'sortOrder', label: 'Manual' },
  { value: 'createdAt', label: 'Date created' },
  { value: 'title', label: 'Title' },
  { value: 'isComplete', label: 'Status' },
  { value: 'dueDate', label: 'Due date' },
]

function isOverdue(todo) {
  if (!todo.dueDate || todo.isComplete) return false
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  return new Date(todo.dueDate) < today
}

const statusCounts = computed(() => ({
  all: todos.value.length,
  active: todos.value.filter(t => !t.isComplete).length,
  completed: todos.value.filter(t => t.isComplete).length,
  overdue: todos.value.filter(isOverdue).length,
}))

const filtersActive = computed(() =>
  statusFilter.value !== 'all' || priorityFilter.value !== null || searchQuery.value.trim() !== ''
)

function clearFilters() {
  statusFilter.value = 'all'
  priorityFilter.value = null
  searchQuery.value = ''
}

const sortedTodos = computed(() => {
  let result = todos.value

  if (statusFilter.value === 'active') result = result.filter(t => !t.isComplete)
  else if (statusFilter.value === 'completed') result = result.filter(t => t.isComplete)
  else if (statusFilter.value === 'overdue') result = result.filter(isOverdue)

  if (priorityFilter.value) result = result.filter(t => t.priority === priorityFilter.value)

  const q = searchQuery.value.trim().toLowerCase()
  if (q) result = result.filter(t =>
    t.title.toLowerCase().includes(q) ||
    (t.description && t.description.toLowerCase().includes(q))
  )

  return [...result].sort((a, b) => {
    let aVal = a[sortBy.value]
    let bVal = b[sortBy.value]

    if (aVal == null && bVal == null) return 0
    if (aVal == null) return 1
    if (bVal == null) return -1

    if (typeof aVal === 'string') {
      aVal = aVal.toLowerCase()
      bVal = (bVal || '').toLowerCase()
    }
    if (typeof aVal === 'boolean') {
      aVal = aVal ? 1 : 0
      bVal = bVal ? 1 : 0
    }
    if (aVal < bVal) return sortDir.value === 'asc' ? -1 : 1
    if (aVal > bVal) return sortDir.value === 'asc' ? 1 : -1
    return 0
  })
})

const activeTodos = computed(() => sortedTodos.value.filter(t => !t.isComplete))
const completedTodos = computed(() => sortedTodos.value.filter(t => t.isComplete))

const draggableActiveTodos = computed({
  get: () => statusFilter.value === 'all' ? activeTodos.value : sortedTodos.value,
  set: (reordered) => {
    reordered.forEach((item, i) => {
      const t = todos.value.find(x => x.id === item.id)
      if (t) t.sortOrder = i
    })
    fetch(`${API}/reorder`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(reordered.map((t, i) => ({ id: t.id, sortOrder: i })))
    })
  }
})

function toggleSortDir() {
  sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
}

const activityLogRef = ref(null)

function refreshActivityLog() {
  activityLogRef.value?.fetchLogs()
}

const API = '/api/todos'

async function fetchTodos() {
  loading.value = true
  const res = await fetch(API)
  todos.value = await res.json()
  loading.value = false
}

async function addTodo() {
  if (!newTitle.value.trim()) return
  await fetch(API, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      title: newTitle.value,
      description: newDescription.value || null,
      dueDate: newDueDate.value || null,
      priority: newPriority.value || null
    })
  })
  newTitle.value = ''
  newDescription.value = ''
  newDueDate.value = ''
  newPriority.value = null
  showNewDetails.value = false
  await fetchTodos()
  refreshActivityLog()
}

async function toggleComplete(todo) {
  await fetch(`${API}/${todo.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ isComplete: !todo.isComplete })
  })
  await fetchTodos()
  refreshActivityLog()
}

function requestDelete(todo) {
  deletingTodo.value = { id: todo.id, title: todo.title }
}

function cancelDelete() {
  deletingTodo.value = null
}

async function confirmDelete() {
  await fetch(`${API}/${deletingTodo.value.id}`, { method: 'DELETE' })
  deletingTodo.value = null
  await fetchTodos()
  refreshActivityLog()
}

async function deleteTodo(id) {
  await fetch(`${API}/${id}`, { method: 'DELETE' })
  await fetchTodos()
  refreshActivityLog()
}

function startEdit(todo) {
  editingId.value = todo.id
  editTitle.value = todo.title
  editDescription.value = todo.description || ''
  editDueDate.value = todo.dueDate ? todo.dueDate.substring(0, 10) : ''
  editPriority.value = todo.priority || null
}

function cancelEdit() {
  editingId.value = null
}

async function saveEdit(id) {
  await fetch(`${API}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      title: editTitle.value,
      description: editDescription.value || null,
      dueDate: editDueDate.value || null,
      priority: editPriority.value || null
    })
  })
  editingId.value = null
  await fetchTodos()
  refreshActivityLog()
}

const priorityClasses = {
  High: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-400',
  Medium: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-400',
  Low: 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-400',
}

const priorityBorderClasses = {
  High: 'border-l-[3px] border-l-red-400',
  Medium: 'border-l-[3px] border-l-amber-400',
  Low: 'border-l-[3px] border-l-blue-400',
}

function onKeydown(e) {
  if (e.key === 'Escape' && deletingTodo.value) cancelDelete()
}

onMounted(() => {
  document.documentElement.classList.toggle('dark', isDark.value)
  fetchTodos()
  window.addEventListener('keydown', onKeydown)
})
onUnmounted(() => window.removeEventListener('keydown', onKeydown))
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 transition-colors">
    <div class="max-w-5xl mx-auto px-4 py-12">

      <!-- Header -->
      <div class="flex items-center justify-between mb-8">
        <h1 class="text-3xl font-bold text-gray-900 dark:text-white">Todo List</h1>
        <button
          @click="toggleDark"
          class="p-2 rounded-md text-gray-500 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
          :title="isDark ? 'Switch to light mode' : 'Switch to dark mode'"
        >
          <!-- Sun (shown in dark mode) -->
          <svg v-if="isDark" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364-6.364l-.707.707M6.343 17.657l-.707.707M17.657 17.657l-.707-.707M6.343 6.343l-.707-.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"/>
          </svg>
          <!-- Moon (shown in light mode) -->
          <svg v-else class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"/>
          </svg>
        </button>
      </div>

      <div class="lg:grid lg:grid-cols-[1fr_320px] lg:gap-8 lg:items-start">

        <!-- Left column -->
        <div>

          <!-- Add Todo Form -->
          <form @submit.prevent="addTodo" class="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 p-4 mb-6">
            <div class="flex gap-2">
              <input
                v-model="newTitle"
                type="text"
                placeholder="What needs to be done?"
                class="flex-1 px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
              <button
                type="submit"
                class="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-md hover:bg-blue-700 transition-colors whitespace-nowrap"
              >Add</button>
            </div>

            <div v-if="showNewDetails" class="mt-2 space-y-2">
              <input
                v-model="newDescription"
                type="text"
                placeholder="Description (optional)"
                class="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
              <div class="flex gap-2 flex-wrap items-center">
                <input
                  v-model="newDueDate"
                  type="date"
                  class="px-3 py-1.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
                <div class="flex gap-1">
                  <button type="button" @click="newPriority = null"
                    class="px-2.5 py-1.5 text-xs font-medium rounded-md transition-colors"
                    :class="newPriority === null ? 'bg-gray-700 dark:bg-gray-500 text-white' : 'bg-white dark:bg-gray-700 text-gray-600 dark:text-gray-300 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-600'"
                  >None</button>
                  <button v-for="p in PRIORITIES" :key="p" type="button" @click="newPriority = p"
                    class="px-2.5 py-1.5 text-xs font-medium rounded-md transition-colors"
                    :class="newPriority === p ? priorityClasses[p] + ' ring-1 ring-inset ring-current' : 'bg-white dark:bg-gray-700 text-gray-600 dark:text-gray-300 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-600'"
                  >{{ p }}</button>
                </div>
              </div>
            </div>

            <button type="button" @click="showNewDetails = !showNewDetails"
              class="mt-2 text-xs text-gray-400 dark:text-gray-500 hover:text-gray-500 dark:hover:text-gray-400 transition-colors"
            >{{ showNewDetails ? '↑ Fewer options' : '+ More options' }}</button>
          </form>

          <!-- Controls: Search + Filter + Sort -->
          <div v-if="todos.length > 0" class="mb-5 space-y-2">
            <div class="relative">
              <input
                v-model="searchQuery"
                type="text"
                placeholder="Search todos..."
                class="w-full pl-9 pr-8 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
              <svg class="absolute left-2.5 top-2.5 h-4 w-4 text-gray-400 dark:text-gray-500 pointer-events-none" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-4.35-4.35M17 11A6 6 0 1 1 5 11a6 6 0 0 1 12 0z"/>
              </svg>
              <button v-if="searchQuery" @click="searchQuery = ''"
                class="absolute right-2.5 top-2 text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-300 text-sm leading-none p-0.5"
              >✕</button>
            </div>

            <div class="flex items-center justify-between gap-2">
              <div class="flex gap-1 flex-wrap">
                <button
                  v-for="opt in [
                    { value: 'all', label: 'All' },
                    { value: 'active', label: 'Active' },
                    { value: 'completed', label: 'Done' },
                    { value: 'overdue', label: 'Overdue', dot: true },
                  ]"
                  :key="opt.value"
                  @click="statusFilter = opt.value"
                  class="relative px-2.5 py-1 text-xs font-medium rounded-md transition-colors"
                  :class="statusFilter === opt.value
                    ? 'bg-blue-600 text-white'
                    : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700'"
                >
                  {{ opt.label }} <span class="opacity-60">{{ statusCounts[opt.value] }}</span>
                  <span v-if="opt.dot && statusCounts.overdue > 0 && statusFilter !== 'overdue'"
                    class="absolute -top-1 -right-1 w-2 h-2 bg-red-500 rounded-full"/>
                </button>
              </div>
              <div class="flex items-center gap-1 shrink-0">
                <select v-model="sortBy"
                  class="px-2 py-1 text-xs text-gray-600 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                >
                  <option v-for="opt in sortOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                </select>
                <button @click="toggleSortDir"
                  class="px-2 py-1 text-xs text-gray-600 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
                >{{ sortDir === 'asc' ? '↑' : '↓' }}</button>
              </div>
            </div>

            <div class="flex items-center justify-between gap-2">
              <div class="flex gap-1">
                <button @click="priorityFilter = null"
                  class="px-2.5 py-1 text-xs font-medium rounded-md transition-colors"
                  :class="priorityFilter === null
                    ? 'bg-gray-700 dark:bg-gray-500 text-white'
                    : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700'"
                >Any priority</button>
                <button v-for="p in PRIORITIES" :key="p" @click="priorityFilter = p"
                  class="px-2.5 py-1 text-xs font-medium rounded-md transition-colors"
                  :class="priorityFilter === p
                    ? priorityClasses[p] + ' ring-1 ring-inset ring-current'
                    : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700'"
                >{{ p }}</button>
              </div>
              <button v-if="filtersActive" @click="clearFilters"
                class="text-xs text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-300 transition-colors shrink-0"
              >× Clear filters</button>
            </div>
          </div>

          <!-- States -->
          <p v-if="loading" class="text-gray-500 dark:text-gray-400 text-center py-8">Loading...</p>
          <p v-else-if="todos.length === 0" class="text-gray-400 dark:text-gray-500 text-center py-8">No todos yet. Add one above!</p>
          <p v-else-if="sortedTodos.length === 0" class="text-gray-400 dark:text-gray-500 text-center py-8">No todos match your search.</p>

          <!-- Todo list -->
          <template v-else>

            <draggable
              v-model="draggableActiveTodos"
              item-key="id"
              handle=".drag-handle"
              tag="ul"
              class="space-y-2"
              :animation="150"
              :disabled="sortBy !== 'sortOrder' || statusFilter !== 'all'"
            >
              <template #item="{ element: todo }">
              <li
                :key="todo.id"
                class="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 p-4"
                :class="todo.priority ? priorityBorderClasses[todo.priority] : ''"
              >
                <div v-if="editingId !== todo.id" class="flex items-start gap-3">
                  <svg v-if="sortBy === 'sortOrder' && statusFilter === 'all'"
                    class="drag-handle mt-0.5 h-4 w-4 shrink-0 cursor-grab text-gray-300 dark:text-gray-600 hover:text-gray-500 dark:hover:text-gray-400 transition-colors active:cursor-grabbing"
                    viewBox="0 0 20 20" fill="currentColor"
                  >
                    <circle cx="7" cy="4" r="1.5"/><circle cx="13" cy="4" r="1.5"/>
                    <circle cx="7" cy="10" r="1.5"/><circle cx="13" cy="10" r="1.5"/>
                    <circle cx="7" cy="16" r="1.5"/><circle cx="13" cy="16" r="1.5"/>
                  </svg>
                  <input type="checkbox" :checked="todo.isComplete" @change="toggleComplete(todo)"
                    class="mt-0.5 h-4 w-4 rounded border-gray-300 dark:border-gray-600 text-blue-600 focus:ring-blue-500 cursor-pointer shrink-0"
                  />
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2 flex-wrap">
                      <p class="text-sm font-medium" :class="todo.isComplete ? 'line-through text-gray-400 dark:text-gray-500' : 'text-gray-900 dark:text-gray-100'">{{ todo.title }}</p>
                      <span v-if="todo.priority" class="px-1.5 py-0.5 text-xs font-medium rounded" :class="priorityClasses[todo.priority]">{{ todo.priority }}</span>
                    </div>
                    <p v-if="todo.description" class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ todo.description }}</p>
                    <div class="flex items-center gap-3 mt-1 flex-wrap text-xs">
                      <span class="text-gray-400 dark:text-gray-500">{{ timeAgo(todo.createdAt) }}</span>
                      <span v-if="todo.dueDate" :class="formatDueDate(todo.dueDate, todo.isComplete).className">{{ formatDueDate(todo.dueDate, todo.isComplete).label }}</span>
                    </div>
                  </div>
                  <div class="flex gap-1 shrink-0">
                    <button @click="startEdit(todo)" class="px-2 py-1 text-xs text-gray-400 dark:text-gray-500 hover:text-blue-600 dark:hover:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/30 rounded transition-colors">Edit</button>
                    <button @click="requestDelete(todo)" class="px-2 py-1 text-xs text-gray-400 dark:text-gray-500 hover:text-red-600 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 rounded transition-colors">Delete</button>
                  </div>
                </div>

                <div v-else class="space-y-2">
                  <input v-model="editTitle" type="text"
                    class="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                  <input v-model="editDescription" type="text" placeholder="Description"
                    class="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                  <div class="flex gap-2 flex-wrap items-center">
                    <input v-model="editDueDate" type="date"
                      class="px-3 py-1.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    />
                    <div class="flex gap-1">
                      <button type="button" @click="editPriority = null"
                        class="px-2.5 py-1.5 text-xs font-medium rounded-md transition-colors"
                        :class="editPriority === null ? 'bg-gray-700 dark:bg-gray-500 text-white' : 'bg-white dark:bg-gray-700 text-gray-600 dark:text-gray-300 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-600'"
                      >None</button>
                      <button v-for="p in PRIORITIES" :key="p" type="button" @click="editPriority = p"
                        class="px-2.5 py-1.5 text-xs font-medium rounded-md transition-colors"
                        :class="editPriority === p ? priorityClasses[p] + ' ring-1 ring-inset ring-current' : 'bg-white dark:bg-gray-700 text-gray-600 dark:text-gray-300 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-600'"
                      >{{ p }}</button>
                    </div>
                  </div>
                  <div class="flex gap-2">
                    <button @click="saveEdit(todo.id)" class="px-3 py-1.5 bg-blue-600 text-white text-xs font-medium rounded-md hover:bg-blue-700 transition-colors">Save</button>
                    <button @click="cancelEdit" class="px-3 py-1.5 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 text-xs font-medium rounded-md hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors">Cancel</button>
                  </div>
                </div>
              </li>
              </template>
            </draggable>

            <!-- Completed todos — collapsible -->
            <template v-if="statusFilter === 'all' && completedTodos.length > 0">
              <button @click="showCompleted = !showCompleted"
                class="mt-4 w-full flex items-center gap-3 py-1.5 text-xs text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
              >
                <span class="shrink-0">{{ showCompleted ? '↑ Hide' : '↓ Show' }} {{ completedTodos.length }} completed</span>
                <span class="flex-1 border-t border-gray-200 dark:border-gray-700"></span>
              </button>

              <ul v-if="showCompleted" class="space-y-2 mt-2">
                <li v-for="todo in completedTodos" :key="todo.id"
                  class="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 p-4 opacity-60"
                  :class="todo.priority ? priorityBorderClasses[todo.priority] : ''"
                >
                  <div v-if="editingId !== todo.id" class="flex items-start gap-3">
                    <input type="checkbox" :checked="todo.isComplete" @change="toggleComplete(todo)"
                      class="mt-0.5 h-4 w-4 rounded border-gray-300 dark:border-gray-600 text-blue-600 focus:ring-blue-500 cursor-pointer shrink-0"
                    />
                    <div class="flex-1 min-w-0">
                      <div class="flex items-center gap-2 flex-wrap">
                        <p class="text-sm font-medium line-through text-gray-400 dark:text-gray-500">{{ todo.title }}</p>
                        <span v-if="todo.priority" class="px-1.5 py-0.5 text-xs font-medium rounded" :class="priorityClasses[todo.priority]">{{ todo.priority }}</span>
                      </div>
                      <p v-if="todo.description" class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ todo.description }}</p>
                      <div class="flex items-center gap-3 mt-1 flex-wrap text-xs">
                        <span class="text-gray-400 dark:text-gray-500">{{ timeAgo(todo.createdAt) }}</span>
                        <span v-if="todo.dueDate" :class="formatDueDate(todo.dueDate, todo.isComplete).className">{{ formatDueDate(todo.dueDate, todo.isComplete).label }}</span>
                      </div>
                    </div>
                    <div class="flex gap-1 shrink-0">
                      <button @click="startEdit(todo)" class="px-2 py-1 text-xs text-gray-400 dark:text-gray-500 hover:text-blue-600 dark:hover:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/30 rounded transition-colors">Edit</button>
                      <button @click="requestDelete(todo)" class="px-2 py-1 text-xs text-gray-400 dark:text-gray-500 hover:text-red-600 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 rounded transition-colors">Delete</button>
                    </div>
                  </div>

                  <div v-else class="space-y-2">
                    <input v-model="editTitle" type="text"
                      class="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    />
                    <input v-model="editDescription" type="text" placeholder="Description"
                      class="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    />
                    <div class="flex gap-2 flex-wrap items-center">
                      <input v-model="editDueDate" type="date"
                        class="px-3 py-1.5 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                      />
                      <div class="flex gap-1">
                        <button type="button" @click="editPriority = null"
                          class="px-2.5 py-1.5 text-xs font-medium rounded-md transition-colors"
                          :class="editPriority === null ? 'bg-gray-700 dark:bg-gray-500 text-white' : 'bg-white dark:bg-gray-700 text-gray-600 dark:text-gray-300 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-600'"
                        >None</button>
                        <button v-for="p in PRIORITIES" :key="p" type="button" @click="editPriority = p"
                          class="px-2.5 py-1.5 text-xs font-medium rounded-md transition-colors"
                          :class="editPriority === p ? priorityClasses[p] + ' ring-1 ring-inset ring-current' : 'bg-white dark:bg-gray-700 text-gray-600 dark:text-gray-300 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-600'"
                        >{{ p }}</button>
                      </div>
                    </div>
                    <div class="flex gap-2">
                      <button @click="saveEdit(todo.id)" class="px-3 py-1.5 bg-blue-600 text-white text-xs font-medium rounded-md hover:bg-blue-700 transition-colors">Save</button>
                      <button @click="cancelEdit" class="px-3 py-1.5 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 text-xs font-medium rounded-md hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors">Cancel</button>
                    </div>
                  </div>
                </li>
              </ul>
            </template>

          </template>
        </div>

        <!-- Right column: Activity Log -->
        <aside class="mt-8 lg:mt-0 lg:sticky lg:top-6">
          <ActivityLog ref="activityLogRef" />
        </aside>

      </div>
    </div>

    <!-- Delete confirmation modal -->
    <Teleport to="body">
      <div v-if="deletingTodo" class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="absolute inset-0 bg-black/40 dark:bg-black/60" @click="cancelDelete" />
        <div class="relative bg-white dark:bg-gray-800 rounded-lg shadow-xl border border-gray-200 dark:border-gray-700 p-6 w-full max-w-sm mx-4">
          <h2 class="text-sm font-semibold text-gray-900 dark:text-gray-100 mb-1">Delete todo?</h2>
          <p class="text-sm text-gray-500 dark:text-gray-400 mb-5">
            "<span class="text-gray-700 dark:text-gray-200">{{ deletingTodo.title }}</span>" will be permanently removed.
          </p>
          <div class="flex justify-end gap-2">
            <button @click="cancelDelete"
              class="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 rounded-md hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
            >Cancel</button>
            <button @click="confirmDelete"
              class="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-md hover:bg-red-700 transition-colors"
            >Delete</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

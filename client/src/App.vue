<script setup>
import { ref, computed, onMounted } from 'vue'
import ActivityLog from './components/ActivityLog.vue'
import { timeAgo } from './utils/timeAgo.js'

const todos = ref([])
const newTitle = ref('')
const newDescription = ref('')
const editingId = ref(null)
const editTitle = ref('')
const editDescription = ref('')
const loading = ref(false)
const sortBy = ref('createdAt')
const sortDir = ref('desc')

const sortOptions = [
  { value: 'createdAt', label: 'Date' },
  { value: 'title', label: 'Title' },
  { value: 'isComplete', label: 'Status' },
]

const sortedTodos = computed(() => {
  return [...todos.value].sort((a, b) => {
    let aVal = a[sortBy.value]
    let bVal = b[sortBy.value]
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
    body: JSON.stringify({ title: newTitle.value, description: newDescription.value || null })
  })
  newTitle.value = ''
  newDescription.value = ''
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

async function deleteTodo(id) {
  await fetch(`${API}/${id}`, { method: 'DELETE' })
  await fetchTodos()
  refreshActivityLog()
}

function startEdit(todo) {
  editingId.value = todo.id
  editTitle.value = todo.title
  editDescription.value = todo.description || ''
}

function cancelEdit() {
  editingId.value = null
}

async function saveEdit(id) {
  await fetch(`${API}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title: editTitle.value, description: editDescription.value || null })
  })
  editingId.value = null
  await fetchTodos()
  refreshActivityLog()
}

function formatDate(dateStr) {
  return timeAgo(dateStr)
}

onMounted(fetchTodos)
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <div class="max-w-2xl mx-auto px-4 py-12">
      <h1 class="text-3xl font-bold text-gray-900 mb-8">Todo List</h1>

      <!-- Add Todo Form -->
      <form @submit.prevent="addTodo" class="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-8">
        <input
          v-model="newTitle"
          type="text"
          placeholder="What needs to be done?"
          class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        />
        <input
          v-model="newDescription"
          type="text"
          placeholder="Description (optional)"
          class="w-full mt-2 px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        />
        <button
          type="submit"
          class="mt-3 px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-md hover:bg-blue-700 transition-colors"
        >
          Add Todo
        </button>
      </form>

      <!-- Sort Controls -->
      <div v-if="todos.length > 0" class="flex items-center gap-3 mb-4">
        <span class="text-sm text-gray-500">Sort by:</span>
        <div class="flex gap-1">
          <button
            v-for="opt in sortOptions"
            :key="opt.value"
            @click="sortBy = opt.value"
            class="px-3 py-1 text-xs font-medium rounded-md transition-colors"
            :class="sortBy === opt.value
              ? 'bg-blue-600 text-white'
              : 'bg-white text-gray-600 border border-gray-300 hover:bg-gray-50'"
          >
            {{ opt.label }}
          </button>
        </div>
        <button
          @click="toggleSortDir"
          class="px-2 py-1 text-xs text-gray-600 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
          :title="sortDir === 'asc' ? 'Ascending' : 'Descending'"
        >
          {{ sortDir === 'asc' ? '↑ Asc' : '↓ Desc' }}
        </button>
      </div>

      <!-- Loading -->
      <p v-if="loading" class="text-gray-500 text-center py-8">Loading...</p>

      <!-- Empty State -->
      <p v-else-if="todos.length === 0" class="text-gray-400 text-center py-8">No todos yet. Add one above!</p>

      <!-- Todo List -->
      <ul v-else class="space-y-3">
        <li
          v-for="todo in sortedTodos"
          :key="todo.id"
          class="bg-white rounded-lg shadow-sm border border-gray-200 p-4"
        >
          <!-- View Mode -->
          <div v-if="editingId !== todo.id" class="flex items-start gap-3">
            <input
              type="checkbox"
              :checked="todo.isComplete"
              @change="toggleComplete(todo)"
              class="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500 cursor-pointer"
            />
            <div class="flex-1 min-w-0">
              <p
                class="text-sm font-medium"
                :class="todo.isComplete ? 'line-through text-gray-400' : 'text-gray-900'"
              >
                {{ todo.title }}
              </p>
              <p v-if="todo.description" class="text-sm text-gray-500 mt-0.5">
                {{ todo.description }}
              </p>
              <p class="text-xs text-gray-400 mt-1">{{ formatDate(todo.createdAt) }}</p>
            </div>
            <div class="flex gap-1">
              <button
                @click="startEdit(todo)"
                class="px-2 py-1 text-xs text-gray-600 hover:text-blue-600 hover:bg-blue-50 rounded transition-colors"
              >
                Edit
              </button>
              <button
                @click="deleteTodo(todo.id)"
                class="px-2 py-1 text-xs text-gray-600 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
              >
                Delete
              </button>
            </div>
          </div>

          <!-- Edit Mode -->
          <div v-else class="space-y-2">
            <input
              v-model="editTitle"
              type="text"
              class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
            <input
              v-model="editDescription"
              type="text"
              placeholder="Description"
              class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
            <div class="flex gap-2">
              <button
                @click="saveEdit(todo.id)"
                class="px-3 py-1.5 bg-blue-600 text-white text-xs font-medium rounded-md hover:bg-blue-700 transition-colors"
              >
                Save
              </button>
              <button
                @click="cancelEdit"
                class="px-3 py-1.5 bg-gray-100 text-gray-700 text-xs font-medium rounded-md hover:bg-gray-200 transition-colors"
              >
                Cancel
              </button>
            </div>
          </div>
        </li>
      </ul>

      <!-- Activity Log -->
      <ActivityLog ref="activityLogRef" />
    </div>
  </div>
</template>

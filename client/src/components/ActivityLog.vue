<script setup>
import { ref } from 'vue'
import { timeAgo } from '../utils/timeAgo.js'

const logs = ref([])
const loading = ref(false)
const collapsed = ref(true)
const logCount = ref(0)

const actionColors = {
  Created: 'bg-green-100 text-green-800',
  Updated: 'bg-blue-100 text-blue-800',
  Completed: 'bg-purple-100 text-purple-800',
  Uncompleted: 'bg-yellow-100 text-yellow-800',
  Deleted: 'bg-red-100 text-red-800',
}

async function fetchLogs() {
  loading.value = true
  try {
    const [logsRes, countRes] = await Promise.all([
      fetch('/api/activity-logs?limit=50'),
      fetch('/api/activity-logs/count'),
    ])
    logs.value = await logsRes.json()
    const countData = await countRes.json()
    logCount.value = countData.count
  } finally {
    loading.value = false
  }
}

function formatTime(dateStr) {
  return timeAgo(dateStr)
}

function toggle() {
  collapsed.value = !collapsed.value
  if (!collapsed.value && logs.value.length === 0) {
    fetchLogs()
  }
}

defineExpose({ fetchLogs })
</script>

<template>
  <div class="bg-white rounded-lg shadow-sm border border-gray-200 mt-8">
    <!-- Header / Toggle -->
    <button
      @click="toggle"
      class="w-full flex items-center justify-between px-4 py-3 text-left hover:bg-gray-50 transition-colors rounded-lg"
    >
      <div class="flex items-center gap-2">
        <span class="text-sm font-medium text-gray-700">Activity Log</span>
        <span
          v-if="logCount > 0"
          class="px-2 py-0.5 text-xs font-medium bg-gray-100 text-gray-600 rounded-full"
        >
          {{ logCount }}
        </span>
      </div>
      <span class="text-gray-400 text-sm">{{ collapsed ? '▸' : '▾' }}</span>
    </button>

    <!-- Log Entries -->
    <div v-if="!collapsed" class="border-t border-gray-100 px-4 py-3">
      <p v-if="loading" class="text-gray-400 text-sm text-center py-4">Loading activity...</p>

      <p v-else-if="logs.length === 0" class="text-gray-400 text-sm text-center py-4">
        No activity yet.
      </p>

      <ul v-else class="space-y-3">
        <li v-for="log in logs" :key="log.id" class="flex items-start gap-3">
          <!-- Timeline dot -->
          <div class="mt-1.5 w-2 h-2 rounded-full bg-gray-300 flex-shrink-0"></div>

          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 flex-wrap">
              <span
                class="px-2 py-0.5 text-xs font-medium rounded-full"
                :class="actionColors[log.action] || 'bg-gray-100 text-gray-800'"
              >
                {{ log.action }}
              </span>
              <span class="text-sm text-gray-900 font-medium truncate">{{ log.todoTitle }}</span>
              <span class="text-xs text-gray-400">{{ formatTime(log.timestamp) }}</span>
            </div>

            <!-- Field-level changes for Updates -->
            <div
              v-if="log.changes && log.changes.length > 0"
              class="mt-1 space-y-0.5"
            >
              <div
                v-for="(change, i) in log.changes"
                :key="i"
                class="text-xs text-gray-500"
              >
                <span class="font-medium">{{ change.field }}</span>:
                <span class="line-through text-gray-400">{{ change.from || '(empty)' }}</span>
                →
                <span class="text-gray-700">{{ change.to || '(empty)' }}</span>
              </div>
            </div>

            <!-- Details for Created -->
            <div v-if="log.details" class="mt-1 text-xs text-gray-500">
              <span v-if="log.details.description">{{ log.details.description }}</span>
            </div>
          </div>
        </li>
      </ul>
    </div>
  </div>
</template>

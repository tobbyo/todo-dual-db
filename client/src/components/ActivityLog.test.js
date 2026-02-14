import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import ActivityLog from './ActivityLog.vue'

const makeLogs = () => [
  {
    id: '1',
    action: 'Created',
    todoId: 1,
    todoTitle: 'Test todo',
    timestamp: '2025-01-15T10:00:00Z',
    details: { description: 'A description', isComplete: false },
    changes: null,
    snapshot: null,
  },
  {
    id: '2',
    action: 'Updated',
    todoId: 1,
    todoTitle: 'Test todo',
    timestamp: '2025-01-15T11:00:00Z',
    details: null,
    changes: [{ field: 'title', from: 'Old', to: 'New' }],
    snapshot: null,
  },
  {
    id: '3',
    action: 'Deleted',
    todoId: 2,
    todoTitle: 'Deleted todo',
    timestamp: '2025-01-15T12:00:00Z',
    details: null,
    changes: null,
    snapshot: { id: 2, title: 'Deleted todo' },
  },
]

function mockFetchResponses(logs = [], count = 0) {
  return vi.fn((url) => {
    if (url.includes('/api/activity-logs/count')) {
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve({ count }),
      })
    }
    if (url.includes('/api/activity-logs')) {
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve(logs),
      })
    }
    return Promise.resolve({ ok: true, json: () => Promise.resolve([]) })
  })
}

describe('ActivityLog', () => {
  beforeEach(() => {
    global.fetch = mockFetchResponses([], 0)
  })

  it('renders collapsed by default', () => {
    const wrapper = mount(ActivityLog)
    expect(wrapper.text()).toContain('Activity Log')
    expect(wrapper.text()).not.toContain('No activity yet')
  })

  it('expands when clicking the header', async () => {
    global.fetch = mockFetchResponses([], 0)
    const wrapper = mount(ActivityLog)

    await wrapper.find('button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('No activity yet')
  })

  it('shows log entries when expanded', async () => {
    const logs = makeLogs()
    global.fetch = mockFetchResponses(logs, logs.length)
    const wrapper = mount(ActivityLog)

    await wrapper.find('button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Created')
    expect(wrapper.text()).toContain('Updated')
    expect(wrapper.text()).toContain('Deleted')
    expect(wrapper.text()).toContain('Test todo')
    expect(wrapper.text()).toContain('Deleted todo')
  })

  it('shows field-level changes for updates', async () => {
    const logs = makeLogs()
    global.fetch = mockFetchResponses(logs, logs.length)
    const wrapper = mount(ActivityLog)

    await wrapper.find('button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('title')
    expect(wrapper.text()).toContain('Old')
    expect(wrapper.text()).toContain('New')
  })

  it('shows count badge when logs exist', async () => {
    const logs = makeLogs()
    global.fetch = mockFetchResponses(logs, 3)
    const wrapper = mount(ActivityLog)

    // Call fetchLogs via exposed method to load count
    await wrapper.vm.fetchLogs()
    await flushPromises()

    expect(wrapper.text()).toContain('3')
  })

  it('fetchLogs is callable via ref', async () => {
    const logs = makeLogs()
    global.fetch = mockFetchResponses(logs, logs.length)
    const wrapper = mount(ActivityLog)

    await wrapper.vm.fetchLogs()
    await flushPromises()

    // After fetchLogs, expanding should show data without re-fetching
    await wrapper.find('button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Created')
  })
})

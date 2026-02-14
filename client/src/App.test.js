import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import App from './App.vue'

const makeTodo = (overrides = {}) => ({
  id: 1,
  title: 'Test todo',
  description: 'A description',
  isComplete: false,
  createdAt: '2025-01-15T10:00:00Z',
  ...overrides,
})

function mockFetch(data = []) {
  return vi.fn((url) => {
    if (url && url.includes('/api/activity-logs/count')) {
      return Promise.resolve({ ok: true, json: () => Promise.resolve({ count: 0 }) })
    }
    if (url && url.includes('/api/activity-logs')) {
      return Promise.resolve({ ok: true, json: () => Promise.resolve([]) })
    }
    return Promise.resolve({
      ok: true,
      json: () => Promise.resolve(data),
    })
  })
}

describe('App', () => {
  beforeEach(() => {
    global.fetch = mockFetch([])
  })

  it('renders the title', async () => {
    const wrapper = mount(App)
    await flushPromises()
    expect(wrapper.text()).toContain('Todo List')
  })

  it('shows empty state when no todos', async () => {
    const wrapper = mount(App)
    await flushPromises()
    expect(wrapper.text()).toContain('No todos yet')
  })

  it('renders todo items', async () => {
    global.fetch = mockFetch([
      makeTodo({ id: 1, title: 'First' }),
      makeTodo({ id: 2, title: 'Second' }),
    ])
    const wrapper = mount(App)
    await flushPromises()
    expect(wrapper.text()).toContain('First')
    expect(wrapper.text()).toContain('Second')
  })

  it('shows sort controls when todos exist', async () => {
    global.fetch = mockFetch([makeTodo()])
    const wrapper = mount(App)
    await flushPromises()
    expect(wrapper.text()).toContain('Sort by:')
    expect(wrapper.text()).toContain('Date')
    expect(wrapper.text()).toContain('Title')
    expect(wrapper.text()).toContain('Status')
  })

  it('hides sort controls when no todos', async () => {
    const wrapper = mount(App)
    await flushPromises()
    expect(wrapper.text()).not.toContain('Sort by:')
  })

  it('calls POST when adding a todo', async () => {
    global.fetch = mockFetch([])
    const wrapper = mount(App)
    await flushPromises()

    await wrapper.find('input[placeholder="What needs to be done?"]').setValue('New task')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    const postCall = global.fetch.mock.calls.find(
      ([, opts]) => opts && opts.method === 'POST'
    )
    expect(postCall).toBeTruthy()
    const body = JSON.parse(postCall[1].body)
    expect(body.title).toBe('New task')
  })

  it('does not POST when title is empty', async () => {
    global.fetch = mockFetch([])
    const wrapper = mount(App)
    await flushPromises()

    global.fetch.mockClear()
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    const postCall = global.fetch.mock.calls.find(
      ([, opts]) => opts && opts.method === 'POST'
    )
    expect(postCall).toBeUndefined()
  })

  it('calls DELETE when clicking delete', async () => {
    global.fetch = mockFetch([makeTodo({ id: 42, title: 'Delete me' })])
    const wrapper = mount(App)
    await flushPromises()

    const deleteBtn = wrapper.findAll('button').find((b) => b.text() === 'Delete')
    await deleteBtn.trigger('click')
    await flushPromises()

    const deleteCall = global.fetch.mock.calls.find(
      ([url, opts]) => opts && opts.method === 'DELETE'
    )
    expect(deleteCall).toBeTruthy()
    expect(deleteCall[0]).toContain('/42')
  })

  it('enters edit mode when clicking edit', async () => {
    global.fetch = mockFetch([makeTodo({ id: 1, title: 'Edit me' })])
    const wrapper = mount(App)
    await flushPromises()

    const editBtn = wrapper.findAll('button').find((b) => b.text() === 'Edit')
    await editBtn.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Save')
    expect(wrapper.text()).toContain('Cancel')
  })

  it('cancels edit mode', async () => {
    global.fetch = mockFetch([makeTodo({ id: 1, title: 'Edit me' })])
    const wrapper = mount(App)
    await flushPromises()

    const editBtn = wrapper.findAll('button').find((b) => b.text() === 'Edit')
    await editBtn.trigger('click')
    await flushPromises()

    const cancelBtn = wrapper.findAll('button').find((b) => b.text() === 'Cancel')
    await cancelBtn.trigger('click')
    await flushPromises()

    expect(wrapper.text()).not.toContain('Save')
    expect(wrapper.text()).toContain('Edit')
  })

  it('calls PUT when toggling complete', async () => {
    global.fetch = mockFetch([makeTodo({ id: 5, isComplete: false })])
    const wrapper = mount(App)
    await flushPromises()

    await wrapper.find('input[type="checkbox"]').trigger('change')
    await flushPromises()

    const putCall = global.fetch.mock.calls.find(
      ([url, opts]) => opts && opts.method === 'PUT'
    )
    expect(putCall).toBeTruthy()
    const body = JSON.parse(putCall[1].body)
    expect(body.isComplete).toBe(true)
  })

  it('shows completed todo with line-through', async () => {
    global.fetch = mockFetch([makeTodo({ id: 1, title: 'Done', isComplete: true })])
    const wrapper = mount(App)
    await flushPromises()

    const titleEl = wrapper.find('.line-through')
    expect(titleEl.exists()).toBe(true)
    expect(titleEl.text()).toBe('Done')
  })
})

describe('Sorting', () => {
  const todos = [
    makeTodo({ id: 1, title: 'Banana', isComplete: false, createdAt: '2025-01-10T00:00:00Z' }),
    makeTodo({ id: 2, title: 'Apple', isComplete: true, createdAt: '2025-01-20T00:00:00Z' }),
    makeTodo({ id: 3, title: 'Cherry', isComplete: false, createdAt: '2025-01-15T00:00:00Z' }),
  ]

  it('sorts by date descending by default', async () => {
    global.fetch = mockFetch(todos)
    const wrapper = mount(App)
    await flushPromises()

    const items = wrapper.findAll('li')
    expect(items[0].text()).toContain('Apple')
    expect(items[1].text()).toContain('Cherry')
    expect(items[2].text()).toContain('Banana')
  })

  it('sorts by title when clicking Title button', async () => {
    global.fetch = mockFetch(todos)
    const wrapper = mount(App)
    await flushPromises()

    const titleBtn = wrapper.findAll('button').find((b) => b.text() === 'Title')
    await titleBtn.trigger('click')
    await flushPromises()

    // Default dir is desc, so Z->A
    const items = wrapper.findAll('li')
    expect(items[0].text()).toContain('Cherry')
    expect(items[1].text()).toContain('Banana')
    expect(items[2].text()).toContain('Apple')
  })

  it('toggles sort direction', async () => {
    global.fetch = mockFetch(todos)
    const wrapper = mount(App)
    await flushPromises()

    const titleBtn = wrapper.findAll('button').find((b) => b.text() === 'Title')
    await titleBtn.trigger('click')

    const ascBtn = wrapper.findAll('button').find((b) => b.text().includes('Desc'))
    await ascBtn.trigger('click')
    await flushPromises()

    // Now A->Z
    const items = wrapper.findAll('li')
    expect(items[0].text()).toContain('Apple')
    expect(items[1].text()).toContain('Banana')
    expect(items[2].text()).toContain('Cherry')
  })

  it('sorts by status', async () => {
    global.fetch = mockFetch(todos)
    const wrapper = mount(App)
    await flushPromises()

    const statusBtn = wrapper.findAll('button').find((b) => b.text() === 'Status')
    await statusBtn.trigger('click')
    await flushPromises()

    // Desc: completed (true=1) first
    const items = wrapper.findAll('li')
    expect(items[0].text()).toContain('Apple')
  })
})

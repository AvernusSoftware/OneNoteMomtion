import { describe, expect, it, vi } from 'vitest'

const fetchLanguages = vi.fn(async () => [{ id: 'plain', label: 'Zwykły tekst' }])

vi.mock('../bridge/onenoteBridge', () => ({
  fetchLanguages: () => fetchLanguages(),
  fetchTexts: vi.fn(),
}))

const { loadLanguages } = await import('./useLanguages')

describe('loadLanguages', () => {
  it('asks the engine again only after the interface language changes', async () => {
    await loadLanguages('pl')
    await loadLanguages('pl')
    expect(fetchLanguages).toHaveBeenCalledTimes(1)

    await loadLanguages('en')
    expect(fetchLanguages).toHaveBeenCalledTimes(2)
  })
})

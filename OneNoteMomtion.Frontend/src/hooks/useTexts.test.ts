import { beforeEach, describe, expect, it, vi } from 'vitest'
import { currentTexts } from '../i18n/texts'
import type { UiTexts } from '../i18n/texts'

const pending: Array<(texts: UiTexts) => void> = []

vi.mock('../bridge/onenoteBridge', () => ({
  fetchTexts: () => new Promise<UiTexts>((resolve) => pending.push(resolve)),
}))

const { invalidateTexts } = await import('./useTexts')

describe('invalidateTexts', () => {
  beforeEach(() => {
    vi.stubGlobal('document', { documentElement: { lang: '' } })
  })

  it('keeps the newer texts when an older request answers last', async () => {
    invalidateTexts()
    invalidateTexts()
    const [older, newer] = pending

    newer({ language: 'pl', texts: { 'palette.back': 'Wstecz' } })
    await Promise.resolve()
    older({ language: 'en', texts: { 'palette.back': 'Back' } })
    await new Promise((resolve) => setTimeout(resolve, 0))

    expect(currentTexts().language).toBe('pl')
    expect(document.documentElement.lang).toBe('pl')
  })
})

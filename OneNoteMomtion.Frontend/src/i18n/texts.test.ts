import { describe, expect, it } from 'vitest'
import { englishTexts, readUiTexts, t } from './texts'

describe('t', () => {
  it('fills in the named values', () => {
    const texts = readUiTexts({ language: 'pl', texts: { 'palette.empty.noMatch': 'Brak akcji pasujących do „{{query}}”.' } })

    expect(t(texts, 'palette.empty.noMatch', { query: 'xyz' })).toBe('Brak akcji pasujących do „xyz”.')
  })

  it('falls back to English for a key the language does not have', () => {
    const texts = readUiTexts({ language: 'pl', texts: {} })

    expect(t(texts, 'palette.back')).toBe('Back')
  })
})

describe('readUiTexts', () => {
  it('keeps only the palette texts', () => {
    const texts = readUiTexts({ language: 'pl', texts: { 'palette.back': 'Wstecz', 'ribbon.grpTools.label': 'Narzędzia' } })

    expect(texts).toEqual({ language: 'pl', texts: { 'palette.back': 'Wstecz' } })
  })

  it('uses English when the engine sent something unreadable', () => {
    expect(readUiTexts(null)).toBe(englishTexts)
  })
})

import english from '../../../OneNoteMomtion.Core/Localization/en.json'

// The palette has no dictionary of its own: the engine sends the "palette.*" texts of the language chosen in
// styles.json. English is bundled from the same file as the fallback and for development mode.
export type PaletteTextKey = Extract<keyof typeof english, `palette.${string}`>

export interface UiTexts {
  language: string
  texts: Record<string, string>
}

function paletteTextsOf(source: Record<string, string>): Record<string, string> {
  return Object.fromEntries(Object.entries(source).filter(([key]) => key.startsWith('palette.')))
}

export const englishTexts: UiTexts = { language: 'en', texts: paletteTextsOf(english) }

let current: UiTexts = englishTexts

export function currentTexts(): UiTexts {
  return current
}

export function setCurrentTexts(texts: UiTexts): void {
  current = texts
}

export function readUiTexts(data: unknown): UiTexts {
  if (typeof data !== 'object' || data === null) {
    return englishTexts
  }

  const source = data as Record<string, unknown>
  const texts = typeof source.texts === 'object' && source.texts !== null ? (source.texts as Record<string, string>) : {}

  return { language: typeof source.language === 'string' ? source.language : 'en', texts: paletteTextsOf(texts) }
}

export function t(texts: UiTexts, key: PaletteTextKey, values?: Record<string, string>): string {
  const template = texts.texts[key] ?? englishTexts.texts[key] ?? key

  return template.replace(/\{\{(\w+)\}\}/g, (placeholder, name: string) => values?.[name] ?? placeholder)
}

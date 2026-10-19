import { useEffect, useState } from 'react'
import { fetchLanguages } from '../bridge/onenoteBridge'
import { useTexts } from './useTexts'

export interface Language {
  id: string
  label: string
}

export interface LanguagesState {
  languages: Language[]
  error: string | null
  loading: boolean
}

// The engine names "Plain text" in the interface language, so the list is fetched again once that changes.
let cached: { language: string; languages: Promise<Language[]> } | null = null

export function loadLanguages(language: string): Promise<Language[]> {
  if (cached === null || cached.language !== language) {
    const languages = fetchLanguages().catch((reason: unknown) => {
      if (cached?.languages === languages) {
        cached = null
      }

      throw reason
    })

    cached = { language, languages }
  }

  return cached.languages
}

export function useLanguages(): LanguagesState {
  const [state, setState] = useState<LanguagesState>({ languages: [], error: null, loading: true })
  const { language } = useTexts()

  useEffect(() => {
    let cancelled = false

    loadLanguages(language)
      .then((languages) => {
        if (!cancelled) {
          setState({ languages, error: null, loading: false })
        }
      })
      .catch((reason: unknown) => {
        if (!cancelled) {
          setState({
            languages: [],
            error: reason instanceof Error ? reason.message : String(reason),
            loading: false,
          })
        }
      })

    return () => {
      cancelled = true
    }
  }, [language])

  return state
}

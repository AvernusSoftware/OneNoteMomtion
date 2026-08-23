import { useEffect, useState } from 'react'
import { fetchLanguages } from '../bridge/onenoteBridge'

export interface Language {
  id: string
  label: string
}

export interface LanguagesState {
  languages: Language[]
  error: string | null
  loading: boolean
}

let cached: Promise<Language[]> | null = null

function loadLanguages(): Promise<Language[]> {
  if (cached === null) {
    cached = fetchLanguages().catch((reason: unknown) => {
      cached = null
      throw reason
    })
  }

  return cached
}

export function useLanguages(): LanguagesState {
  const [state, setState] = useState<LanguagesState>({ languages: [], error: null, loading: true })

  useEffect(() => {
    let cancelled = false

    loadLanguages()
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
  }, [])

  return state
}

import { useEffect, useState } from 'react'
import { fetchTexts } from '../bridge/onenoteBridge'
import { currentTexts, setCurrentTexts } from '../i18n/texts'
import type { UiTexts } from '../i18n/texts'

let loaded = false
let inFlight: Promise<UiTexts> | null = null

const listeners = new Set<(texts: UiTexts) => void>()

function loadTexts(): Promise<UiTexts> {
  if (loaded) {
    return Promise.resolve(currentTexts())
  }

  if (inFlight === null) {
    inFlight = fetchTexts()
      .then((texts) => {
        setCurrentTexts(texts)
        document.documentElement.lang = texts.language
        loaded = true
        inFlight = null
        listeners.forEach((listener) => listener(texts))
        return texts
      })
      .catch((reason: unknown) => {
        inFlight = null
        throw reason
      })
  }

  return inFlight
}

export function invalidateTexts(): void {
  loaded = false
  inFlight = null

  void loadTexts().catch(() => undefined)
}

export function useTexts(): UiTexts {
  const [texts, setTexts] = useState<UiTexts>(currentTexts())

  useEffect(() => {
    let cancelled = false

    const listener = (next: UiTexts) => {
      if (!cancelled) {
        setTexts(next)
      }
    }

    listeners.add(listener)

    loadTexts()
      .then(listener)
      .catch(() => undefined)

    return () => {
      cancelled = true
      listeners.delete(listener)
    }
  }, [])

  return texts
}

import { useEffect, useState } from 'react'
import { fetchTexts } from '../bridge/onenoteBridge'
import { currentTexts, setCurrentTexts } from '../i18n/texts'
import type { UiTexts } from '../i18n/texts'

let loaded = false
let inFlight: Promise<UiTexts> | null = null

// Bumped by every invalidation, so that an answer to an older request - say, one sent before the user saved a
// new language - cannot overwrite the texts of a newer one when it happens to arrive later.
let generation = 0

const listeners = new Set<(texts: UiTexts) => void>()

function loadTexts(): Promise<UiTexts> {
  if (loaded) {
    return Promise.resolve(currentTexts())
  }

  if (inFlight === null) {
    const requested = generation

    const request: Promise<UiTexts> = fetchTexts()
      .then((texts) => {
        if (requested !== generation) {
          return currentTexts()
        }

        setCurrentTexts(texts)
        document.documentElement.lang = texts.language
        loaded = true
        inFlight = null
        listeners.forEach((listener) => listener(texts))
        return texts
      })
      .catch((reason: unknown) => {
        if (inFlight === request) {
          inFlight = null
        }

        throw reason
      })

    inFlight = request
  }

  return inFlight
}

export function invalidateTexts(): void {
  generation++
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

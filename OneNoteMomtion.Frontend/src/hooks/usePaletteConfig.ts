import { useEffect, useState } from 'react'
import { fetchPaletteConfig } from '../bridge/onenoteBridge'
import { emptyPaletteConfig } from '../paletteConfig'
import type { PaletteConfig } from '../paletteConfig'

let cached: PaletteConfig | null = null
let inFlight: Promise<PaletteConfig> | null = null

const listeners = new Set<(config: PaletteConfig) => void>()

function loadPaletteConfig(): Promise<PaletteConfig> {
  if (cached !== null) {
    return Promise.resolve(cached)
  }

  if (inFlight === null) {
    inFlight = fetchPaletteConfig()
      .then((config) => {
        cached = config
        inFlight = null
        listeners.forEach((listener) => listener(config))
        return config
      })
      .catch((reason: unknown) => {
        inFlight = null
        throw reason
      })
  }

  return inFlight
}

export function invalidatePaletteConfig(): void {
  cached = null
  inFlight = null

  void loadPaletteConfig().catch(() => undefined)
}

export function usePaletteConfig(): PaletteConfig {
  const [config, setConfig] = useState<PaletteConfig>(cached ?? emptyPaletteConfig)

  useEffect(() => {
    let cancelled = false

    const listener = (next: PaletteConfig) => {
      if (!cancelled) {
        setConfig(next)
      }
    }

    listeners.add(listener)

    loadPaletteConfig()
      .then(listener)
      .catch(() => undefined)

    return () => {
      cancelled = true
      listeners.delete(listener)
    }
  }, [])

  return config
}

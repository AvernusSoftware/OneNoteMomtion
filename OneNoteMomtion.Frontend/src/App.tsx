import { useCallback, useEffect, useMemo, useState } from 'react'
import { Info } from 'lucide-react'
import FloatingPalette from './components/FloatingPalette'
import LanguagePicker from './components/LanguagePicker'
import StyleSettings from './components/StyleSettings'
import SubmenuList from './components/SubmenuList'
import { actionKey, caseActions } from './commands'
import type { PaletteAction } from './commands'
import { useLanguages } from './hooks/useLanguages'
import { invalidatePaletteConfig, usePaletteConfig } from './hooks/usePaletteConfig'
import { orderCaseActions, orderLanguages } from './paletteConfig'
import { closePalette, isHosted, onViewChange, requestResize, sendCommand } from './bridge/onenoteBridge'

type View = 'actions' | 'languages' | 'case' | 'settings'

interface Status {
  message: string
  ok: boolean
}

const SUBMENU_HEADER = 41 // header with the back button (py-2 + icon) together with the rule below it
const SUBMENU_LIST_PADDING = 12 // py-1.5 above the first and below the last row
const SUBMENU_ROW = 32 // py-1.5 around a single 13px/1.5 line
const SUBMENU_ROW_WITH_HINT = 49 // the same row with a second 11px/1.5 line
const SUBMENU_PLACEHOLDER = 66 // a message instead of the list - "Loading…", an error, an empty registry
const SUBMENU_MAX_HEIGHT = 560 // beyond this the list scrolls instead of covering the screen

function submenuHeight(contentHeight: number): number {
  return Math.min(SUBMENU_HEADER + SUBMENU_LIST_PADDING + contentHeight, SUBMENU_MAX_HEIGHT)
}

export default function App() {
  const [view, setView] = useState<View>('actions')
  const [status, setStatus] = useState<Status | null>(null)
  const { languages } = useLanguages()
  const config = usePaletteConfig()

  const caseItems = useMemo(
    () =>
      orderCaseActions(caseActions, config).map((action) => ({
        id: action.id,
        label: action.label,
        hint: action.hint,
        icon: action.icon,
      })),
    [config],
  )

  const visibleLanguageCount = useMemo(() => orderLanguages(languages, config).length, [languages, config])

  const [query, setQuery] = useState('')
  const [restoreActiveId, setRestoreActiveId] = useState<string | null>(null)

  useEffect(
    () =>
      onViewChange((next) => {
        setView(next === 'settings' ? 'settings' : 'actions')
        setQuery('')
        setRestoreActiveId(null)
        invalidatePaletteConfig()
      }),
    [],
  )

  const viewSize = useMemo<Record<View, { width: number; height: number }>>(
    () => ({
      actions: { width: 380, height: 460 },
      languages: {
        width: 380,
        height: submenuHeight(
          visibleLanguageCount === 0 ? SUBMENU_PLACEHOLDER : visibleLanguageCount * SUBMENU_ROW,
        ),
      },
      case: {
        width: 380,
        height: submenuHeight(
          caseItems.length === 0 ? SUBMENU_PLACEHOLDER : caseItems.length * SUBMENU_ROW_WITH_HINT,
        ),
      },
      settings: { width: 520, height: 560 },
    }),
    [visibleLanguageCount, caseItems.length],
  )

  useEffect(() => {
    const size = viewSize[view]
    requestResize(size.width, size.height)
  }, [view, viewSize])

  useEffect(() => {
    if (!status) {
      return
    }

    const timer = window.setTimeout(() => setStatus(null), 4000)
    return () => window.clearTimeout(timer)
  }, [status])

  const run = useCallback(async (command: string, argument?: string) => {
    try {
      const response = await sendCommand(command, argument)

      if (!response.success) {
        setStatus({ message: response.message ?? 'The command failed.', ok: false })
        return
      }

      if (!response.changed && response.message) {
        setStatus({ message: response.message, ok: true })
      }
    } catch (reason) {
      setStatus({ message: reason instanceof Error ? reason.message : String(reason), ok: false })
    }
  }, [])

  const handleAction = useCallback(
    (action: PaletteAction) => {
      if (action.opens) {
        setRestoreActiveId(actionKey(action))
        setView(action.opens)
        return
      }

      void run(action.id, action.argument)
    },
    [run],
  )

  return (
    <div className="flex h-full flex-col overflow-hidden rounded-[--radius-palette] border border-border-subtle bg-surface shadow-[0_12px_32px_rgba(15,15,15,0.18)]">
      {!isHosted && (
        <div className="bg-accent-soft px-3 py-1 text-center text-[11px] text-ink-muted">
          Development mode — no connection to OneNote, commands are simulated.
        </div>
      )}

      <div className="min-h-0 flex-1">
        {view === 'actions' && (
          <FloatingPalette
            query={query}
            onQueryChange={setQuery}
            restoreActiveId={restoreActiveId}
            onRun={handleAction}
            onClose={closePalette}
          />
        )}

        {view === 'languages' && (
          <LanguagePicker
            onBack={() => setView('actions')}
            onPick={(languageId) => {
              setView('actions')
              void run('code.format', languageId)
            }}
          />
        )}

        {view === 'case' && (
          <SubmenuList
            title="Letter case"
            items={caseItems}
            onBack={() => setView('actions')}
            onPick={(commandId) => {
              setView('actions')
              void run(commandId)
            }}
            placeholder={
              caseItems.length === 0 && (
                <p className="px-3 py-6 text-center text-[12px] text-ink-muted">
                  Every entry is switched off in styles.json.
                </p>
              )
            }
          />
        )}

        {view === 'settings' && (
          <StyleSettings
            onBack={() => setView('actions')}
            onStatus={(message, ok) => setStatus({ message, ok })}
          />
        )}
      </div>

      {status && (
        <div
          role="status"
          className={`flex items-start gap-2 border-t border-border-subtle px-3 py-2 text-[11.5px] ${
            status.ok ? 'text-ink-muted' : 'text-ink'
          }`}
        >
          <Info className="mt-0.5 size-3.5 shrink-0" aria-hidden />
          <span>{status.message}</span>
        </div>
      )}
    </div>
  )
}

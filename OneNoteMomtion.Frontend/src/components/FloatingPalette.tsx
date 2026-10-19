import { useMemo, useRef } from 'react'
import { ChevronRight, Search } from 'lucide-react'
import { actionKey, buildPaletteActions, filterActions, groupLabel } from '../commands'
import type { PaletteAction } from '../commands'
import { t } from '../i18n/texts'
import { useLanguages } from '../hooks/useLanguages'
import { useListNavigation } from '../hooks/useListNavigation'
import { usePaletteConfig } from '../hooks/usePaletteConfig'
import { useTexts } from '../hooks/useTexts'

interface FloatingPaletteProps {
  query: string
  onQueryChange: (query: string) => void
  restoreActiveId?: string | null
  onRun: (action: PaletteAction) => void
  onClose: () => void
}

function caretAtEnd(input: HTMLInputElement | null): boolean {
  if (input === null) {
    return true
  }

  return input.selectionStart === input.selectionEnd && input.selectionStart === input.value.length
}

export default function FloatingPalette({
  query,
  onQueryChange,
  restoreActiveId,
  onRun,
  onClose,
}: FloatingPaletteProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const { languages } = useLanguages()
  const config = usePaletteConfig()
  const texts = useTexts()
  const actions = useMemo(() => buildPaletteActions(languages, texts, config), [languages, texts, config])
  const matches = useMemo(() => filterActions(actions, query, texts), [actions, query, texts])

  const restoredIndex = matches.findIndex((action) => actionKey(action) === restoreActiveId)

  const { activeIndex, listRef, handleKeyDown, handleHover, handleMouseMove } = useListNavigation({
    count: matches.length,
    resetKey: query,
    initialIndex: restoredIndex < 0 ? 0 : restoredIndex,
    onActivate: (index) => {
      const action = matches[index]
      if (action) {
        onRun(action)
      }
    },
    onEscape: onClose,
    onExpand: (index) => {
      const action = matches[index]
      if (action?.opens === undefined || !caretAtEnd(inputRef.current)) {
        return false
      }

      onRun(action)
      return true
    },
  })

  let lastGroup: string = ''

  return (
    <div className="flex h-full flex-col" onKeyDown={handleKeyDown}>
      <div className="flex items-center gap-2 border-b border-border-subtle px-3 py-2.5">
        <Search className="size-4 shrink-0 text-ink-muted" aria-hidden />
        <input
          ref={inputRef}
          autoFocus
          value={query}
          onChange={(event) => onQueryChange(event.target.value)}
          placeholder={t(texts, 'palette.search.placeholder')}
          aria-label={t(texts, 'palette.search.label')}
          className="w-full bg-transparent text-[13px] text-ink outline-none placeholder:text-ink-muted"
        />
      </div>

      <div ref={listRef} className="flex-1 overflow-y-auto py-1.5" onMouseMove={handleMouseMove}>
        {matches.length === 0 ? (
          <p className="px-3 py-6 text-center text-[12px] text-ink-muted">
            {query.trim().length === 0 ? t(texts, 'palette.empty.allDisabled') : t(texts, 'palette.empty.noMatch', { query })}
          </p>
        ) : (
          matches.map((action, index) => {
            const showGroup = action.group !== lastGroup
            lastGroup = action.group
            const Icon = action.icon
            const isActive = index === activeIndex

            return (
              <div key={actionKey(action)}>
                {showGroup && (
                  <p className="px-3 pt-2.5 pb-1 text-[10px] font-semibold uppercase tracking-wider text-ink-muted">
                    {groupLabel(action.group, texts)}
                  </p>
                )}

                <button
                  type="button"
                  data-active={isActive}
                  onMouseEnter={() => handleHover(index)}
                  onClick={() => onRun(action)}
                  className={`flex w-full items-center gap-2.5 px-3 py-1.5 text-left transition-colors ${
                    isActive ? 'bg-accent-soft' : 'bg-transparent'
                  }`}
                >
                  <Icon className="size-4 shrink-0 text-ink-muted" aria-hidden />

                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-[13px] text-ink">{action.label}</span>
                    <span className="block truncate text-[11px] text-ink-muted">{action.hint}</span>
                  </span>

                  {action.opens && <ChevronRight className="size-3.5 shrink-0 text-ink-muted" aria-hidden />}
                </button>
              </div>
            )
          })
        )}
      </div>
    </div>
  )
}
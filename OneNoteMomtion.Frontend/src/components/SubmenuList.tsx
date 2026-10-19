import type { ReactNode } from 'react'
import { ArrowLeft } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { t } from '../i18n/texts'
import { useListNavigation } from '../hooks/useListNavigation'
import { useTexts } from '../hooks/useTexts'

export interface SubmenuItem {
  id: string
  label: string
  icon: LucideIcon
  hint?: string
  trailing?: string
}

interface SubmenuListProps {
  title: string
  items: SubmenuItem[]
  onPick: (id: string) => void
  onBack: () => void
  placeholder?: ReactNode
}

export default function SubmenuList({ title, items, onPick, onBack, placeholder }: SubmenuListProps) {
  const texts = useTexts()
  const { activeIndex, listRef, handleKeyDown, handleHover, handleMouseMove } = useListNavigation({
    count: items.length,
    onActivate: (index) => {
      const item = items[index]
      if (item) {
        onPick(item.id)
      }
    },
    onEscape: onBack,
    onCollapse: () => {
      onBack()
      return true
    },
  })

  return (
    <div className="flex h-full flex-col" onKeyDown={handleKeyDown}>
      <div className="flex items-center gap-2 border-b border-border-subtle px-2 py-2">
        <button
          type="button"
          autoFocus
          onClick={onBack}
          aria-label={t(texts, 'palette.back')}
          className="rounded-md p-1 text-ink-muted transition-colors hover:bg-surface-sunken"
        >
          <ArrowLeft className="size-4" aria-hidden />
        </button>
        <span className="text-[13px] font-medium text-ink">{title}</span>
      </div>

      <div ref={listRef} className="flex-1 overflow-y-auto py-1.5" onMouseMove={handleMouseMove}>
        {placeholder ?? null}

        {items.map((item, index) => {
          const Icon = item.icon
          const isActive = index === activeIndex

          return (
            <button
              key={item.id}
              type="button"
              data-active={isActive}
              onMouseEnter={() => handleHover(index)}
              onClick={() => onPick(item.id)}
              className={`flex w-full items-center gap-2.5 px-3 py-1.5 text-left transition-colors ${
                isActive ? 'bg-accent-soft' : 'bg-transparent'
              }`}
            >
              <Icon className="size-4 shrink-0 text-ink-muted" aria-hidden />

              <span className="min-w-0 flex-1">
                <span className="block truncate text-[13px] text-ink">{item.label}</span>
                {item.hint && <span className="block truncate text-[11px] text-ink-muted">{item.hint}</span>}
              </span>

              {item.trailing && <span className="text-[11px] text-ink-muted">{item.trailing}</span>}
            </button>
          )
        })}
      </div>
    </div>
  )
}

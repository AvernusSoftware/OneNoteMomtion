import { useCallback, useEffect, useRef, useState } from 'react'

export interface ListNavigationOptions {
  count: number
  onActivate: (index: number) => void
  onEscape: () => void
  onExpand?: (index: number) => boolean
  onCollapse?: () => boolean
  resetKey?: unknown
  initialIndex?: number
}

export function useListNavigation(options: ListNavigationOptions) {
  const { count, onActivate, onEscape, onExpand, onCollapse, resetKey, initialIndex } = options

  const [activeIndex, setActiveIndex] = useState(initialIndex ?? 0)
  const listRef = useRef<HTMLDivElement>(null)
  const mountedRef = useRef(false)
  const suppressHoverRef = useRef(false)

  useEffect(() => {
    if (!mountedRef.current) {
      mountedRef.current = true
      return
    }

    setActiveIndex(0)
    suppressHoverRef.current = true
  }, [resetKey, count])

  useEffect(() => {
    const active = listRef.current?.querySelector<HTMLElement>('[data-active="true"]')
    active?.scrollIntoView({ block: 'nearest' })
  }, [activeIndex])

  const handleKeyDown = useCallback(
    (event: React.KeyboardEvent) => {
      if (event.key === 'ArrowDown') {
        event.preventDefault()
        suppressHoverRef.current = true
        setActiveIndex((index) => (count === 0 ? 0 : (index + 1) % count))
        return
      }

      if (event.key === 'ArrowUp') {
        event.preventDefault()
        suppressHoverRef.current = true
        setActiveIndex((index) => (count === 0 ? 0 : (index - 1 + count) % count))
        return
      }

      if (event.key === 'ArrowRight') {
        if (onExpand?.(activeIndex)) {
          event.preventDefault()
        }

        return
      }

      if (event.key === 'ArrowLeft') {
        if (onCollapse?.()) {
          event.preventDefault()
        }

        return
      }

      if (event.key === 'Enter') {
        event.preventDefault()
        onActivate(activeIndex)
        return
      }

      if (event.key === 'Escape') {
        event.preventDefault()
        onEscape()
      }
    },
    [activeIndex, count, onActivate, onCollapse, onEscape, onExpand],
  )

  const handleHover = useCallback((index: number) => {
    if (suppressHoverRef.current) {
      return
    }

    setActiveIndex(index)
  }, [])

  const handleMouseMove = useCallback(() => {
    suppressHoverRef.current = false
  }, [])

  return { activeIndex, listRef, handleKeyDown, handleHover, handleMouseMove }
}

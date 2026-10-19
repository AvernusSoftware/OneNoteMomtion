import { useEffect, useState } from 'react'
import { ArrowLeft, FolderOpen, RefreshCw, Save } from 'lucide-react'
import { sendCommand } from '../bridge/onenoteBridge'
import { t } from '../i18n/texts'
import { invalidatePaletteConfig } from '../hooks/usePaletteConfig'
import { invalidateTexts, useTexts } from '../hooks/useTexts'

interface StyleSettingsProps {
  onBack: () => void
  onStatus: (message: string, ok: boolean) => void
}

export default function StyleSettings({ onBack, onStatus }: StyleSettingsProps) {
  const [text, setText] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [dirty, setDirty] = useState(false)
  const texts = useTexts()

  useEffect(() => {
    void load()
  }, [])

  async function load() {
    setLoading(true)

    try {
      const response = await sendCommand('settings.get')
      setText(typeof response.data === 'string' ? response.data : '')
      setDirty(false)
    } catch (reason) {
      onStatus(reason instanceof Error ? reason.message : String(reason), false)
    } finally {
      setLoading(false)
    }
  }

  async function save() {
    try {
      JSON.parse(text)
    } catch (reason) {
      onStatus(t(texts, 'palette.settings.invalidJson', { error: reason instanceof Error ? reason.message : String(reason) }), false)
      return
    }

    setSaving(true)

    try {
      const response = await sendCommand('settings.save', text)
      onStatus(response.message ?? t(texts, response.success ? 'palette.settings.saved' : 'palette.settings.saveFailed'), response.success)

      if (response.success) {
        setDirty(false)
        invalidatePaletteConfig()
        invalidateTexts()
      }
    } catch (reason) {
      onStatus(reason instanceof Error ? reason.message : String(reason), false)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div
      className="flex h-full flex-col"
      onKeyDown={(event) => {
        if (event.key === 'Escape') {
          event.preventDefault()
          onBack()
          return
        }

        if (event.key === 's' && (event.ctrlKey || event.metaKey)) {
          event.preventDefault()
          void save()
        }
      }}
    >
      <div className="flex items-center gap-2 border-b border-border-subtle px-2 py-2">
        <button
          type="button"
          onClick={onBack}
          aria-label={t(texts, 'palette.back')}
          className="rounded-md p-1 text-ink-muted transition-colors hover:bg-surface-sunken"
        >
          <ArrowLeft className="size-4" aria-hidden />
        </button>

        <span className="flex-1 text-[13px] font-medium text-ink">
          styles.json{dirty ? ' •' : ''}
        </span>

        <button
          type="button"
          onClick={() => void sendCommand('settings.openFolder')}
          title={t(texts, 'palette.settings.openFolder')}
          aria-label={t(texts, 'palette.settings.openFolder')}
          className="rounded-md p-1 text-ink-muted transition-colors hover:bg-surface-sunken"
        >
          <FolderOpen className="size-4" aria-hidden />
        </button>

        <button
          type="button"
          onClick={() => void load()}
          title={t(texts, 'palette.settings.reload')}
          aria-label={t(texts, 'palette.settings.reload')}
          className="rounded-md p-1 text-ink-muted transition-colors hover:bg-surface-sunken"
        >
          <RefreshCw className="size-4" aria-hidden />
        </button>
      </div>

      <textarea
        value={loading ? t(texts, 'palette.loading') : text}
        readOnly={loading}
        spellCheck={false}
        onChange={(event) => {
          setText(event.target.value)
          setDirty(true)
        }}
        className="flex-1 resize-none bg-surface-sunken px-3 py-2 font-mono text-[11.5px] leading-relaxed text-ink outline-none"
      />

      <div className="flex items-center justify-end gap-2 border-t border-border-subtle px-3 py-2">
        <span className="mr-auto text-[11px] text-ink-muted">{t(texts, 'palette.settings.saveHint')}</span>
        <button
          type="button"
          disabled={saving || loading}
          onClick={() => void save()}
          className="flex items-center gap-1.5 rounded-md bg-accent px-2.5 py-1 text-[12px] font-medium text-white transition-opacity disabled:opacity-50"
        >
          <Save className="size-3.5" aria-hidden />
          {saving ? t(texts, 'palette.settings.saving') : t(texts, 'palette.settings.save')}
        </button>
      </div>
    </div>
  )
}

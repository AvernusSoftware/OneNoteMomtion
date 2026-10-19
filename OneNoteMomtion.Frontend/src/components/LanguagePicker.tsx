import { useMemo } from 'react'
import { Braces } from 'lucide-react'
import { useLanguages } from '../hooks/useLanguages'
import { t } from '../i18n/texts'
import { usePaletteConfig } from '../hooks/usePaletteConfig'
import { useTexts } from '../hooks/useTexts'
import { orderLanguages } from '../paletteConfig'
import SubmenuList from './SubmenuList'

interface LanguagePickerProps {
  onPick: (languageId: string) => void
  onBack: () => void
}

export default function LanguagePicker({ onPick, onBack }: LanguagePickerProps) {
  const { languages, error, loading } = useLanguages()
  const config = usePaletteConfig()
  const texts = useTexts()

  const items = useMemo(
    () =>
      orderLanguages(languages, config).map((language) => ({
        id: language.id,
        label: config.languages[language.id]?.label ?? language.label,
        icon: Braces,
        trailing: language.id,
      })),
    [languages, config],
  )

  let placeholder: string | null = null

  if (loading) {
    placeholder = t(texts, 'palette.loading')
  } else if (error) {
    placeholder = error
  } else if (languages.length === 0) {
    placeholder = t(texts, 'palette.languages.none')
  } else if (items.length === 0) {
    placeholder = t(texts, 'palette.languages.allDisabled')
  }

  return (
    <SubmenuList
      title={t(texts, 'palette.languages.title')}
      items={items}
      onPick={onPick}
      onBack={onBack}
      placeholder={
        placeholder && <p className="px-3 py-6 text-center text-[12px] text-ink-muted">{placeholder}</p>
      }
    />
  )
}

import { useMemo } from 'react'
import { Braces } from 'lucide-react'
import { useLanguages } from '../hooks/useLanguages'
import { usePaletteConfig } from '../hooks/usePaletteConfig'
import { orderLanguages } from '../paletteConfig'
import SubmenuList from './SubmenuList'

interface LanguagePickerProps {
  onPick: (languageId: string) => void
  onBack: () => void
}

export default function LanguagePicker({ onPick, onBack }: LanguagePickerProps) {
  const { languages, error, loading } = useLanguages()
  const config = usePaletteConfig()

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
    placeholder = 'Loading…'
  } else if (error) {
    placeholder = error
  } else if (languages.length === 0) {
    placeholder = 'The engine returned no languages.'
  } else if (items.length === 0) {
    placeholder = 'Every language is switched off in styles.json.'
  }

  return (
    <SubmenuList
      title="Code block language"
      items={items}
      onPick={onPick}
      onBack={onBack}
      placeholder={
        placeholder && <p className="px-3 py-6 text-center text-[12px] text-ink-muted">{placeholder}</p>
      }
    />
  )
}

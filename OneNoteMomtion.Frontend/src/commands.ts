import {
  Braces,
  Bug,
  CaseLower,
  CaseSensitive,
  CaseUpper,
  FolderMinus,
  FolderTree,
  Heading1,
  Heading2,
  Heading3,
  Pilcrow,
  Shuffle,
  SpellCheck,
  Type,
  Wand2,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { t } from './i18n/texts'
import type { PaletteTextKey, UiTexts } from './i18n/texts'
import { applyOverrides, emptyPaletteConfig, orderLanguages, orderPaletteActions } from './paletteConfig'
import type { PaletteConfig } from './paletteConfig'

export type SubmenuId = 'languages' | 'case'
export type GroupId = 'styles' | 'navigation' | 'code' | 'letterCase' | 'tools'

export interface PaletteGroup {
  id: GroupId
}

export interface PaletteAction {
  id: string
  argument?: string
  label: string
  hint: string
  icon: LucideIcon
  group: GroupId
  keywords?: string[]
  opens?: SubmenuId
  children?: PaletteAction[]
  hidden?: boolean
}

// Labels and hints come from the texts of the interface language ("palette.action.<id>.label" and ".hint").
// The keywords listed here are the English ones, searchable in every language; ".keywords" adds the localized ones.
export type PaletteActionDefinition = Omit<PaletteAction, 'label' | 'hint' | 'children'> & {
  children?: PaletteActionDefinition[]
}

export const paletteGroups: PaletteGroup[] = [
  { id: 'styles' },
  { id: 'navigation' },
  { id: 'code' },
  { id: 'letterCase' },
  { id: 'tools' },
]

export function groupLabel(id: GroupId, texts: UiTexts): string {
  return t(texts, `palette.group.${id}` as PaletteTextKey)
}

export function actionKey(action: PaletteAction): string {
  return action.argument === undefined ? action.id : action.id + ':' + action.argument
}

export const caseActions: PaletteActionDefinition[] = [
  {
    id: 'case.upper',
    icon: CaseUpper,
    group: 'letterCase',
    keywords: ['uppercase', 'caps'],
  },
  {
    id: 'case.lower',
    icon: CaseLower,
    group: 'letterCase',
    keywords: ['lowercase'],
  },
  {
    id: 'case.capitalize',
    icon: CaseSensitive,
    group: 'letterCase',
    keywords: ['words', 'title'],
  },
  {
    id: 'case.sentence',
    icon: Type,
    group: 'letterCase',
    keywords: ['sentence'],
  },
  {
    id: 'case.toggle',
    icon: Shuffle,
    group: 'letterCase',
    keywords: ['invert', 'swap'],
  },
]

export const paletteActions: PaletteActionDefinition[] = [
  {
    id: 'style.heading1',
    icon: Heading1,
    group: 'styles',
    keywords: ['h1', 'heading', 'title'],
  },
  {
    id: 'style.heading2',
    icon: Heading2,
    group: 'styles',
    keywords: ['h2', 'heading'],
  },
  {
    id: 'style.heading3',
    icon: Heading3,
    group: 'styles',
    keywords: ['h3', 'heading'],
  },
  {
    id: 'style.normal',
    icon: Pilcrow,
    group: 'styles',
    keywords: ['plain', 'paragraph', 'reset'],
  },
  {
    id: 'nav.reveal',
    icon: FolderTree,
    group: 'navigation',
    keywords: ['navigation', 'notebook', 'section', 'where', 'locate', 'tree', 'pane', 'reveal'],
  },
  {
    id: 'nav.collapseAll',
    icon: FolderMinus,
    group: 'navigation',
    keywords: ['navigation', 'notebook', 'section', 'collapse', 'fold', 'close', 'tree', 'pane'],
  },
  {
    id: 'nav.dump',
    icon: Bug,
    group: 'navigation',
    keywords: ['diagnostics', 'dump', 'uia', 'automation', 'debug'],
    hidden: true,
  },
  {
    id: 'code.format',
    icon: Braces,
    group: 'code',
    keywords: ['block', 'syntax', 'colour', 'highlight'],
    opens: 'languages',
  },
  {
    id: 'case.menu',
    icon: CaseSensitive,
    group: 'letterCase',
    keywords: ['uppercase', 'lowercase', 'caps', 'case'],
    opens: 'case',
    children: caseActions,
  },
  {
    id: 'page.reformat',
    icon: Wand2,
    group: 'tools',
    keywords: ['reformat', 'format', 'cleanup', 'tidy', 'whitespace', 'page', 'sprzatanie'],
  },
  {
    id: 'spell.disable',
    icon: SpellCheck,
    group: 'tools',
    keywords: ['spelling', 'underlines'],
  },
  {
    id: 'spell.enable',
    icon: SpellCheck,
    group: 'tools',
    keywords: ['spelling'],
  },
]

function localizedKeywords(texts: UiTexts, key: PaletteTextKey): string[] {
  return t(texts, key).split(' ').filter((keyword) => keyword.length > 0)
}

export function localizeAction(action: PaletteActionDefinition, texts: UiTexts): PaletteAction {
  const prefix = `palette.action.${action.id}`

  return {
    ...action,
    label: t(texts, `${prefix}.label` as PaletteTextKey),
    hint: t(texts, `${prefix}.hint` as PaletteTextKey),
    keywords: [...(action.keywords ?? []), ...localizedKeywords(texts, `${prefix}.keywords` as PaletteTextKey)],
    children: action.children?.map((child) => localizeAction(child, texts)),
  }
}

function languageAction(language: { id: string; label: string }, config: PaletteConfig, texts: UiTexts): PaletteAction {
  const action: PaletteAction = {
    id: 'code.format',
    argument: language.id,
    label: language.label,
    hint: t(texts, 'palette.language.hint', { language: language.label }),
    icon: Braces,
    group: 'code',
    keywords: [language.id, 'block', 'syntax', 'colour', 'highlight', ...localizedKeywords(texts, 'palette.language.keywords')],
  }

  return applyOverrides(action, config.languages[language.id])
}

export function buildPaletteActions(languages: Array<{ id: string; label: string }>, texts: UiTexts, config: PaletteConfig = emptyPaletteConfig): PaletteAction[] {
  const localized = paletteActions.map((action) => localizeAction(action, texts))
  const ordered = orderPaletteActions(localized, paletteGroups, config)

  if (languages.length === 0) {
    return ordered
  }

  const children = orderLanguages(languages, config).map((language) => languageAction(language, config, texts))

  return ordered.map((action) => (action.opens === 'languages' ? { ...action, children } : action))
}

// Lower case without diacritics, so "naglowek" finds "Nagłówek". "ł" is a letter of its own in Unicode rather
// than an "l" with a combining mark, which is why it needs a replacement of its own.
function foldForSearch(text: string): string {
  return text.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/ł/g, 'l')
}

export function filterActions(actions: PaletteAction[], query: string, texts: UiTexts): PaletteAction[] {
  const needle = foldForSearch(query.trim())

  if (needle.length === 0) {
    return actions.filter((action) => action.hidden !== true)
  }

  return flatten(actions).filter((action) => {
    const haystack = foldForSearch([action.label, action.hint, groupLabel(action.group, texts), ...(action.keywords ?? [])].join(' '))

    return haystack.includes(needle)
  })
}

function flatten(actions: PaletteAction[]): PaletteAction[] {
  return actions.flatMap((action) => (action.children === undefined ? [action] : flatten(action.children)))
}

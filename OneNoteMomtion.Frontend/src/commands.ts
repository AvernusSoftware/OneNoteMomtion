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
import { applyOverrides, emptyPaletteConfig, orderLanguages, orderPaletteActions } from './paletteConfig'
import type { PaletteConfig } from './paletteConfig'

export type SubmenuId = 'languages' | 'case'
export type GroupId = 'styles' | 'navigation' | 'code' | 'letterCase' | 'tools'

export interface PaletteGroup {
  id: GroupId
  label: string
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

export const paletteGroups: PaletteGroup[] = [
  { id: 'styles', label: 'Styles' },
  { id: 'navigation', label: 'Navigation' },
  { id: 'code', label: 'Code' },
  { id: 'letterCase', label: 'Letter case' },
  { id: 'tools', label: 'Tools' },
]

export function groupLabel(id: GroupId): string {
  return paletteGroups.find((group) => group.id === id)?.label ?? id
}

export function actionKey(action: PaletteAction): string {
  return action.argument === undefined ? action.id : action.id + ':' + action.argument
}

export const caseActions: PaletteAction[] = [
  {
    id: 'case.upper',
    label: 'UPPER CASE',
    hint: 'Every letter uppercase',
    icon: CaseUpper,
    group: 'letterCase',
    keywords: ['uppercase', 'caps'],
  },
  {
    id: 'case.lower',
    label: 'lower case',
    hint: 'Every letter lowercase',
    icon: CaseLower,
    group: 'letterCase',
    keywords: ['lowercase'],
  },
  {
    id: 'case.capitalize',
    label: 'Capitalize Each Word',
    hint: 'Each word starts with a capital letter',
    icon: CaseSensitive,
    group: 'letterCase',
    keywords: ['words', 'title'],
  },
  {
    id: 'case.sentence',
    label: 'Sentence case',
    hint: 'The sentence starts with a capital letter',
    icon: Type,
    group: 'letterCase',
    keywords: ['sentence'],
  },
  {
    id: 'case.toggle',
    label: 'tOGGLE cASE',
    hint: 'Inverts the case of every letter',
    icon: Shuffle,
    group: 'letterCase',
    keywords: ['invert', 'swap'],
  },
]

export const paletteActions: PaletteAction[] = [
  {
    id: 'style.heading1',
    label: 'Heading 1',
    hint: 'First level heading',
    icon: Heading1,
    group: 'styles',
    keywords: ['h1', 'heading', 'title'],
  },
  {
    id: 'style.heading2',
    label: 'Heading 2',
    hint: 'Second level heading',
    icon: Heading2,
    group: 'styles',
    keywords: ['h2', 'heading'],
  },
  {
    id: 'style.heading3',
    label: 'Heading 3',
    hint: 'Third level heading',
    icon: Heading3,
    group: 'styles',
    keywords: ['h3', 'heading'],
  },
  {
    id: 'style.normal',
    label: 'Normal text',
    hint: 'Plain body text',
    icon: Pilcrow,
    group: 'styles',
    keywords: ['plain', 'paragraph', 'reset'],
  },
  {
    id: 'nav.reveal',
    label: 'Show in notebook tree',
    hint: "Expands and selects this page in OneNote's navigation pane",
    icon: FolderTree,
    group: 'navigation',
    keywords: ['navigation', 'notebook', 'section', 'where', 'locate', 'tree', 'pane', 'reveal'],
  },
  {
    id: 'nav.collapseAll',
    label: 'Collapse the notebook tree',
    hint: "Folds every notebook, section group and section in OneNote's navigation pane",
    icon: FolderMinus,
    group: 'navigation',
    keywords: ['navigation', 'notebook', 'section', 'collapse', 'fold', 'close', 'tree', 'pane'],
  },
  {
    id: 'nav.dump',
    label: 'Dump the navigation tree (diagnostics)',
    hint: 'Writes the UI Automation tree of the OneNote window to a file',
    icon: Bug,
    group: 'navigation',
    keywords: ['diagnostics', 'dump', 'uia', 'automation', 'debug'],
    hidden: true,
  },
  {
    id: 'code.format',
    label: 'Format code block',
    hint: 'Highlights the syntax of the selected code',
    icon: Braces,
    group: 'code',
    keywords: ['block', 'syntax', 'colour', 'highlight'],
    opens: 'languages',
  },
  {
    id: 'case.menu',
    label: 'Letter case',
    hint: 'UPPER, lower, Capitalize, Sentence, tOGGLE',
    icon: CaseSensitive,
    group: 'letterCase',
    keywords: ['uppercase', 'lowercase', 'caps', 'case'],
    opens: 'case',
    children: caseActions,
  },
  {
    id: 'page.reformat',
    label: 'Reformat page',
    hint: 'Applies the configured styles to the whole page and tidies it up',
    icon: Wand2,
    group: 'tools',
    keywords: ['reformat', 'format', 'cleanup', 'tidy', 'whitespace', 'page', 'sprzatanie'],
  },
  {
    id: 'spell.disable',
    label: 'Disable spellcheck',
    hint: 'Removes the red underlines from the selection',
    icon: SpellCheck,
    group: 'tools',
    keywords: ['spelling', 'underlines'],
  },
  {
    id: 'spell.enable',
    label: 'Enable spellcheck',
    hint: 'Restores spell checking',
    icon: SpellCheck,
    group: 'tools',
    keywords: ['spelling'],
  },
]

function languageAction(language: { id: string; label: string }, config: PaletteConfig): PaletteAction {
  const action: PaletteAction = {
    id: 'code.format',
    argument: language.id,
    label: language.label,
    hint: 'Highlights the selected code as ' + language.label,
    icon: Braces,
    group: 'code',
    keywords: [language.id, 'block', 'syntax', 'colour', 'highlight'],
  }

  return applyOverrides(action, config.languages[language.id])
}

export function buildPaletteActions(
  languages: Array<{ id: string; label: string }>,
  config: PaletteConfig = emptyPaletteConfig,
): PaletteAction[] {
  const ordered = orderPaletteActions(paletteActions, paletteGroups, config)

  if (languages.length === 0) {
    return ordered
  }

  const children = orderLanguages(languages, config).map((language) => languageAction(language, config))

  return ordered.map((action) => (action.opens === 'languages' ? { ...action, children } : action))
}

export function filterActions(actions: PaletteAction[], query: string): PaletteAction[] {
  const needle = query.trim().toLowerCase()

  if (needle.length === 0) {
    return actions.filter((action) => action.hidden !== true)
  }

  return flatten(actions).filter((action) => {
    const haystack = [action.label, action.hint, groupLabel(action.group), ...(action.keywords ?? [])]
      .join(' ')
      .toLowerCase()

    return haystack.includes(needle)
  })
}

function flatten(actions: PaletteAction[]): PaletteAction[] {
  return actions.flatMap((action) => (action.children === undefined ? [action] : flatten(action.children)))
}

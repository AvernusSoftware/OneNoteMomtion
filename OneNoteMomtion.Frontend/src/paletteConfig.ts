import type { GroupId, PaletteAction, PaletteGroup } from './commands'

export interface PaletteEntryConfig {
  order?: number
  enabled?: boolean
  label?: string
  hint?: string
  keywords?: string[]
}

export type PaletteEntryMap = Record<string, PaletteEntryConfig>

export interface PaletteConfig {
  groups: PaletteEntryMap
  actions: PaletteEntryMap
  case: PaletteEntryMap
  languages: PaletteEntryMap
}

export const emptyPaletteConfig: PaletteConfig = { groups: {}, actions: {}, case: {}, languages: {} }

export function isEnabled(entry: PaletteEntryConfig | undefined): boolean {
  return entry?.enabled !== false
}

function orderOf(entry: PaletteEntryConfig | undefined): number {
  return typeof entry?.order === 'number' ? entry.order : Number.MAX_SAFE_INTEGER
}

function byOrder<T>(items: T[], entryOf: (item: T) => PaletteEntryConfig | undefined): T[] {
  return [...items].sort((left, right) => orderOf(entryOf(left)) - orderOf(entryOf(right)))
}

export function applyOverrides(action: PaletteAction, entry: PaletteEntryConfig | undefined): PaletteAction {
  if (entry === undefined) {
    return action
  }

  const label = entry.label ?? action.label
  const hint = entry.hint ?? action.hint
  const keywords = entry.keywords === undefined ? action.keywords : [...(action.keywords ?? []), ...entry.keywords]

  if (label === action.label && hint === action.hint && keywords === action.keywords) {
    return action
  }

  return { ...action, label, hint, keywords }
}

export function orderPaletteActions(actions: PaletteAction[], groups: PaletteGroup[], config: PaletteConfig): PaletteAction[] {
  const enabledGroups: GroupId[] = byOrder(groups, (group) => config.groups[group.id])
    .filter((group) => isEnabled(config.groups[group.id]))
    .map((group) => group.id)

  return enabledGroups.flatMap((groupId) => {
    const inGroup = actions.filter((action) => action.group === groupId && isEnabled(config.actions[action.id]))

    return byOrder(inGroup, (action) => config.actions[action.id]).map((action) =>
      applyOverrides(action, config.actions[action.id]),
    )
  })
}

export function orderCaseActions(actions: PaletteAction[], config: PaletteConfig): PaletteAction[] {
  const enabled = actions.filter((action) => isEnabled(config.case[action.id]))

  return byOrder(enabled, (action) => config.case[action.id]).map((action) =>
    applyOverrides(action, config.case[action.id]),
  )
}

export function orderLanguages<T extends { id: string }>(languages: T[], config: PaletteConfig): T[] {
  const enabled = languages.filter((language) => isEnabled(config.languages[language.id]))

  return byOrder(enabled, (language) => config.languages[language.id])
}

function readEntryMap(source: unknown): PaletteEntryMap {
  if (typeof source !== 'object' || source === null || Array.isArray(source)) {
    return {}
  }

  const result: PaletteEntryMap = {}

  for (const [key, value] of Object.entries(source as Record<string, unknown>)) {
    if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      result[key] = value as PaletteEntryConfig
    }
  }

  return result
}

export function readPaletteConfig(data: unknown): PaletteConfig {
  if (typeof data !== 'object' || data === null) {
    return emptyPaletteConfig
  }

  const source = data as Record<string, unknown>

  return {
    groups: readEntryMap(source.groups),
    actions: readEntryMap(source.actions),
    case: readEntryMap(source.case),
    languages: readEntryMap(source.languages),
  }
}

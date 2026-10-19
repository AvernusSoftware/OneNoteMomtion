import { describe, expect, it } from 'vitest'
import polish from '../../OneNoteMomtion.Core/Localization/pl.json'
import { buildPaletteActions, caseActions, filterActions, paletteActions } from './commands'
import type { PaletteAction, PaletteActionDefinition } from './commands'
import { englishTexts, readUiTexts } from './i18n/texts'
import { emptyPaletteConfig } from './paletteConfig'

const polishTexts = readUiTexts({ language: 'pl', texts: polish })
const languages = [
  { id: 'csharp', label: 'C#' },
  { id: 'plain', label: 'Zwykły tekst' },
]

function labels(actions: PaletteAction[]): string[] {
  return actions.map((action) => action.label)
}

function allDefinitions(): PaletteActionDefinition[] {
  return [...paletteActions, ...caseActions]
}

describe('palette texts', () => {
  it.each(allDefinitions().map((action) => action.id))('has a label and a hint for %s in every language', (id) => {
    for (const texts of [englishTexts, polishTexts]) {
      expect(texts.texts[`palette.action.${id}.label`]).toBeTruthy()
      expect(texts.texts[`palette.action.${id}.hint`]).toBeTruthy()
    }
  })
})

describe('buildPaletteActions', () => {
  it('takes labels and hints from the texts of the interface language', () => {
    const actions = buildPaletteActions(languages, polishTexts)

    expect(labels(actions)).toContain('Nagłówek 1')
    expect(actions.find((action) => action.id === 'page.reformat')?.hint).toBe('Stosuje skonfigurowane style do całej strony i porządkuje ją')
  })

  it('describes each code block language in the interface language', () => {
    const format = buildPaletteActions(languages, polishTexts).find((action) => action.opens === 'languages')

    expect(format?.children?.[0].hint).toBe('Podświetla zaznaczony kod jako C#')
  })

  it('lets a label set in styles.json win over the translation', () => {
    const config = { ...emptyPaletteConfig, actions: { 'style.heading1': { label: 'Tytuł' } } }

    expect(labels(buildPaletteActions(languages, polishTexts, config))).toContain('Tytuł')
  })
})

describe('filterActions', () => {
  const actions = buildPaletteActions(languages, polishTexts)

  it('matches Polish text typed without diacritics', () => {
    expect(labels(filterActions(actions, 'naglowek', polishTexts))).toEqual(['Nagłówek 1', 'Nagłówek 2', 'Nagłówek 3'])
    expect(labels(filterActions(actions, 'zwin drzewo', polishTexts))).toEqual(['Zwiń drzewo notesów'])
  })

  it('matches Polish text typed with diacritics in any letter case', () => {
    expect(labels(filterActions(actions, 'NAGŁÓWEK 2', polishTexts))).toEqual(['Nagłówek 2'])
  })

  it('keeps the English keywords searchable in Polish', () => {
    expect(labels(filterActions(actions, 'h1', polishTexts))).toEqual(['Nagłówek 1'])
  })

  it('finds entries of a submenu, such as the letter case actions', () => {
    expect(labels(filterActions(actions, 'jak w zdaniu', polishTexts))).toEqual(['Jak w zdaniu'])
  })
})

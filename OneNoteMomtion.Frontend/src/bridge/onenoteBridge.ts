import { currentTexts, englishTexts, readUiTexts, t } from '../i18n/texts'
import type { UiTexts } from '../i18n/texts'
import { readPaletteConfig } from '../paletteConfig'
import type { PaletteConfig } from '../paletteConfig'

export interface CommandResponse {
  requestId?: string
  success: boolean
  changed: boolean
  message?: string | null
  data?: unknown
}

interface HostMessage {
  type?: string
  view?: string
  requestId?: string
  success?: boolean
  changed?: boolean
  message?: string | null
  data?: unknown
}

interface WebViewHost {
  postMessage(message: unknown): void
  addEventListener(type: 'message', listener: (event: { data: HostMessage }) => void): void
  removeEventListener(type: 'message', listener: (event: { data: HostMessage }) => void): void
}

declare global {
  interface Window {
    chrome?: { webview?: WebViewHost }
  }
}

const host = window.chrome?.webview

export const isHosted = host !== undefined

const REQUEST_TIMEOUT_MS = 15_000

type Pending = {
  resolve: (response: CommandResponse) => void
  reject: (error: Error) => void
  timer: number
}

const pending = new Map<string, Pending>()
const viewListeners = new Set<(view: string) => void>()

let nextRequestId = 0

if (host) {
  host.addEventListener('message', (event) => {
    const message = event.data

    if (message?.type === 'view' && message.view) {
      viewListeners.forEach((listener) => listener(message.view as string))
      return
    }

    if (!message?.requestId) {
      return
    }

    const waiting = pending.get(message.requestId)
    if (!waiting) {
      return
    }

    pending.delete(message.requestId)
    window.clearTimeout(waiting.timer)
    waiting.resolve({
      requestId: message.requestId,
      success: message.success ?? false,
      changed: message.changed ?? false,
      message: message.message ?? null,
      data: message.data,
    })
  })
}

export function sendCommand(command: string, argument?: string): Promise<CommandResponse> {
  if (!host) {
    return mockCommand(command, argument)
  }

  const requestId = String(++nextRequestId)

  return new Promise<CommandResponse>((resolve, reject) => {
    const timer = window.setTimeout(() => {
      pending.delete(requestId)
      reject(new Error(t(currentTexts(), 'palette.engineTimeout', { command })))
    }, REQUEST_TIMEOUT_MS)

    pending.set(requestId, { resolve, reject, timer })
    host.postMessage({ requestId, command, argument: argument ?? null })
  })
}

export function onViewChange(listener: (view: string) => void): () => void {
  viewListeners.add(listener)
  return () => viewListeners.delete(listener)
}

export function closePalette(): void {
  void sendCommand('ui.close')
}

export function requestResize(width: number, height: number): void {
  void sendCommand('ui.resize', `${Math.round(width)}x${Math.round(height)}`)
}

export async function fetchLanguages(): Promise<Array<{ id: string; label: string }>> {
  const response = await sendCommand('languages.list')
  const entries = Array.isArray(response.data) ? (response.data as string[]) : []

  return entries
    .map((entry) => {
      const separator = entry.indexOf('=')
      return separator > 0
        ? { id: entry.slice(0, separator), label: entry.slice(separator + 1) }
        : null
    })
    .filter((entry): entry is { id: string; label: string } => entry !== null)
}

export async function fetchPaletteConfig(): Promise<PaletteConfig> {
  const response = await sendCommand('palette.config')
  return readPaletteConfig(response.data)
}

export async function fetchTexts(): Promise<UiTexts> {
  const response = await sendCommand('texts.palette')
  return readUiTexts(response.data)
}

function numbered(...keys: string[]): Record<string, { order: number; enabled: boolean }> {
  return Object.fromEntries(keys.map((key, index) => [key, { order: (index + 1) * 10, enabled: true }]))
}

async function mockCommand(command: string, argument?: string): Promise<CommandResponse> {
  await new Promise((resolve) => setTimeout(resolve, 120))

  if (command === 'languages.list') {
    return {
      success: true,
      changed: false,
      data: ['csharp=C#', 'java=Java', 'groovy=Groovy', 'sql=SQL', 'json=JSON', 'xml=XML'],
    }
  }

  if (command === 'palette.config') {
    return {
      success: true,
      changed: false,
      data: {
        groups: numbered('styles', 'navigation', 'code', 'letterCase', 'tools'),
        actions: {
          ...numbered('style.heading1', 'style.heading2', 'style.heading3', 'style.normal'),
          ...numbered('nav.reveal', 'nav.collapseAll', 'nav.dump'),
          ...numbered('code.format'),
          ...numbered('case.menu'),
          ...numbered('page.reformat', 'spell.disable', 'spell.enable'),
        },
        case: numbered('case.upper', 'case.lower', 'case.capitalize', 'case.sentence', 'case.toggle'),
        languages: numbered('csharp', 'java', 'groovy', 'sql', 'json', 'xml', 'powershell'),
      },
    }
  }

  if (command === 'texts.palette') {
    // Development mode previews another language with ?lang=pl in the address.
    const language = new URLSearchParams(window.location.search).get('lang')
    const data =
      import.meta.env.DEV && language === 'pl'
        ? { language, texts: (await import('../../../OneNoteMomtion.Core/Localization/pl.json')).default }
        : englishTexts

    return { success: true, changed: false, data }
  }

  if (command === 'settings.get') {
    return {
      success: true,
      changed: false,
      data: JSON.stringify(
        {
          version: 1,
          styles: {
            Heading1: { displayName: 'Heading 1', font: 'Calibri Light', fontSize: 20, bold: true },
          },
          autoMarkdownTyping: true,
        },
        null,
        2,
      ),
    }
  }

  return {
    success: true,
    changed: !command.startsWith('ui.'),
    message: t(currentTexts(), 'palette.devMessage', { command: `${command}${argument ? ` (${argument})` : ''}` }),
  }
}

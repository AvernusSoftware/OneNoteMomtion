# OneNoteMomtion — Installation Guide

Editing tools for the **desktop** version of Microsoft OneNote on Windows: headings that
appear as you type `# `, configurable paragraph styles, syntax-highlighted code blocks, text-case
transformations and a spell-check toggle — reachable from a ribbon tab or a floating command
palette bound to `Ctrl+Space`.

---

## Requirements

- **OneNote desktop** (2016 or Microsoft 365)
- **Windows 10 1809+ / Windows 11**
- **.NET Framework 4.8**
- **.NET 10 Desktop Runtime** (x64)
- **Microsoft Edge WebView2 Runtime**
- **.NET 10 SDK** - Only to build from source.
- **Node.js 20+** - Only to build the frontend, via the bundled `npm`.

---

## Install

**From a release package:** everything in it is already built. Close OneNote and run `install.ps1`
from the unpacked folder — it only copies and registers, so neither the .NET SDK nor Node.js is
needed, just the .NET 10 Desktop Runtime and the WebView2 Runtime. The rest of this section covers
installing from source.

```powershell
git clone https://github.com/AvernusSoftware/OneNoteMomtion.git
cd OneNoteMomtion\install

# Close OneNote first — its process locks the add-in DLL.
.\install.ps1
```

The script builds the frontend, the engine and the COM shim, copies everything to
`%LOCALAPPDATA%\OneNoteMomtion\bin`, and registers the add-in.

It also merges the **OneNoteMomtion** ribbon tab into
`%LOCALAPPDATA%\Microsoft\Office\OneNote.officeSL`, keeping whatever else is already in there and
saving the first version it sees as `OneNote.officeSL.momtion-backup`. Pass `-SkipRibbonTab` to
install without touching that file — every command stays reachable from the `Ctrl+Space` palette.

Every registry key goes under `HKEY_CURRENT_USER` — this is a per-user install, no elevation
needed for the add-in itself. The one exception is the *trusted protocol* entry that stops Office
from warning on every click of the **Copy** button in a code block; the script asks for it with a
single UAC prompt near the end. Decline it and everything still installs and works — see
*Troubleshooting* below for adding it manually later.

---

## Updating

```powershell
git pull
cd install
.\install.ps1     # OneNote must be closed
```

---

## Uninstall

```powershell
cd install
.\uninstall.ps1                    # keeps styles.json and logs
.\uninstall.ps1 -RemoveUserData    # removes them too
```

---

## Usage

Everything operates on the **current selection**. With no selection and no cursor in a paragraph,
commands report *"nothing to format"* rather than touching the page.

| Tool | Description |
|---|---|
| **Heading 1 / 2 / 3** | Turns the current paragraph into a heading. Type `# `, `## ` or `### ` at the **start of an empty paragraph** and it converts the moment you press space — no ribbon button for this. `Enter` at the end of a heading starts the next paragraph as `Normal text` instead of a second heading. Also reachable from the command palette (`Ctrl+Space`) → *Heading 1 / 2 / 3*. Detecting the typed prefix needs a low-level keyboard hook, since OneNote's API exposes no typing events; it runs only while OneNote is the foreground window and stores no keystrokes, just whether the previous character was `#` (some antivirus tools flag `SetWindowsHookEx`, this is what it's used for). |
| **Normal text** | Resets the paragraph to plain body text. Command palette (`Ctrl+Space`) → *Normal text*. |
| **Format code block** | Turns the selected paragraphs into a syntax-highlighted block with its own **Copy** button. Ribbon: *OneNoteMomtion* tab → *Code* group → *Language* → pick a language from the dropdown, which formats immediately. Palette: `Ctrl+Space` → *Format code block* → pick a language. |
| **Letter case** | UPPER CASE, lower case, Capitalize Each Word, Sentence case, tOGGLE cASE. Ribbon: *Text Case* group (one button each). Palette: `Ctrl+Space` → *Letter case* submenu. |
| **Reformat page** | Applies the configured styles to every heading and paragraph on the whole page, tidies whitespace and blank paragraphs, and refreshes code blocks. One `Ctrl+Z` undoes all of it. Ribbon: *Tools* group → *Reformat Page*. Palette: `Ctrl+Space` → *Reformat page*. |
| **Disable / Enable spellcheck** | Gives the selection a language with no dictionary to hide the red underlines, or undoes that. Ribbon: *Tools* group. Palette: `Ctrl+Space` → *Disable / Enable spellcheck*. |
| **Show in notebook tree** | Expands and selects the current page in OneNote's navigation pane. Ribbon: *Tools* group. Palette: `Ctrl+Space` → *Show in notebook tree*. |
| **Collapse the notebook tree** | Folds every notebook, section group and section in the navigation pane. Command palette (`Ctrl+Space` → *Collapse the notebook tree*), or add it to the ribbon yourself — see the note below. |
| **Command palette** | Floating window with fuzzy search over every command above (matches label, hint and keywords). Opens near the caret with the hotkey `Ctrl+Space` (configurable), or via the ribbon's *Settings* button (opens straight into the settings view). |
| **Settings / style editor** | Edits styles, the syntax-highlighting theme, palette layout and behaviour switches, stored in `%APPDATA%\OneNoteMomtion\styles.json`. Reachable from the ribbon's *Settings* button (opens the editor inside the floating palette) or by editing the JSON file directly — the add-in reloads it automatically, and a malformed file is never overwritten (it falls back to defaults and logs the parse error). Notable switches: `language` (`auto` follows the Windows display language, or `en` / `pl`; the palette, the ribbon, messages and new code blocks switch over as soon as the file is saved, no reinstall or restart needed), `paletteHotkey` (default `Ctrl+Space`), `autoMarkdownTyping` (toggles the heading-on-typing behaviour), `autoNormalAfterHeading`, and a `palette` section controlling which rows the palette shows, their order and labels — see inline comments in the generated file for the full set. |

The **OneNoteMomtion** ribbon tab is an ordinary ribbon customization, so every command on it can be
added, removed, renamed or moved to another group from *File → Options → Customize Ribbon*. The full
command list — including the ones the tab does not show by default, such as the four paragraph styles
and *Collapse the notebook tree* — lives under **Main Tabs → OneNoteMomtion**; the entry marked
*(Custom)* is the tab you actually see, the other one is the add-in's command catalog it is built
from. Leave the catalog unticked: it never reaches the ribbon, and the icons keep working wherever
you put them. *Reset all customizations* wipes the tab; re-running `install.ps1` puts it back.

---

## Troubleshooting

Everything starts with the shared log:

```
%LOCALAPPDATA%\OneNoteMomtion\logs\addin.log
```

Lines are tagged `[addin]` (the COM shim, running inside `dllhost` — that's normal, not a fault)
or `[engine]` (the .NET 10 engine).

**The OneNoteMomtion tab does not appear**
- The tab lives in `%LOCALAPPDATA%\Microsoft\Office\OneNote.officeSL`, so it is missing if OneNote
  was open while `install.ps1` ran (OneNote rewrites that file from memory when it exits, undoing
  the change), or if *Customize Ribbon → Reset* has since cleared it. Close OneNote completely and
  re-run the installer. If the commands are still reachable through `Ctrl+Space`, the add-in itself
  is loaded fine and only the tab is gone.
- Office may have disabled it after a slow/failed load. Check
  `HKCU:\Software\Microsoft\Office\OneNote\AddIns\OneNoteMomtion.AddIn` — if `LoadBehavior` is `2`,
  re-enable it via *File → Options → Add-ins → COM Add-ins → Go…*.
- Also check *File → Options → Add-ins → Disabled Items*.
- Confirm the COM registration at
  `HKCU:\Software\Classes\CLSID\{7F6CF95B-A3C4-4EFC-A774-9F0A4B741C46}\InprocServer32` points at an
  existing `OneNoteMomtion.AddIn.dll`; if you moved the install folder, re-run
  `install.ps1 -SkipBuild`.
- A 32-bit OneNote cannot load the x64 shim — this fails silently.

**The ribbon works but nothing happens when I click** — look for `[engine]` lines in the log. A
missing `OneNoteMomtion.Server.exe` means re-running `install.ps1`; no `[engine]` lines at all
usually means the .NET 10 **Desktop** Runtime isn't installed (`dotnet --list-runtimes`).

**The palette opens blank or shows "Frontend not built"** — build it:

```powershell
cd OneNoteMomtion.Frontend
npm install
npm run build
```

**Ctrl+Space does nothing** — the hotkey is only registered while OneNote is the foreground
window; another program may be holding it system-wide. The ribbon's *Palette* button always works.

**Typing `# ` does not produce a heading** — check `autoMarkdownTyping` is `true`, make sure the
`#` is the first character of the paragraph, and look for `Failed to install the keyboard hook` in
the log (happens if OneNote runs elevated). If headings convert only sometimes, raise
`typingApplyDelayMs`.

**Copy button warns "This location may be unsafe"** — the trusted-protocol entry is missing
(you declined the UAC prompt during install). Add it from an elevated prompt, replacing `<SID>`
with the output of the first line, then restart OneNote:

```powershell
[System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
reg add "HKEY_USERS\<SID>\Software\Policies\Microsoft\Office\16.0\Common\Security\Trusted Protocols\All Applications\momtion:" /f
```

**Leftover engine process** — it should exit with OneNote, but if one is left behind:

```powershell
Get-Process OneNoteMomtion.Server | Stop-Process
```

---

## Building from source

```powershell
dotnet test  OneNoteMomtion.Core.Tests\OneNoteMomtion.Core.Tests.csproj
dotnet build OneNoteMomtion.slnx -c Release

cd OneNoteMomtion.Frontend
npm install
npm test
npm run build
```

Open `OneNoteMomtion.slnx` in Visual Studio 2026 to work on it.

To cut a release, run `install\pack-release.ps1`: it builds the frontend, publishes the engine and
lays out a self-installing package in `dist\` — zip that folder and ship it.

### Project layout

| Project | Target | Role |
|---|---|---|
| `OneNoteMomtion.Core` | `net10.0` | All the logic: OneNote page XML, text transforms, syntax highlighting. Takes XML in, returns XML out — no COM, fully unit-testable. |
| `OneNoteMomtion.Core.Tests` | `net10.0` | NUnit test suite. |
| `OneNoteMomtion.AddIn` | `net48` | The COM shim loaded into `ONENOTE.EXE`. Ribbon and add-in lifecycle only. |
| `OneNoteMomtion.Server` | `net10.0-windows` | The engine: a separate process hosting WebView2, driving OneNote, watching the keyboard for Markdown prefixes and `Enter`, and dispatching commands. |
| `OneNoteMomtion.Frontend` | Vite + React | The floating palette. |

**Why two runtimes?** Office resolves ribbon callbacks through `IDispatch`, which .NET Core no
longer exposes for managed objects — so the piece Office talks to stays on .NET Framework.
Everything else runs on .NET 10 in its own process, keeping WebView2 off Office's UI thread.

**Adding an interface language:** copy `OneNoteMomtion.Core/Localization/en.json` to a file named after the
two-letter language code (e.g. `de.json`) and translate the values. It is picked up automatically: `language`
accepts the new code and `auto` chooses it on a Windows set to that language. Keys ending in `_one`, `_few`,
`_many` or `_other` are plural forms (CLDR categories); if the language needs other forms than English or
Polish, add its rule to `PluralRules`. The tests in `LocalizationFilesTests` fail until every text, placeholder
and plural form is there.

**Adding a syntax-highlighting language:** implement `ISyntaxHighlightProvider` (usually by
deriving from `RegexTokenizerBase`) and register it in `SyntaxHighlighterRegistry.CreateDefault()`.
The ribbon dropdown and the palette both read that registry automatically.

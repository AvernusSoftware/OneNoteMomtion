<#
.SYNOPSIS
    Installs OneNoteMomtion for the current user, building it first when run from the sources.

.DESCRIPTION
    Runs in one of two modes, decided by what sits next to the script:

      Release package - an unpacked release has a bin\ folder holding the finished build. Nothing
                        is compiled: the folder is copied to InstallPath and registered. Needs the
                        .NET 10 Desktop Runtime, nothing else.
      Repository      - a source checkout builds the frontend, the engine and the COM shim first.
                        Needs the .NET 10 SDK, Node.js and the interop assemblies the shim
                        references.

    Does not need administrator rights. Every registry key goes under HKEY_CURRENT_USER,
    including the COM class registration - the same entries `regasm /codebase` would write
    to HKEY_CLASSES_ROOT, only per user. That is also why regasm is not used here: it writes
    to HKLM, would demand elevation, and would still not write the AppID that OneNote needs.

    This script is the only source of truth for the registration. OneNote hosts add-ins in the
    dllhost.exe COM surrogate, which takes an AppID entry regasm knows nothing about, so a
    registration built out of regasm alone loads in no version of OneNote.

.PARAMETER InstallPath
    Where to copy the build output. Defaults to %LOCALAPPDATA%\OneNoteMomtion\bin.

.PARAMETER SkipBuild
    Register what is already in InstallPath instead of building or copying anything. Useful after
    editing styles.json or when repairing a broken registration.

.PARAMETER SkipFrontend
    Skip the npm install/build step. The palette will show a placeholder page until the
    frontend is built at least once. Ignored when running from a release package, where the
    frontend is already built.

.PARAMETER SkipTrustedProtocol
    Do not prompt for elevation to put the momtion: protocol on Office's trusted list. The
    Copy button inside code blocks still works, but every click costs a security prompt.

.PARAMETER SkipRibbonTab
    Leave OneNote.officeSL alone. The add-in still installs, but no OneNoteMomtion tab appears
    on the ribbon until one is built by hand in Options -> Customize Ribbon.

.EXAMPLE
    .\install.ps1

.EXAMPLE
    .\install.ps1 -SkipFrontend
#>
[CmdletBinding()]
param(
    [string] $InstallPath = (Join-Path $env:LOCALAPPDATA 'OneNoteMomtion\bin'),
    [switch] $SkipBuild,
    [switch] $SkipFrontend,
    [switch] $SkipTrustedProtocol,
    [switch] $SkipRibbonTab
)

$ErrorActionPreference = 'Stop'

# dotnet and MSBuild otherwise localize their console output to the OS UI language.
$env:DOTNET_CLI_UI_LANGUAGE = 'en'

$ClassId = '{7F6CF95B-A3C4-4EFC-A774-9F0A4B741C46}'
$ProgId = 'OneNoteMomtion.AddIn'
$ClassName = 'OneNoteMomtion.AddIn.AddIn'

# Ribbon customizations split across two files, and the difference matters: OneNote.officeUI holds
# the ones built purely from OneNote's own commands, OneNote.officeSL the ones that reference an
# add-in's controls. A tab put in the wrong one is accepted, kept and silently rendered empty.
$OfficeSolutionsPath = Join-Path $env:LOCALAPPDATA 'Microsoft\Office\OneNote.officeSL'
$MsoNamespace = 'http://schemas.microsoft.com/office/2009/07/customui'
$RibbonTabId = 'OneNoteMomtion.Tab'
$CatalogTabId = 'OneNoteMomtionCommands'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$AddInProject = Join-Path $RepoRoot 'OneNoteMomtion.AddIn\OneNoteMomtion.AddIn.csproj'
$ServerProject = Join-Path $RepoRoot 'OneNoteMomtion.Server\OneNoteMomtion.Server.csproj'
$FrontendPath = Join-Path $RepoRoot 'OneNoteMomtion.Frontend'

# A release package is this script sitting next to a bin\ folder with the engine already built in it,
# which is exactly what pack-release.ps1 produces. In a source checkout that folder does not exist.
$PackageBin = Join-Path $PSScriptRoot 'bin'
$IsPackage = Test-Path (Join-Path $PackageBin 'OneNoteMomtion.Server.exe')

function Write-Step([string] $Message) {
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Ok([string] $Message) {
    Write-Host "    $Message" -ForegroundColor Green
}

function Write-Warn([string] $Message) {
    Write-Host "    $Message" -ForegroundColor Yellow
}

<#
.SYNOPSIS
    Runs an external program and fails on a non-zero exit code.
.DESCRIPTION
    Windows PowerShell turns anything a native program writes to stderr into a NativeCommandError,
    which $ErrorActionPreference = 'Stop' then treats as fatal. Both npm and dotnet write ordinary
    progress lines there, so the preference has to be relaxed around the call and the real outcome
    read from the exit code instead.
#>
function Invoke-Native {
    param(
        [Parameter(Mandatory)] [string] $FilePath,
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [string] $What
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'

    try {
        & $FilePath @Arguments 2>&1 | ForEach-Object { Write-Host "    $_" }
    }
    finally {
        $ErrorActionPreference = $previous
    }

    if ($LASTEXITCODE -ne 0) {
        throw "$What failed with exit code $LASTEXITCODE."
    }
}

# --- Prerequisites ----------------------------------------------------------

Write-Step 'Checking prerequisites'

if ($IsPackage) {
    # Deliberately not `dotnet --list-runtimes`: a runtime-only installation does not have to put
    # dotnet on PATH, and the engine is a WinExe - launched without its runtime it dies before the
    # log is opened, leaving a ribbon whose every button does nothing.
    $desktopRuntimes = Get-ChildItem (Join-Path $env:ProgramFiles 'dotnet\shared\Microsoft.WindowsDesktop.App') -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like '10.*' }

    if (-not $desktopRuntimes) {
        throw 'The .NET 10 Desktop Runtime (x64) was not found. Get it from https://dotnet.microsoft.com/download/dotnet/10.0 and run this script again.'
    }

    Write-Ok ".NET Desktop Runtime: $(($desktopRuntimes | Sort-Object Name | Select-Object -Last 1).Name)"
}
elseif (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK was not found on PATH. Install .NET 10 SDK and try again.'
}

$oneNote = @(
    "$env:ProgramFiles\Microsoft Office\root\Office16\ONENOTE.EXE",
    "${env:ProgramFiles(x86)}\Microsoft Office\root\Office16\ONENOTE.EXE"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $oneNote) {
    throw 'Desktop OneNote was not found. This add-in does not work with the Store version ("OneNote for Windows 10").'
}

Write-Ok "OneNote: $oneNote"

$webView2 = (Get-ItemProperty 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}' -ErrorAction SilentlyContinue).pv
if ($webView2) {
    Write-Ok "WebView2 Runtime: $webView2"
}
else {
    Write-Warn 'WebView2 Runtime was not detected. The ribbon will work but the floating palette will not open.'
    Write-Warn 'Get it from https://developer.microsoft.com/microsoft-edge/webview2/'
}

# OneNote holds the add-in DLL open for as long as it runs, so the file cannot be overwritten
# until it is gone. ONENOTEM.EXE, the notification-area helper, outlives the main window and
# keeps OneNote alive with it.
if (Get-Process ONENOTE -ErrorAction SilentlyContinue) {
    throw 'OneNote is running. Close it completely and run this script again.'
}

Get-Process ONENOTEM -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Warn "stopping the OneNote helper process $($_.Id)"
    Stop-Process -Id $_.Id -Force
}

Get-CimInstance Win32_Process -Filter "Name = 'dllhost.exe'" |
    Where-Object { $_.CommandLine -and $_.CommandLine.Contains($ClassId) } |
    ForEach-Object {
        Write-Warn "stopping leftover COM surrogate $($_.ProcessId)"
        Stop-Process -Id $_.ProcessId -Force
    }

# --- Build ------------------------------------------------------------------

if ($IsPackage -and -not $SkipBuild) {
    Write-Step 'Copying the release build'

    if ($SkipFrontend) {
        Write-Warn '-SkipFrontend ignored: a release package ships the frontend already built.'
    }

    # Copy-Item with a wildcard source drops an existing subfolder inside itself (wwwroot\wwwroot),
    # and files left by an older release would linger anyway, so the folder starts out empty. Only
    # build output lives here: styles.json, the logs and the WebView2 profile sit outside it.
    if (Test-Path $InstallPath) {
        Remove-Item (Join-Path $InstallPath '*') -Recurse -Force
    }
    else {
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    }

    Copy-Item (Join-Path $PackageBin '*') -Destination $InstallPath -Recurse -Force
    Write-Ok "Copied to $InstallPath"
}
elseif (-not $SkipBuild) {
    if (-not $SkipFrontend) {
        Write-Step 'Building the React frontend'

        $npm = Join-Path (Split-Path -Parent (Get-Command node).Source) 'npm.cmd'
        if (-not (Test-Path $npm)) {
            throw "npm was not found next to node.exe. Install Node.js 20 or newer."
        }

        Push-Location $FrontendPath
        try {
            Invoke-Native -FilePath $npm -Arguments @('install') -What 'npm install'
            Invoke-Native -FilePath $npm -Arguments @('run', 'build') -What 'npm run build'
        }
        finally {
            Pop-Location
        }

        Write-Ok 'Frontend built into OneNoteMomtion.Frontend\dist'
    }

    Write-Step 'Building the engine (.NET 10)'
    Invoke-Native -FilePath 'dotnet' `
        -Arguments @('publish', $ServerProject, '-c', 'Release', '-o', $InstallPath, '--nologo') `
        -What 'Building the engine'

    Write-Step 'Building the COM shim (.NET Framework 4.8)'
    Invoke-Native -FilePath 'dotnet' `
        -Arguments @('build', $AddInProject, '-c', 'Release', '--nologo') `
        -What 'Building the shim'

    # Only Release output counts, and only from the two folders the shim can actually build into:
    # bin\Release with AppendTargetFrameworkToOutputPath disabled, bin\x64\Release when Platform is
    # passed explicitly. Searching bin recursively for the newest match, as this script used to do,
    # silently picks up a Debug build left behind by Visual Studio.
    $addInProjectDir = Split-Path -Parent $AddInProject
    $addInBuild = @(
        Join-Path $addInProjectDir 'bin\x64\Release\OneNoteMomtion.AddIn.dll'
        Join-Path $addInProjectDir 'bin\Release\OneNoteMomtion.AddIn.dll'
    ) | Where-Object { Test-Path $_ } |
        Sort-Object { (Get-Item $_).LastWriteTime } -Descending |
        Select-Object -First 1

    if (-not $addInBuild) {
        throw 'OneNoteMomtion.AddIn.dll was not found in the Release build output.'
    }

    # An incremental build that skipped the shim leaves an assembly older than the sources it was
    # built from, and the add-in then runs code nobody wrote today - the ribbon in particular, whose
    # labels live in an embedded Ribbon.xml, keeps showing the previous names with no error anywhere.
    $newestSource = Get-ChildItem $addInProjectDir -Recurse -File -Include *.cs, *.xml, *.csproj |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($newestSource -and $newestSource.LastWriteTime -gt (Get-Item $addInBuild).LastWriteTime) {
        throw "$($newestSource.Name) is newer than the built OneNoteMomtion.AddIn.dll. The build did not pick the change up - delete OneNoteMomtion.AddIn\bin and obj, then run this script again."
    }

    Copy-Item $addInBuild -Destination $InstallPath -Force
    Write-Ok "Copied to $InstallPath"
}

$addInDll = Join-Path $InstallPath 'OneNoteMomtion.AddIn.dll'
$engineExe = Join-Path $InstallPath 'OneNoteMomtion.Server.exe'

foreach ($required in @($addInDll, $engineExe)) {
    if (-not (Test-Path $required)) {
        throw "Missing $required. Run the script without -SkipBuild."
    }
}

# --- Registration -----------------------------------------------------------

Write-Step 'Registering for the current user'

$assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($addInDll)
$assemblyFullName = $assemblyName.FullName
$assemblyVersion = $assemblyName.Version.ToString()
$codeBase = 'file:///' + ($addInDll -replace '\\', '/')

function Set-RegistryValues([string] $Path, [hashtable] $Values) {
    if (-not (Test-Path $Path)) {
        New-Item -Path $Path -Force | Out-Null
    }

    foreach ($entry in $Values.GetEnumerator()) {
        $kind = if ($entry.Value -is [int]) { 'DWord' } else { 'String' }
        New-ItemProperty -Path $Path -Name $entry.Key -Value $entry.Value -PropertyType $kind -Force | Out-Null
    }
}

$clsidPath = "HKCU:\Software\Classes\CLSID\$ClassId"
$serverValues = @{
    'Class'          = $ClassName
    'Assembly'       = $assemblyFullName
    'RuntimeVersion' = 'v4.0.30319'
    'CodeBase'       = $codeBase
}

# OneNote activates its add-ins out of process, in dllhost.exe, and an AppID whose DllSurrogate is
# the empty string is what makes that possible. Measured, not assumed: with this key removed OneNote
# never creates the object at all - the constructor never reaches the log - while every other host
# (and a direct CoCreateInstance with CLSCTX_INPROC_SERVER) still activates the class in process.
# OneMore registers the same pair of entries, for the same reason.
Set-RegistryValues "HKCU:\Software\Classes\AppID\$ClassId" @{ 'DllSurrogate' = '' }

Set-RegistryValues $clsidPath @{ '(default)' = $ProgId; 'AppID' = $ClassId }
Set-RegistryValues "$clsidPath\InprocServer32" ($serverValues + @{ '(default)' = 'mscoree.dll'; 'ThreadingModel' = 'Both' })
Set-RegistryValues "$clsidPath\InprocServer32\$assemblyVersion" $serverValues
Set-RegistryValues "$clsidPath\ProgId" @{ '(default)' = $ProgId }
Set-RegistryValues "$clsidPath\VersionIndependentProgID" @{ '(default)' = $ProgId }
Set-RegistryValues "$clsidPath\Programmable" @{ '(default)' = '' }

# Deliberately no TypeLib subkey: this add-in ships no type library. Earlier versions pointed it at
# the class id, a GUID that resolves to nothing. Since the add-in is called across a process
# boundary, COM does need type information for our interfaces - but it takes it from the libraries
# that actually declare them (msaddndr and MSO), found through the IID, not through this key.
if (Test-Path "$clsidPath\TypeLib") {
    Remove-Item "$clsidPath\TypeLib" -Recurse -Force
    Write-Warn 'removed the dead TypeLib entry left by an earlier version'
}

Set-RegistryValues "HKCU:\Software\Classes\$ProgId" @{ '(default)' = 'OneNoteMomtion' }
Set-RegistryValues "HKCU:\Software\Classes\$ProgId\CLSID" @{ '(default)' = $ClassId }
Set-RegistryValues "HKCU:\Software\Classes\$ProgId\CurVer" @{ '(default)' = "$ProgId.1" }
Set-RegistryValues "HKCU:\Software\Classes\$ProgId.1" @{ '(default)' = 'OneNoteMomtion' }
Set-RegistryValues "HKCU:\Software\Classes\$ProgId.1\CLSID" @{ '(default)' = $ClassId }

Set-RegistryValues "HKCU:\Software\Microsoft\Office\OneNote\AddIns\$ProgId" @{
    'FriendlyName' = 'OneNoteMomtion'
    'Description'  = 'Notion-style editing tools for OneNote: Markdown shortcuts, styles, syntax highlighting and text case.'
    'LoadBehavior' = 3
}

Write-Ok "COM class $ClassId registered under HKCU, hosted by the dllhost.exe surrogate"
Write-Ok "OneNote add-in key written, LoadBehavior = 3"

# --- The momtion: protocol --------------------------------------------------

# The Copy button inside a code block is a hyperlink in the page itself - OneNote has no way to host
# a real control in its content. Clicking it launches the engine through this protocol handler.
Write-Step 'Registering the momtion: protocol'

Set-RegistryValues 'HKCU:\Software\Classes\momtion' @{
    '(default)'    = 'URL:OneNoteMomtion'
    'URL Protocol' = ''
}

Set-RegistryValues 'HKCU:\Software\Classes\momtion\shell\open\command' @{
    '(default)' = "`"$engineExe`" --uri `"%1`""
}

Write-Ok 'momtion: handled by the engine executable'

function Install-DefaultRibbonTab {
    if ($SkipRibbonTab) {
        Write-Warn 'Skipped, as requested. No OneNoteMomtion tab will appear until you build one by hand.'
        return
    }

    # Group label -> "control id = imageMso", in ribbon order. Styles and nav.collapseAll are left
    # out on purpose: they stay in the catalog tab, for whoever wants them.
    $layout = [ordered] @{
        'Text Case' = @('caseUpper=ChangeCase', 'caseLower=ChangeCase', 'caseCapitalize=ChangeCase', 'caseSentence=ChangeCase', 'caseToggle=ChangeCase')
        'Language'  = @('languageMenu=SetLanguageDialog')
        'Tools'     = @('spellDisable=SpellingAndGrammar', 'spellEnable=SpellingAndGrammar', 'navReveal=FindDialog', 'pageReformat=TableAutoFormat', 'settingsOpen=ApplicationOptionsDialog')
    }

    $directory = Split-Path -Parent $OfficeSolutionsPath
    if (-not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    if (Test-Path $OfficeSolutionsPath) {
        $backup = "$OfficeSolutionsPath.momtion-backup"
        if (-not (Test-Path $backup)) {
            Copy-Item $OfficeSolutionsPath -Destination $backup
            Write-Ok "Kept the previous customizations as $(Split-Path -Leaf $backup)"
        }

        $document = [xml] (Get-Content $OfficeSolutionsPath -Raw)
    }
    else {
        $document = [xml] "<mso:customUI xmlns:mso=`"$MsoNamespace`"><mso:ribbon><mso:qat/></mso:ribbon></mso:customUI>"
    }

    $namespaces = New-Object System.Xml.XmlNamespaceManager($document.NameTable)
    $namespaces.AddNamespace('mso', $MsoNamespace)

    # SetAttribute refuses to write a namespace declaration, hence building it by hand. Appending it
    # again on a reinstall replaces the old one rather than duplicating it.
    $root = $document.DocumentElement
    $declaration = $document.CreateAttribute('xmlns', 'onm', 'http://www.w3.org/2000/xmlns/')
    $declaration.Value = $ProgId
    $root.Attributes.Append($declaration) | Out-Null

    $ribbon = $root.SelectSingleNode('mso:ribbon', $namespaces)
    if (-not $ribbon) {
        $ribbon = $root.AppendChild($document.CreateElement('mso', 'ribbon', $MsoNamespace))
    }

    $tabs = $ribbon.SelectSingleNode('mso:tabs', $namespaces)
    if (-not $tabs) {
        $tabs = $ribbon.AppendChild($document.CreateElement('mso', 'tabs', $MsoNamespace))
    }

    # Replace rather than add, so reinstalling does not stack up copies. Match the catalog on the
    # local part of its idQ - the prefix is whatever Office last rewrote the file with.
    $stale = @($tabs.SelectNodes("mso:tab[@id='$RibbonTabId']", $namespaces)) +
             @($tabs.SelectNodes("mso:tab[substring-after(@idQ, ':') = '$CatalogTabId']", $namespaces))

    foreach ($previous in $stale) {
        $tabs.RemoveChild($previous) | Out-Null
    }

    # The catalog has to be declared visible in Ribbon.xml or Office never wires up its controls'
    # callbacks, so this entry is the only thing keeping it off the ribbon.
    $catalog = $document.CreateElement('mso', 'tab', $MsoNamespace)
    $catalog.SetAttribute('idQ', "onm:$CatalogTabId")
    $catalog.SetAttribute('visible', 'false')
    $tabs.AppendChild($catalog) | Out-Null

    $tab = $document.CreateElement('mso', 'tab', $MsoNamespace)
    $tab.SetAttribute('id', $RibbonTabId)
    $tab.SetAttribute('label', 'OneNoteMomtion')
    $tab.SetAttribute('insertAfterQ', "onm:$CatalogTabId")

    $groupNumber = 0
    foreach ($groupLabel in $layout.Keys) {
        $groupNumber++

        $group = $document.CreateElement('mso', 'group', $MsoNamespace)
        $group.SetAttribute('id', "$RibbonTabId.Group$groupNumber")
        $group.SetAttribute('label', $groupLabel)
        $group.SetAttribute('autoScale', 'true')

        foreach ($entry in $layout[$groupLabel]) {
            $controlId, $imageMso = $entry -split '=', 2

            $control = $document.CreateElement('mso', 'control', $MsoNamespace)
            $control.SetAttribute('idQ', "onm:$controlId")
            $control.SetAttribute('imageMso', $imageMso)
            $control.SetAttribute('visible', 'true')
            $group.AppendChild($control) | Out-Null
        }

        $tab.AppendChild($group) | Out-Null
    }

    $tabs.AppendChild($tab) | Out-Null

    # No XML declaration and no byte order mark, as Office writes it - a file Office cannot parse is
    # discarded silently.
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.OmitXmlDeclaration = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)

    $writer = [System.Xml.XmlWriter]::Create($OfficeSolutionsPath, $settings)
    try {
        $document.Save($writer)
    }
    finally {
        $writer.Dispose()
    }

    Write-Ok "OneNoteMomtion tab written to $(Split-Path -Leaf $OfficeSolutionsPath)"
}

Write-Step 'Adding the OneNoteMomtion tab to the ribbon'
Install-DefaultRibbonTab

<#
.SYNOPSIS
    Puts momtion: on Office's list of trusted protocols.
.DESCRIPTION
    Without this key Office shows "This location may be unsafe" on every click of the Copy button.
    Measured, not assumed: Office reads the list only from the Policies branch. The twin key under
    HKCU\Software\Microsoft\Office\16.0\... writes fine and is ignored outright.

    That branch has restricted ACLs, so this is the one step of the installation that needs
    elevation. The write goes through HKEY_USERS\<SID> rather than HKCU on purpose: if UAC is
    satisfied by a different administrator account, HKCU in the elevated process is that account's
    hive, and the key silently lands where Office will never look for it.
#>
function Register-TrustedProtocol {
    $sid = ([System.Security.Principal.WindowsIdentity]::GetCurrent()).User.Value
    $key = "HKEY_USERS\$sid\Software\Policies\Microsoft\Office\16.0\Common\Security\Trusted Protocols\All Applications\momtion:"

    if (Test-Path "Registry::$key") {
        Write-Ok 'momtion: is already on the Office trusted protocol list'
        return
    }

    if ($SkipTrustedProtocol) {
        Write-Warn 'Skipped, as requested. Clicking Copy in a code block will show an Office security prompt.'
        return
    }

    Write-Host '    Office needs one elevated registry write so that clicking Copy does not warn every time.'
    Write-Host '    A UAC prompt is about to appear. Decline it and everything else still works.'

    try {
        Start-Process -FilePath 'reg.exe' `
            -ArgumentList @('add', "`"$key`"", '/f') `
            -Verb RunAs -Wait -WindowStyle Hidden
    }
    catch {
        # The most common case by far is the user dismissing the UAC prompt.
        Write-Warn "Elevation did not go through: $($_.Exception.Message)"
    }

    if (Test-Path "Registry::$key") {
        Write-Ok 'momtion: added to the Office trusted protocol list'
    }
    else {
        Write-Warn 'Not added. The Copy button works, but Office will ask for confirmation on every click.'
        Write-Warn 'To do it later, run this from an elevated prompt:'
        Write-Warn "    reg add `"$key`" /f"
    }
}

Register-TrustedProtocol

Write-Step 'Done'
Write-Host "    Installed to : $InstallPath"
Write-Host "    Settings     : $(Join-Path $env:APPDATA 'OneNoteMomtion\styles.json')"
Write-Host "    Log file     : $(Join-Path $env:LOCALAPPDATA 'OneNoteMomtion\logs\addin.log')"
Write-Host ''
Write-Host '    Start OneNote and look for the OneNoteMomtion tab on the ribbon.'
Write-Host '    If it is missing, check the log file and the troubleshooting section of README.md.'

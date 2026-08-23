<#
.SYNOPSIS
    Removes the OneNoteMomtion registration for the current user.

.DESCRIPTION
    Mirrors install.ps1 and needs no administrator rights. Settings and logs are kept unless
    -RemoveUserData is given, so reinstalling brings your styles back.

.PARAMETER InstallPath
    Folder the add-in was installed to. Defaults to %LOCALAPPDATA%\OneNoteMomtion\bin.

.PARAMETER KeepFiles
    Unregister but leave the installed files in place.

.PARAMETER RemoveUserData
    Also delete styles.json, the log files and the WebView2 profile.

.EXAMPLE
    .\uninstall.ps1
#>
[CmdletBinding()]
param(
    [string] $InstallPath = (Join-Path $env:LOCALAPPDATA 'OneNoteMomtion\bin'),
    [switch] $KeepFiles,
    [switch] $RemoveUserData
)

$ErrorActionPreference = 'Stop'

$ClassId = '{7F6CF95B-A3C4-4EFC-A774-9F0A4B741C46}'
$ProgId = 'OneNoteMomtion.AddIn'

$OfficeSolutionsPath = Join-Path $env:LOCALAPPDATA 'Microsoft\Office\OneNote.officeSL'
$MsoNamespace = 'http://schemas.microsoft.com/office/2009/07/customui'
$RibbonTabId = 'OneNoteMomtion.Tab'
$CatalogTabId = 'OneNoteMomtionCommands'

function Write-Step([string] $Message) {
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Remove-KeyIfPresent([string] $Path) {
    if (Test-Path $Path) {
        Remove-Item -Path $Path -Recurse -Force
        Write-Host "    removed $Path" -ForegroundColor Green
    }
}

if (Get-Process ONENOTE -ErrorAction SilentlyContinue) {
    throw 'OneNote is running. Close it completely and run this script again.'
}

# The add-in runs inside a COM surrogate, which outlives OneNote and keeps a handle on the installed
# DLL. Its command line is  DllHost.exe /Processid:{ClassId}  which is how we tell ours apart from
# the many other dllhost instances.
Get-CimInstance Win32_Process -Filter "Name = 'dllhost.exe'" |
    Where-Object { $_.CommandLine -and $_.CommandLine.Contains($ClassId) } |
    ForEach-Object {
        Write-Host "    stopping leftover COM surrogate $($_.ProcessId)" -ForegroundColor Yellow
        Stop-Process -Id $_.ProcessId -Force
    }

# The engine normally exits with OneNote, but a crash can leave it behind holding the Ctrl+Space hotkey.
Get-Process 'OneNoteMomtion.Server' -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "    stopping leftover engine process $($_.Id)" -ForegroundColor Yellow
    Stop-Process -Id $_.Id -Force
}

function Remove-RibbonTab {
    if (-not (Test-Path $OfficeSolutionsPath)) {
        return
    }

    $document = [xml] (Get-Content $OfficeSolutionsPath -Raw)

    $namespaces = New-Object System.Xml.XmlNamespaceManager($document.NameTable)
    $namespaces.AddNamespace('mso', $MsoNamespace)

    # Match the catalog on the local part of its idQ - the prefix is whatever Office last rewrote
    # the file with.
    $ours = @($document.SelectNodes("//mso:tab[@id='$RibbonTabId']", $namespaces)) +
            @($document.SelectNodes("//mso:tab[substring-after(@idQ, ':') = '$CatalogTabId']", $namespaces))

    if ($ours.Count -eq 0) {
        return
    }

    foreach ($tab in $ours) {
        $tab.ParentNode.RemoveChild($tab) | Out-Null
    }

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

    Write-Host "    removed the OneNoteMomtion tab from $(Split-Path -Leaf $OfficeSolutionsPath)" -ForegroundColor Green
}

Write-Step 'Removing the ribbon tab'
Remove-RibbonTab

Write-Step 'Removing registry entries'
Remove-KeyIfPresent "HKCU:\Software\Microsoft\Office\OneNote\AddIns\$ProgId"
Remove-KeyIfPresent "HKCU:\Software\Classes\CLSID\$ClassId"
Remove-KeyIfPresent "HKCU:\Software\Classes\AppID\$ClassId"
Remove-KeyIfPresent "HKCU:\Software\Classes\$ProgId"
Remove-KeyIfPresent "HKCU:\Software\Classes\$ProgId.1"
Remove-KeyIfPresent 'HKCU:\Software\Classes\momtion'

# The Office trusted-protocol entry is deliberately left alone: it lives under HKCU\Software\Policies,
# which needs elevation to touch, and an orphaned entry for a protocol nobody handles any more does
# nothing. Removing it would turn an uninstall that needs no rights into one that asks for them.
$trustedProtocolKey = "HKEY_USERS\$(([System.Security.Principal.WindowsIdentity]::GetCurrent()).User.Value)\Software\Policies\Microsoft\Office\16.0\Common\Security\Trusted Protocols\All Applications\momtion:"
if (Test-Path "Registry::$trustedProtocolKey") {
    Write-Host '    left the Office trusted protocol entry in place (needs elevation to remove):' -ForegroundColor Yellow
    Write-Host "        reg delete `"$trustedProtocolKey`" /f" -ForegroundColor Yellow
}

if (-not $KeepFiles) {
    Write-Step 'Removing installed files'

    if (Test-Path $InstallPath) {
        Remove-Item -Path $InstallPath -Recurse -Force
        Write-Host "    removed $InstallPath" -ForegroundColor Green
    }
    else {
        Write-Host "    nothing at $InstallPath" -ForegroundColor Yellow
    }
}

if ($RemoveUserData) {
    Write-Step 'Removing settings and logs'

    foreach ($path in @(
            (Join-Path $env:APPDATA 'OneNoteMomtion'),
            (Join-Path $env:LOCALAPPDATA 'OneNoteMomtion')
        )) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force
            Write-Host "    removed $path" -ForegroundColor Green
        }
    }
}
else {
    Write-Step 'Keeping settings and logs'
    Write-Host "    $(Join-Path $env:APPDATA 'OneNoteMomtion')"
    Write-Host '    Pass -RemoveUserData to delete them as well.'
}

Write-Step 'Done'

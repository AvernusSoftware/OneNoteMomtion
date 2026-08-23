<#
.SYNOPSIS
    Builds OneNoteMomtion and lays out a ready-to-run release package in dist\.

.DESCRIPTION
    The result installs on a machine with no SDK, no Node.js and no source tree: install.ps1 ships
    inside the package, notices the bin\ folder next to it and then only copies and registers.
    Zipping the folder is left to whoever cuts the release.

    The engine is published framework-dependent, so the target machine still needs the .NET 10
    Desktop Runtime (x64) - install.ps1 checks for it before touching anything. OneNote desktop and
    the WebView2 Runtime cannot be packaged either way.

.PARAMETER OutputPath
    Where to build the package. Defaults to dist\ in the repository root.

.PARAMETER SkipFrontend
    Skip the npm install/build step and package whatever is already in
    OneNoteMomtion.Frontend\dist. The palette shows a placeholder page if that was never built.

.PARAMETER KeepExisting
    Write into the existing OutputPath instead of emptying it first.

.EXAMPLE
    .\pack-release.ps1
#>
[CmdletBinding()]
param(
    [string] $OutputPath,
    [switch] $SkipFrontend,
    [switch] $KeepExisting
)

$ErrorActionPreference = 'Stop'

# dotnet and MSBuild otherwise localize their console output to the OS UI language.
$env:DOTNET_CLI_UI_LANGUAGE = 'en'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$AddInProject = Join-Path $RepoRoot 'OneNoteMomtion.AddIn\OneNoteMomtion.AddIn.csproj'
$ServerProject = Join-Path $RepoRoot 'OneNoteMomtion.Server\OneNoteMomtion.Server.csproj'
$FrontendPath = Join-Path $RepoRoot 'OneNoteMomtion.Frontend'

if (-not $OutputPath) {
    $OutputPath = Join-Path $RepoRoot 'dist'
}

$PackageBin = Join-Path $OutputPath 'bin'

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

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK was not found on PATH. Install .NET 10 SDK and try again.'
}

Write-Ok "Repository: $RepoRoot"
Write-Ok "Package   : $OutputPath"

# --- Layout -----------------------------------------------------------------

if ($KeepExisting) {
    Write-Warn 'Keeping whatever is already in the package folder.'
}
elseif (Test-Path $OutputPath) {
    Write-Step 'Emptying the package folder'
    Remove-Item (Join-Path $OutputPath '*') -Recurse -Force
    Write-Ok 'Cleared'
}

if (-not (Test-Path $PackageBin)) {
    New-Item -ItemType Directory -Path $PackageBin -Force | Out-Null
}

# --- Build ------------------------------------------------------------------

if (-not $SkipFrontend) {
    Write-Step 'Building the React frontend'

    $npm = Join-Path (Split-Path -Parent (Get-Command node).Source) 'npm.cmd'
    if (-not (Test-Path $npm)) {
        throw 'npm was not found next to node.exe. Install Node.js 20 or newer.'
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

# The frontend reaches the package as wwwroot\, carried by the <Content> item in the engine csproj -
# publish takes project items only, which is why that item exists instead of a post-build copy.
Write-Step 'Publishing the engine (.NET 10, framework-dependent)'
Invoke-Native -FilePath 'dotnet' `
    -Arguments @('publish', $ServerProject, '-c', 'Release', '-o', $PackageBin, '--nologo') `
    -What 'Publishing the engine'

Write-Step 'Building the COM shim (.NET Framework 4.8)'
Invoke-Native -FilePath 'dotnet' `
    -Arguments @('build', $AddInProject, '-c', 'Release', '--nologo') `
    -What 'Building the shim'

# Only Release output counts, and only from the two folders the shim can actually build into:
# bin\Release with AppendTargetFrameworkToOutputPath disabled, bin\x64\Release when Platform is
# passed explicitly. Same lookup as install.ps1, for the same reason: searching bin recursively for
# the newest match silently picks up a Debug build left behind by Visual Studio.
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

# An incremental build that skipped the shim leaves an assembly older than the sources it was built
# from, and the package then ships code nobody wrote today - the ribbon in particular, whose labels
# live in an embedded Ribbon.xml, keeps showing the previous names with no error anywhere.
$newestSource = Get-ChildItem $addInProjectDir -Recurse -File -Include *.cs, *.xml, *.csproj |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($newestSource -and $newestSource.LastWriteTime -gt (Get-Item $addInBuild).LastWriteTime) {
    throw "$($newestSource.Name) is newer than the built OneNoteMomtion.AddIn.dll. The build did not pick the change up - delete OneNoteMomtion.AddIn\bin and obj, then run this script again."
}

Copy-Item $addInBuild -Destination $PackageBin -Force
Write-Ok "Shim copied to $PackageBin"

# --- Package files ----------------------------------------------------------

# OneNoteMomtion.reg is left out on purpose: it is a template for repairing or inspecting the
# registration by hand, and install.ps1 is the supported path.
Write-Step 'Copying the scripts, the licence and the readme'

foreach ($file in @(
        (Join-Path $PSScriptRoot 'install.ps1'),
        (Join-Path $PSScriptRoot 'uninstall.ps1'),
        (Join-Path $RepoRoot 'LICENSE'),
        (Join-Path $RepoRoot 'README.md')
    )) {
    Copy-Item $file -Destination $OutputPath -Force
    Write-Ok (Split-Path -Leaf $file)
}

# --- Summary ----------------------------------------------------------------

$engineExe = Join-Path $PackageBin 'OneNoteMomtion.Server.exe'
$files = Get-ChildItem $OutputPath -Recurse -File
$megabytes = [math]::Round((($files | Measure-Object Length -Sum).Sum / 1MB), 1)

Write-Step 'Done'
Write-Host "    Package : $OutputPath"
Write-Host "    Version : $((Get-Item $engineExe).VersionInfo.ProductVersion)"
Write-Host "    Content : $($files.Count) files, $megabytes MB"
Write-Host ''
Write-Host '    Zip the folder to publish it. To install it, run install.ps1 from the unpacked copy'
Write-Host '    with OneNote closed - it will not build anything.'

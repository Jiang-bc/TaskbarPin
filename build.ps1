#requires -Version 5.1
<#
.SYNOPSIS
  Reproducible TaskbarPin build using the .NET Framework C# compiler already on Windows.
.DESCRIPTION
  1. Compiles tools\IconAssetBuilder.cs with src\TaskbarPin.cs and regenerates assets\TaskbarPin.ico/.png.
  2. Compiles src\TaskbarPin.cs with /win32icon, so Explorer, shortcuts and Alt-Tab show the app icon.
  Output defaults to bin\TaskbarPin.exe. No SDK, NuGet package or network access is used.
#>
[CmdletBinding()]
param(
    [string]$Output,
    [switch]$SkipAssets
)
$ErrorActionPreference = 'Stop'
$Root = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if (-not $Output) { $Output = Join-Path $Root 'bin\TaskbarPin.exe' }

function Find-Csc {
    $candidates = @(
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'))
    foreach ($candidate in $candidates) { if (Test-Path -LiteralPath $candidate) { return $candidate } }
    throw 'The .NET Framework 4 csc.exe was not found.'
}

$csc = Find-Csc
$source = Join-Path $Root 'src\TaskbarPin.cs'
$builder = Join-Path $Root 'tools\IconAssetBuilder.cs'
$assets = Join-Path $Root 'assets'
$ico = Join-Path $assets 'TaskbarPin.ico'
$png = Join-Path $assets 'TaskbarPin.png'
$Output = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force -Path $assets, (Split-Path -Parent $Output) | Out-Null

$refs = @('/r:System.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll')
if (-not $SkipAssets) {
    $tool = Join-Path ([IO.Path]::GetTempPath()) ("TaskbarPinIconBuilder-" + [Guid]::NewGuid().ToString('N') + '.exe')
    try {
        & $csc /nologo /optimize+ /target:exe /main:IconAssetBuilder "/out:$tool" @refs $builder $source
        if ($LASTEXITCODE -ne 0) { throw "Icon generator build failed ($LASTEXITCODE)." }
        & $tool $ico $png
        if ($LASTEXITCODE -ne 0) { throw "Icon generation failed ($LASTEXITCODE)." }
    }
    finally { Remove-Item -LiteralPath $tool -Force -ErrorAction SilentlyContinue }
}
if (-not (Test-Path -LiteralPath $ico)) { throw "Missing $ico; run without -SkipAssets." }

& $csc /nologo /optimize+ /target:winexe "/win32icon:$ico" "/out:$Output" @refs $source
if ($LASTEXITCODE -ne 0) { throw "TaskbarPin build failed ($LASTEXITCODE)." }
Write-Host "Built $Output"


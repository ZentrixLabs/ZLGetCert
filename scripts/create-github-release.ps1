param(
	[Parameter(Mandatory=$true)][string]$Tag,
	[Parameter(Mandatory=$true)][string]$Title,
	[string]$Notes = "",
	[string]$AssetPath,
	[switch]$Draft
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$repo = (git remote get-url origin) -replace ".*github.com[/:]","" -replace "\.git$",""
if ([string]::IsNullOrWhiteSpace($repo)) { throw "Cannot determine GitHub repo from 'origin' remote." }

$gh = Get-Command gh.exe -ErrorAction SilentlyContinue
if (-not $gh) { throw "GitHub CLI 'gh' is required. Install from https://cli.github.com/." }

$args = @('release','create', $Tag, '--title', $Title, '--notes', $Notes)
if ($Draft) { $args += '--draft' }
if ($AssetPath) {
	if (-not (Test-Path -LiteralPath $AssetPath)) { throw "Asset not found: $AssetPath" }
	$args += $AssetPath
}

Write-Host "Creating GitHub release $Tag for $repo..." -ForegroundColor Cyan
& gh @args 2>&1 | ForEach-Object { Write-Host $_ }

Write-Host "Release created." -ForegroundColor Green



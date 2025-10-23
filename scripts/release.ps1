param(
	[Parameter(Mandatory=$true)][string]$Version,
	[switch]$Sign,
	[switch]$Draft,
	[string]$IssuerTimestampUrl = "http://timestamp.sectigo.com"
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$root = Resolve-Path "$PSScriptRoot\.."
Set-Location $root

$artifacts = Join-Path $root 'artifacts'
Ensure-Directory -Path $artifacts

Write-Host "Updating Inno Setup version to $Version" -ForegroundColor Cyan
& "$PSScriptRoot\update-iss-version.ps1" -Version $Version

Write-Host "Please build the WPF app in Visual Studio (Release|x64), then compile the Inno Setup script (ZLGetCertSetup.iss) to generate the installer in 'artifacts'." -ForegroundColor Yellow
Read-Host "Press Enter after the installer has been generated"

$installer = Get-ChildItem -LiteralPath $artifacts -Filter 'ZLGetCertInstaller*.exe' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $installer) { throw "No installer found in $artifacts. Ensure Inno Setup output is configured to 'artifacts'." }

if ($Sign) {
	& "$PSScriptRoot\sign-installer.ps1" -InstallerPath $installer.FullName -TimestampUrl $IssuerTimestampUrl -VerboseLog
}

$checksum = New-Checksum -FilePath $installer.FullName

$tag = "v$Version"
$title = "ZLGetCert $Version"
$notes = "Release $Version"

& "$PSScriptRoot\create-github-release.ps1" -Tag $tag -Title $title -Notes $notes -AssetPath $installer.FullName -Draft:$Draft

Write-Host "Done." -ForegroundColor Green



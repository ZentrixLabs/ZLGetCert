param(
	[Parameter(Mandatory=$true)][string]$InstallerPath,
	[string]$TimestampUrl = "http://timestamp.sectigo.com",
	[string]$Description = "ZLGetCert Installer",
	[switch]$VerboseLog
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\common.ps1"

# Expect environment variables for SSL.com eSigner (or use local cert store if available)
$username = Require-Env -Name 'CODESIGN_USERNAME'
$password = Require-Env -Name 'CODESIGN_PASSWORD'
$credId = Require-Env -Name 'CODESIGN_CREDENTIAL_ID'
$totp = Require-Env -Name 'CODESIGN_TOTP_SECRET'

if (-not (Test-Path -LiteralPath $InstallerPath)) {
	throw "Installer not found: $InstallerPath"
}

# Use sslcom/esigner-codesign if available via npm, else fall back to signtool if local cert present
$npmEsigner = Get-Command esigner-codesign -ErrorAction SilentlyContinue
if ($npmEsigner) {
	$logArg = $null
	if ($VerboseLog) { $logArg = "--verbose" }
	$cmd = @(
		"esigner-codesign",
		"sign",
		"--credential-id", $credId,
		"--username", $username,
		"--password", $password,
		"--totp-secret", $totp,
		"--tsp", $TimestampUrl,
		"--description", $Description,
		$InstallerPath
	)
	if ($logArg) { $cmd += $logArg }
	Write-Host "Signing via SSL.com eSigner..." -ForegroundColor Cyan
	& $cmd 2>&1 | ForEach-Object { Write-Host $_ }
} else {
	# Fallback: signtool using current user cert store (requires pre-installed cert)
	$signtool = Get-Command signtool.exe -ErrorAction SilentlyContinue
	if (-not $signtool) {
		throw "Neither esigner-codesign nor signtool.exe is available. Install one to proceed."
	}
	Write-Host "Signing via signtool.exe using current user cert store..." -ForegroundColor Cyan
	& signtool.exe sign /n "ZentrixLabs" /tr $TimestampUrl /td sha256 /fd sha256 /d $Description $InstallerPath
}

Write-Host "Signed: $InstallerPath" -ForegroundColor Green



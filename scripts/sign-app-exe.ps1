param(
	[Parameter(Mandatory=$true)][string]$ExePath,
	[string]$Thumbprint = $env:CODESIGN_CERT_SHA1,
	[string]$SigntoolPath = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe",
	[string]$TimestampUrl = "http://timestamp.sectigo.com/rfc3161",
	[string]$Description = "ZLGetCert",
	[string]$DescriptionUrl = "https://github.com/ZentrixLabs/ZLGetCert"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ExePath)) {
	throw "EXE not found: $ExePath"
}

if (-not (Test-Path -LiteralPath $SigntoolPath)) {
	throw "signtool.exe not found at: $SigntoolPath"
}

if ([string]::IsNullOrWhiteSpace($Thumbprint)) {
	throw "Provide a certificate thumbprint via -Thumbprint or CODESIGN_CERT_SHA1 environment variable."
}

& $SigntoolPath sign /fd SHA256 /td SHA256 /tr $TimestampUrl /sha1 $Thumbprint /d $Description /du $DescriptionUrl $ExePath

& $SigntoolPath verify /pa $ExePath | Out-Null
Write-Host "Signed: $ExePath" -ForegroundColor Green



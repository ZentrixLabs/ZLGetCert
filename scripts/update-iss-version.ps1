param(
	[Parameter(Mandatory=$true)][string]$Version,
	[string]$IssPath = "${PSScriptRoot}\..\ZLGetCertSetup.iss"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $IssPath)) {
	throw "ISS file not found at: $IssPath"
}

$content = Get-Content -LiteralPath $IssPath -Raw

# Update or insert the MyAppVersion define
if ($content -match '(?m)^#define\s+MyAppVersion\s+".*?"') {
	$content = [System.Text.RegularExpressions.Regex]::Replace(
		$content,
		'(?m)^(#define\s+MyAppVersion\s+)"[^"]*"',
		('$1"{0}"' -f $Version)
	)
} else {
	# Insert after MyAppName define
	$content = [System.Text.RegularExpressions.Regex]::Replace(
		$content,
		'(?m)^(#define\s+MyAppName\s+".*?"\r?\n)',
		('$1#define MyAppVersion "{0}"`r`n' -f $Version)
	)
}

Set-Content -LiteralPath $IssPath -Value $content -Encoding UTF8

Write-Host "Updated MyAppVersion to $Version in $IssPath" -ForegroundColor Green



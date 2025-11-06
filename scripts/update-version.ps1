param(
	[Parameter(Mandatory=$true)][string]$Version,
	[string]$AssemblyInfoPath = "${PSScriptRoot}\..\ZLGetCert\Properties\AssemblyInfo.cs",
	[string]$IssPath = "${PSScriptRoot}\..\ZLGetCertSetup.iss"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $AssemblyInfoPath)) {
	throw "AssemblyInfo file not found at: $AssemblyInfoPath"
}

if (-not (Test-Path -LiteralPath $IssPath)) {
	throw "ISS file not found at: $IssPath"
}

# Update AssemblyInfo.cs
Write-Host "Updating version in $AssemblyInfoPath..." -ForegroundColor Cyan
$assemblyContent = Get-Content -LiteralPath $AssemblyInfoPath -Raw

# Update AssemblyVersion (X.Y.Z.0 format)
$assemblyContent = $assemblyContent -replace '\[assembly:\s*AssemblyVersion\("[^"]*"\)\]', "[assembly: AssemblyVersion(`"$Version.0`")]"

# Update AssemblyFileVersion (X.Y.Z.0 format)
$assemblyContent = $assemblyContent -replace '\[assembly:\s*AssemblyFileVersion\("[^"]*"\)\]', "[assembly: AssemblyFileVersion(`"$Version.0`")]"

# Update AssemblyInformationalVersion (X.Y.Z format)
$assemblyContent = $assemblyContent -replace '\[assembly:\s*AssemblyInformationalVersion\("[^"]*"\)\]', "[assembly: AssemblyInformationalVersion(`"$Version`")]"

Set-Content -LiteralPath $AssemblyInfoPath -Value $assemblyContent -Encoding UTF8 -NoNewline
Write-Host "  Updated AssemblyVersion to $Version.0" -ForegroundColor Green
Write-Host "  Updated AssemblyFileVersion to $Version.0" -ForegroundColor Green
Write-Host "  Updated AssemblyInformationalVersion to $Version" -ForegroundColor Green

# Update ISS file
Write-Host "Updating version in $IssPath..." -ForegroundColor Cyan
$issContent = Get-Content -LiteralPath $IssPath -Raw

# Update or insert the MyAppVersion define
if ($issContent -match '(?m)^#define\s+MyAppVersion\s+".*?"') {
	$issContent = [System.Text.RegularExpressions.Regex]::Replace(
		$issContent,
		'(?m)^(#define\s+MyAppVersion\s+)"[^"]*"',
		('$1"{0}"' -f $Version)
	)
} else {
	# Insert after MyAppName define
	$issContent = [System.Text.RegularExpressions.Regex]::Replace(
		$issContent,
		'(?m)^(#define\s+MyAppName\s+".*?"\r?\n)',
		("`$1#define MyAppVersion `"$Version`"`r`n")
	)
}

Set-Content -LiteralPath $IssPath -Value $issContent -Encoding UTF8 -NoNewline
Write-Host "  Updated MyAppVersion to $Version in ISS" -ForegroundColor Green

Write-Host "`nVersion $Version synchronized across AssemblyInfo and installer files" -ForegroundColor Green


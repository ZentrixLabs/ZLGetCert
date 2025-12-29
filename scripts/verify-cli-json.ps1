<#
.SYNOPSIS
    Verifies that CLI JSON output is valid and parseable.

.DESCRIPTION
    This script acts as a guardrail to prevent breaking automation consumers by
    ensuring that the zlgetcert CLI always produces valid JSON output when the
    --format json flag is used, even when operations fail.

    This script enforces JSON stability, not business success. Commands may fail
    (non-zero exit codes are expected and acceptable), but the JSON output must
    always be parseable.

.PARAMETER None
    This script accepts no parameters.

.NOTES
    Assumes zlgetcert.exe is available on PATH or in the repository root.
#>

# Determine zlgetcert.exe location
$zlgetcertExe = $null
if (Test-Path ".\zlgetcert.exe") {
    $zlgetcertExe = ".\zlgetcert.exe"
} else {
    $cmd = Get-Command zlgetcert.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        $zlgetcertExe = $cmd.Source
    }
}

if (-not $zlgetcertExe) {
    Write-Error "zlgetcert.exe not found. Build it or add it to PATH."
    exit 1
}

$requestFile = "docs\fixtures\golden\request.json"
if (-not (Test-Path $requestFile)) {
    Write-Error "Fixture file not found: $requestFile"
    exit 1
}

Write-Host "Using zlgetcert.exe: $zlgetcertExe"
Write-Host "Using fixture file: $requestFile"
Write-Host ""

$allValid = $true

# Verify doctor JSON output
Write-Host "Testing doctor command JSON output..."
try {
    # Capture stdout only (stderr flows to console naturally, not suppressed)
    $doctorOutput = & $zlgetcertExe doctor --request $requestFile --format json | Out-String
    $null = $doctorOutput | ConvertFrom-Json
    Write-Host "Doctor JSON output valid"
} catch {
    Write-Error "Doctor JSON output is invalid"
    Write-Error $_.Exception.Message
    $allValid = $false
}

# Verify request JSON output (expected failure but valid JSON)
Write-Host "Testing request command JSON output..."
try {
    # Capture stdout only (stderr flows to console naturally, not suppressed)
    $requestOutput = & $zlgetcertExe request --request $requestFile --format json | Out-String
    $null = $requestOutput | ConvertFrom-Json
    Write-Host "Request JSON output valid"
} catch {
    Write-Error "Request JSON output is invalid"
    Write-Error $_.Exception.Message
    $allValid = $false
}

if (-not $allValid) {
    exit 1
}

exit 0


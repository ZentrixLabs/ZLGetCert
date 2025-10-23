$ErrorActionPreference = 'Stop'

function Ensure-Module {
	param(
		[string]$Name
	)
	if (-not (Get-Module -ListAvailable -Name $Name)) {
		Write-Host "Installing module $Name..." -ForegroundColor Yellow
		Install-Module $Name -Scope CurrentUser -Force -AllowClobber | Out-Null
	}
	Import-Module $Name -ErrorAction Stop
}

function Require-Env {
	param(
		[Parameter(Mandatory=$true)][string]$Name
	)
	$val = [Environment]::GetEnvironmentVariable($Name)
	if ([string]::IsNullOrWhiteSpace($val)) {
		throw "Environment variable '$Name' is required."
	}
	return $val
}

function Ensure-Directory {
	param([string]$Path)
	if (-not (Test-Path -LiteralPath $Path)) {
		New-Item -ItemType Directory -Path $Path | Out-Null
	}
}

function Get-GitTag {
	$tag = git describe --tags --abbrev=0 2>$null
	if (-not $tag) { return $null }
	return $tag.Trim()
}

function New-Checksum {
	param(
		[Parameter(Mandatory=$true)][string]$FilePath
	)
	$sha256 = Get-FileHash -Algorithm SHA256 -Path $FilePath
	$sumPath = "$FilePath.sha256"
	"$($sha256.Hash)  $([System.IO.Path]::GetFileName($FilePath))" | Set-Content -NoNewline -Path $sumPath
	return $sumPath
}

## Dot-sourced helpers; no module export required

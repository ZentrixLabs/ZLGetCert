param(
	[Parameter(Mandatory = $true)][string]$Version,
	[switch]$Sign,
	[switch]$Draft,
	[switch]$Auto,
	[switch]$AutoBuild,
	[switch]$AutoInstaller,
	[string]$Configuration = "Release",
	[string]$Platform = "Any CPU",
	[string]$MSBuildPath,
	[string]$InnoSetupPath
)

$ErrorActionPreference = 'Stop'

if ($Auto) {
	$AutoBuild = $true
	$AutoInstaller = $true
}

function Write-Step {
	param(
		[string]$Message,
		[string]$Color = 'Cyan'
	)
	Write-Host "`n$Message" -ForegroundColor $Color
}

function Get-MSBuildPath {
	param([string]$ExplicitPath)

	if ($ExplicitPath -and (Test-Path $ExplicitPath)) {
		return (Resolve-Path $ExplicitPath).Path
	}

	$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
	if (Test-Path $vswhere) {
		$installationPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2>$null
		if ($installationPath) {
			$candidate = Join-Path $installationPath "MSBuild\Current\Bin\MSBuild.exe"
			if (Test-Path $candidate) {
				return $candidate
			}
		}
	}

	$fallbacks = @(
		"${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
		"${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
		"${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
		"${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
		"${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
		"${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
	) | Where-Object { Test-Path $_ }

	if ($fallbacks) {
		return $fallbacks | Select-Object -First 1
	}

	return $null
}

function Invoke-MSBuild {
	param(
		[string]$SolutionPath,
		[string]$Configuration,
		[string]$Platform,
		[string]$MSBuildPath
	)

	$msbuild = Get-MSBuildPath -ExplicitPath $MSBuildPath
	if (-not $msbuild) {
		throw "MSBuild.exe not found. Provide -MSBuildPath or install the Visual Studio Build Tools."
	}

	Write-Host "Using MSBuild at: $msbuild" -ForegroundColor Cyan

	$platformProperty = if ($Platform -match '\s') { "`"$Platform`"" } else { $Platform }

	$arguments = @(
		"`"$SolutionPath`"",
		"/t:Rebuild",
		"/p:Configuration=$Configuration",
		"/p:Platform=$platformProperty",
		"/m"
	)

	Write-Host "Building solution..." -ForegroundColor Yellow

	$argumentString = $arguments -join ' '

	$startInfo = New-Object System.Diagnostics.ProcessStartInfo
	$startInfo.FileName = $msbuild
	$startInfo.Arguments = $argumentString
	$startInfo.UseShellExecute = $false
	$startInfo.RedirectStandardOutput = $true
	$startInfo.RedirectStandardError = $true
	$startInfo.CreateNoWindow = $true

	$process = New-Object System.Diagnostics.Process
	$process.StartInfo = $startInfo

	$null = $process.Start()
	$standardOutput = $process.StandardOutput.ReadToEnd()
	$standardError = $process.StandardError.ReadToEnd()
	$process.WaitForExit()

	if ($standardOutput) {
		Write-Host $standardOutput
	}
	if ($standardError) {
		Write-Error $standardError
	}

	$exitCode = $process.ExitCode
	Set-Variable -Name LASTEXITCODE -Scope Global -Value $exitCode

	Write-Host "MSBuild exited with code $exitCode" -ForegroundColor DarkGray

	if ($exitCode -ne 0) {
		throw "MSBuild failed with exit code $exitCode"
	}
}

function Get-LatestSigntoolPath {
	$candidates = @()

	$kitsRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
	if (Test-Path $kitsRoot) {
		Get-ChildItem -Path $kitsRoot -Directory -ErrorAction SilentlyContinue |
		Where-Object { $_.Name -match '^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$' } |
		Sort-Object { [version]$_.Name } -Descending |
		ForEach-Object {
			$p = Join-Path $_.FullName "x64\signtool.exe"
			if (Test-Path $p) { $candidates += $p }
		}
	}

	$alt = Join-Path ${env:ProgramFiles} "Windows Kits\10\bin\x64\signtool.exe"
	if (Test-Path $alt) { $candidates += $alt }

	try {
		$where = (where.exe signtool 2>$null | Select-Object -First 1)
		if ($where) { $candidates += $where }
	} catch {}

	return $candidates | Select-Object -Unique | Select-Object -First 1
}

function Get-InnoCompilerPath {
	param([string]$ExplicitPath)

	if ($ExplicitPath -and (Test-Path $ExplicitPath)) {
		return (Resolve-Path $ExplicitPath).Path
	}

	$candidates = @(
		"${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
		"${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
		"${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe"
	) | Where-Object { Test-Path $_ }

	if ($candidates) {
		return $candidates | Select-Object -First 1
	}

	return $null
}

function Invoke-InnoCompiler {
	param(
		[string]$ScriptPath,
		[bool]$SigningEnabled,
		[string]$InnoSetupPath
	)

	$compiler = Get-InnoCompilerPath -ExplicitPath $InnoSetupPath
	if (-not $compiler) {
		throw "ISCC.exe (Inno Setup Command-Line Compiler) not found. Provide -InnoSetupPath or install Inno Setup."
	}

	Write-Host "Using Inno Setup compiler at: $compiler" -ForegroundColor Cyan
	$arguments = @("/Qp")

	if ($SigningEnabled) {
		$signWrapperPath = Join-Path $PSScriptRoot "sign-installer.cmd"
		$resolvedSignWrapper = Resolve-Path -Path $signWrapperPath -ErrorAction SilentlyContinue
		if (-not $resolvedSignWrapper) {
			throw "sign-installer.cmd not found at $signWrapperPath"
		}

		$escapedWrapperPath = $resolvedSignWrapper.Path.Replace('"', '""')
		$signToolCommand = 'cmd.exe /c ""{0}"" "$f"' -f $escapedWrapperPath

		$arguments += '/dEnableSigning=1'
		$arguments += "/dSignToolCommand=""$signToolCommand"""
	} else {
		$arguments += '/dEnableSigning=0'
	}

	$arguments += "`"$ScriptPath`""

	Write-Host "Compiling installer..." -ForegroundColor Yellow
	$null = & $compiler @arguments

	if ($LASTEXITCODE -ne 0) {
		throw "Inno Setup compilation failed with exit code $LASTEXITCODE"
	}
}

function Invoke-ExecutableSigner {
	param([string]$PathToSign)

	if (-not (Test-Path $PathToSign)) {
		throw "Cannot sign missing file: $PathToSign"
	}

	if (Get-Command -Name SignAppSSLdotCom -CommandType Function -ErrorAction SilentlyContinue) {
		Write-Host "Signing via SignAppSSLdotCom..." -ForegroundColor Yellow
		try {
			SignAppSSLdotCom -Path $PathToSign
			if ($LASTEXITCODE -eq 0) {
				return
			}
			Write-Warning "SignAppSSLdotCom reported exit code $LASTEXITCODE. Falling back to scripts/sign-app.ps1."
		} catch {
			Write-Warning "SignAppSSLdotCom threw an error: $($_.Exception.Message). Falling back to scripts/sign-app.ps1."
		}
	} else {
		Write-Host "SignAppSSLdotCom not loaded; falling back to scripts/sign-app.ps1" -ForegroundColor Yellow
	}

	& "$PSScriptRoot\sign-app.ps1" -ExePath $PathToSign
}

# Ensure we operate from repository root
$root = Resolve-Path "$PSScriptRoot\.."
Set-Location $root

$artifacts = Join-Path $root 'artifacts'
if (-not (Test-Path $artifacts)) {
	New-Item -ItemType Directory -Path $artifacts | Out-Null
}

Write-Host "=== ZLGetCert Release Workflow ===" -ForegroundColor Green

# Step 1: Update version in project files
Write-Step "Step 1: Updating version to $Version..."
& "$PSScriptRoot\update-version.ps1" -Version $Version

$solutionPath = Join-Path $root 'ZLGetCert.sln'
$builtExePath = Join-Path $root 'ZLGetCert\bin\Release\ZLGetCert.exe'
$innoScriptPath = Join-Path $root 'ZLGetCertSetup.iss'

# Step 2: Build the application
if ($AutoBuild) {
	Write-Step "Step 2: Building solution with MSBuild..." -Color 'Yellow'
	Invoke-MSBuild -SolutionPath $solutionPath -Configuration $Configuration -Platform $Platform -MSBuildPath $MSBuildPath
} else {
	Write-Step "Step 2: Build the solution in Visual Studio ($Configuration)..." -Color 'Yellow'
	Read-Host "Press Enter after building in Visual Studio"
}

# Step 3: Sign the executable (optional)
if ($Sign) {
	Write-Step "Step 3: Signing ZLGetCert.exe..." -Color 'Yellow'
	Invoke-ExecutableSigner -PathToSign $builtExePath
} else {
	Write-Step "Step 3: Skipping EXE signing (Sign switch not provided)." -Color 'DarkGray'
}

# Step 4: Compile installer via Inno Setup (manual)
Write-Step "Step 4: Compile the installer in Inno Setup GUI..." -Color 'Yellow'
Write-Host "  Open Inno Setup IDE and load ZLGetCertSetup.iss" -ForegroundColor DarkGray
Write-Host "  Compile the installer (ensuring your SignTool profile signs outputs)" -ForegroundColor DarkGray
Read-Host "Press Enter after the installer has been generated in 'artifacts'"

# Step 5: Locate installer
$installer = Get-ChildItem -LiteralPath $artifacts -Filter 'ZLGetCertInstaller*.exe' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $installer) {
	throw "No installer found in $artifacts. Ensure Inno Setup output is configured to 'artifacts'."
}

Write-Host "Found installer: $($installer.Name)" -ForegroundColor Green

# Step 6: Create checksum
Write-Step "Step 5: Creating checksum..."
$shaFile = "$($installer.FullName).sha256"
$sha = (Get-FileHash -Algorithm SHA256 $installer.FullName).Hash
Set-Content -Path $shaFile -NoNewline -Value $sha
Write-Host "Checksum: $shaFile" -ForegroundColor Green

# Optional: sign the installer when Inno Setup signing is disabled
if ($Sign -and -not $AutoInstaller) {
	# When using the GUI workflow, signing happens via Inno Setup IDE configuration.
	Write-Host "Installer signing handled by Inno Setup GUI configuration." -ForegroundColor DarkGray
} elseif ($Sign -and $AutoInstaller) {
	Write-Host "Re-signing installer to ensure digital signature..." -ForegroundColor Yellow
	Invoke-ExecutableSigner -PathToSign $installer.FullName
}

# Step 7: Upload to GitHub
Write-Step "Step 6: Uploading to GitHub..."
$tag = "v$Version"
$title = "ZLGetCert $Version"
$notes = "Release $Version"

& "$PSScriptRoot\upload-release.ps1" -Version $Version -InstallerPath $installer.FullName -Notes $notes -Draft:$Draft

Write-Host "`n=== Release complete ===" -ForegroundColor Green
Write-Host "Release: $tag" -ForegroundColor Cyan
Write-Host "Installer: $($installer.FullName)" -ForegroundColor Cyan
if ($Draft) {
	Write-Host "Published as draft release" -ForegroundColor Yellow
}

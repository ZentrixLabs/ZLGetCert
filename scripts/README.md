# ZLGetCert Build and Release Scripts

## Overview

This directory contains scripts to manage versions, sign executables, and release ZLGetCert. The workflow is designed to be simple and match the pattern used in SrtExtractor.

## Scripts

### `update-version.ps1`

Updates the version in both the AssemblyInfo and installer script to keep them in sync.

**Usage:**
```powershell
pwsh ./scripts/update-version.ps1 -Version 1.8.2
```

**What it does:**
- Updates `AssemblyVersion`, `AssemblyFileVersion`, `AssemblyInformationalVersion` in `ZLGetCert/Properties/AssemblyInfo.cs`
- Updates `#define MyAppVersion` in `ZLGetCertSetup.iss`
- Single source of truth for versioning

### `sign-app.ps1`

Signs the ZLGetCert.exe executable using your YubiKey certificate.

**Prerequisites:**
- Set environment variable `CODESIGN_CERT_SHA1` with your certificate thumbprint
- OR pass `-Thumbprint` parameter
- Windows SDK with signtool.exe installed

**Usage:**
```powershell
# Using environment variable
$env:CODESIGN_CERT_SHA1 = "YOUR_THUMBPRINT"
pwsh ./scripts/sign-app.ps1

# With explicit thumbprint
pwsh ./scripts/sign-app.ps1 -Thumbprint "YOUR_THUMBPRINT"

# Custom exe path
pwsh ./scripts/sign-app.ps1 -ExePath "path\to\ZLGetCert.exe"
```

**What it does:**
- Finds signtool.exe automatically
- Signs with SHA256 digest and timestamp
- Verifies the signature
- Uses http://timestamp.sectigo.com/rfc3161 for timestamping

### `release.ps1`

Orchestrates the complete release workflow: version update, building, signing (optional), installer compilation, and GitHub release.

**Usage:**
```powershell
# Without signing (dev/test release)
pwsh ./scripts/release.ps1 -Version 1.8.2

# With signing (production release)
pwsh ./scripts/release.ps1 -Version 1.8.2 -Sign

# Draft release (don't publish immediately)
pwsh ./scripts/release.ps1 -Version 1.8.2 -Sign -Draft

# Fully automated build + installer compile
pwsh ./scripts/release.ps1 -Version 1.8.2 -Sign -Auto

# Auto build only (explicit MSBuild path)
pwsh ./scripts/release.ps1 -Version 1.8.2 -Sign -AutoBuild -MSBuildPath "C:\VS\MSBuild\Current\Bin\MSBuild.exe"
```

**What it does:**
1. Updates version in AssemblyInfo and ISS files
2. Builds automatically with MSBuild when `-Auto`/`-AutoBuild` are supplied (otherwise prompts for Visual Studio build)
3. Optionally signs the exe (if `-Sign` is provided). Uses `SignAppSSLdotCom` when available; falls back to `sign-app.ps1`
4. Compiles installer with Inno Setup CLI when `-Auto`/`-AutoInstaller` are supplied (otherwise prompts for Inno Setup GUI). Pass explicit `-InnoSetupPath` if ISCC.exe isn't on default path
5. Creates checksum file
6. Uploads to GitHub release (supports `-Draft`)

### `upload-release.ps1`

Uploads the installer to a GitHub release. Usually called by `release.ps1`, but can be used standalone.

**Usage:**
```powershell
pwsh ./scripts/upload-release.ps1 -Version 1.8.2 -InstallerPath "artifacts\ZLGetCertInstaller.exe"
```

**What it does:**
- Creates git tag (if doesn't exist)
- Creates GitHub release
- Uploads installer and checksum

## Workflow Examples

### Daily Development (No Signing)

1. Build in Visual Studio (Release)
2. Compile `ZLGetCertSetup.iss` in Inno Setup GUI
3. Installer appears in `artifacts` folder (unsigned)

### Production Release (With Signing)

```powershell
# Ensure your SignAppSSLdotCom function or environment variables are ready

# Run the release orchestrator (fully automated)
pwsh ./scripts/release.ps1 -Version 1.8.2 -Sign -Auto
```

The script will:
1. Update versions
2. Build via MSBuild (falls back to VS prompt if MSBuild not found)
3. Sign the exe (prefers `SignAppSSLdotCom`, falls back to `sign-app.ps1` if unavailable or it errors)
4. Compile the installer with Inno Setup CLI and re-sign the output if necessary
5. Upload to GitHub (pass `-Draft` to keep it unpublished)

### Manual Step-by-Step

If you prefer more control:

```powershell
# 1. Update version
pwsh ./scripts/update-version.ps1 -Version 1.8.2

# 2. Build in Visual Studio (Release)

# 3. Sign the exe
pwsh ./scripts/sign-app.ps1

# 4. Compile in Inno Setup GUI
# (opens ZLGetCertSetup.iss, compiler signs installer automatically)

# 5. Upload to GitHub
pwsh ./scripts/upload-release.ps1 -Version 1.8.2 [-Draft]
```

## Inno Setup Signing

The `ZLGetCertSetup.iss` file references `SignTool=SignTool` which points to the SignTool command configured in Inno Setup's IDE (Tools → Configure Sign Tools).

**To configure:**
1. Open Inno Setup IDE
2. Go to Tools → Configure Sign Tools
3. Add a new tool named "SignTool"
4. Set the command to use your cert thumbprint:

```
signtool.exe sign /fd SHA256 /td SHA256 /tr http://timestamp.sectigo.com/rfc3161 /sha1 YOUR_THUMBPRINT /d "ZLGetCert Installer" $f
```

Replace `YOUR_THUMBPRINT` with your certificate thumbprint.

When you compile the installer in Inno Setup, it will automatically sign both the installer and uninstaller.

## Version Management

Versions are managed in three places:
1. `ZLGetCert/Properties/AssemblyInfo.cs` - `AssemblyVersion`, `AssemblyFileVersion`, `AssemblyInformationalVersion`
2. `ZLGetCertSetup.iss` - `#define MyAppVersion "X.Y.Z"`
3. Git tag - `vX.Y.Z` (created during upload)

The `update-version.ps1` script ensures all three stay in sync. Always run it before creating a release.

## Troubleshooting

### "signtool.exe not found"
Install Windows 10/11 SDK from Microsoft. The script will auto-detect the latest version.

### "Certificate not found"
- Ensure your YubiKey is plugged in
- Verify the thumbprint in the environment variable
- Check that the certificate is installed in Windows cert store

### "Inno Setup signing failed"
- Verify SignTool is configured correctly in Inno Setup IDE
- Check that the thumbprint in the SignTool command matches your cert
- Ensure the YubiKey is plugged in when compiling

### "GitHub release failed"
- Ensure `gh` CLI is installed and authenticated (`gh auth login`)
- Check that you have push permissions to the repository

## Notes

- Each script does one thing well
- Manual steps remain the default (Visual Studio build, Inno GUI), but `-Auto` switches enable full automation when MSBuild/ISCC are installed
- Matching SrtExtractor's proven workflow while allowing incremental automation
- No GitHub Actions - all releases are manual and local


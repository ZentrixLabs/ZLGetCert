# Local Release Workflow (No GitHub Actions)

## Overview

This project uses local PowerShell scripts to prepare releases, sign executables, and publish GitHub releases. Builds are performed manually in Visual Studio and compiled with the Inno Setup GUI.

## Prerequisites

- Visual Studio (Release configuration)
- Inno Setup Compiler (GUI) with output set to `artifacts`
- GitHub CLI (`gh`) authenticated for this repository
- Windows SDK with `signtool.exe` for code signing
- Certificate thumbprint (YubiKey or installed cert)

## Quick Start

### Production Release (With Signing)

```powershell
# Set your certificate thumbprint (one-time per session)
$env:CODESIGN_CERT_SHA1 = "YOUR_THUMBPRINT_HERE"

# Run the release orchestrator
pwsh ./scripts/release.ps1 -Version 1.8.2 -Sign
```

The script will:
1. Update version in AssemblyInfo and Inno Setup files
2. Prompt you to build in Visual Studio (Release)
3. Sign the EXE with your certificate
4. Prompt you to compile in Inno Setup GUI (which signs the installer)
5. Create checksum and upload to GitHub

### Development Release (No Signing)

```powershell
pwsh ./scripts/release.ps1 -Version 1.8.2
```

Skips signing steps for faster iteration.

## Manual Step-by-Step

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
pwsh ./scripts/upload-release.ps1 -Version 1.8.2
```

## Scripts

See `scripts/README.md` for detailed documentation on each script:
- `update-version.ps1` - Sync versions across AssemblyInfo and Inno Setup
- `sign-app.ps1` - Sign the application EXE
- `release.ps1` - Orchestrate the complete workflow
- `upload-release.ps1` - Upload to GitHub release

## Inno Setup Configuration

Configure signing in Inno Setup IDE (Tools → Configure Sign Tools):

**SignTool command:**
```
signtool.exe sign /fd SHA256 /td SHA256 /tr http://timestamp.sectigo.com/rfc3161 /sha1 YOUR_THUMBPRINT /d "ZLGetCert Installer" $f
```

Replace `YOUR_THUMBPRINT` with your certificate thumbprint.

## Notes

- GitHub Actions have been removed; all releases are manual and local
- Keep `OutputDir=artifacts` in `ZLGetCertSetup.iss`
- The app EXE must be signed before compiling the installer
- Inno Setup automatically signs the installer and uninstaller

## Troubleshooting

- **signtool.exe not found**: Install Windows 10/11 SDK
- **Certificate not found**: Ensure YubiKey is plugged in and thumbprint is correct
- **Inno Setup signing failed**: Verify SignTool is configured in Inno Setup IDE
- **GitHub release failed**: Run `gh auth login` and verify permissions

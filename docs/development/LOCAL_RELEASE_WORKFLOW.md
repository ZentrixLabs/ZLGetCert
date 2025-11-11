# Local Release Workflow (No GitHub Actions)

## Overview

This project uses local PowerShell scripts to prepare releases, sign executables, and publish GitHub releases. Manual steps (Visual Studio build, Inno Setup GUI) remain available, but the tooling now supports full automation when the required command-line tools are installed.

## Prerequisites

- Visual Studio (Release configuration)
- Inno Setup Compiler (GUI) with output set to `artifacts`
- GitHub CLI (`gh`) authenticated for this repository
- Windows SDK with `signtool.exe` for code signing
- Certificate thumbprint (YubiKey or installed cert) or `SignAppSSLdotCom` function loaded in your PowerShell profile
- **Optional for automation**
  - MSBuild (`msbuild.exe`) – included with Visual Studio or Build Tools
  - Inno Setup command-line compiler (`ISCC.exe`)

## Quick Start

### Production Release (With Signing)

```powershell
# Ensure SignAppSSLdotCom is available (in profile) or set your thumbprint once per session
# $env:CODESIGN_CERT_SHA1 = "YOUR_THUMBPRINT_HERE"

# Run the release orchestrator (fully automated)
pwsh ./scripts/release.ps1 -Version 1.8.2 -Sign -Auto
```

The script will:
1. Update version in AssemblyInfo and Inno Setup files
2. Build via MSBuild (`-AutoBuild`)
3. Sign the EXE (prefers `SignAppSSLdotCom`, falls back to `sign-app.ps1` if unavailable or it errors)
4. Compile the installer through Inno Setup CLI (`-AutoInstaller`) and re-sign the output if necessary
5. Create checksum and upload to GitHub (use `-Draft` to keep the release unpublished)

### Development Release (No Signing)

```powershell
pwsh ./scripts/release.ps1 -Version 1.8.2 [-Auto]
```

Skips signing steps for faster iteration. Add `-Auto` to include MSBuild/ISCC automation while leaving the EXE unsigned.

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
pwsh ./scripts/upload-release.ps1 -Version 1.8.2 [-Draft]
```

## Scripts

See `scripts/README.md` for detailed documentation on each script:
- `update-version.ps1` - Sync versions across AssemblyInfo and Inno Setup
- `sign-app.ps1` - Sign the application EXE (fallback if `SignAppSSLdotCom` isn't loaded)
- `release.ps1` - Orchestrate the complete workflow (manual prompts by default, automation with switches)
- `upload-release.ps1` - Upload to GitHub release

## Automation Flags

- `-Auto` &mdash; enables both MSBuild and Inno Setup automation (`-AutoBuild` + `-AutoInstaller`)
- `-AutoBuild` &mdash; invoke MSBuild instead of prompting for Visual Studio. Override with `-MSBuildPath`
- `-AutoInstaller` &mdash; invoke Inno Setup CLI instead of prompting for GUI. Override with `-InnoSetupPath`
- `-Draft` &mdash; pass through to GitHub release creation to keep the release unpublished

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
- Inno Setup automatically signs the installer and uninstaller when `EnableSigning` is set (the scripts pass this flag automatically when `-Sign` is used)

## Troubleshooting

- **signtool.exe not found**: Install Windows 10/11 SDK
- **Certificate not found**: Ensure YubiKey is plugged in and thumbprint is correct
- **Inno Setup signing failed**: Verify SignTool is configured in Inno Setup IDE
- **GitHub release failed**: Run `gh auth login` and verify permissions

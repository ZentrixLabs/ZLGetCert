# Local Release Workflow (No GitHub Actions)

## Overview

This project now uses local PowerShell scripts to prepare releases, sign installers, and publish GitHub releases. Builds are performed manually in Visual Studio and compiled with the Inno Setup GUI, per project rules.

## Prerequisites

- Visual Studio build of `ZLGetCert` (Release | x64)
- Inno Setup Compiler (GUI) with output set to `artifacts`
- GitHub CLI (`gh`) authenticated for this repository
- One of the following for code signing:
  - SSL.com eSigner CLI (`npm i -g @sslcom/esigner-codesign`) with environment variables:
    - `CODESIGN_USERNAME`, `CODESIGN_PASSWORD`, `CODESIGN_CREDENTIAL_ID`, `CODESIGN_TOTP_SECRET`
  - or `signtool.exe` with a pre-installed code signing certificate

## Scripts

- `scripts/update-iss-version.ps1` — Updates `MyAppVersion` in `ZLGetCertSetup.iss`
- `scripts/sign-installer.ps1` — Signs the installer via SSL.com or `signtool`
- `scripts/create-github-release.ps1` — Creates a GitHub release and uploads the asset
- `scripts/release.ps1` — Orchestrates version update, signing, and release

## Usage

1) Sign the app EXE after Visual Studio build (Release | x64):
```powershell
pwsh ./scripts/sign-app-exe.ps1 -ExePath ./ZLGetCert/bin/Release/ZLGetCert.exe -Thumbprint "YOUR_SHA1_THUMBPRINT"
```

2) Run the orchestrator:
```powershell
pwsh ./scripts/release.ps1 -Version 1.2.3 -Sign -Draft
```
- Updates Inno Setup version to `1.2.3`
- Prompts you to build in Visual Studio and compile `ZLGetCertSetup.iss`
- Signs the newest installer in `artifacts` (if `-Sign` provided)
- Creates a GitHub draft release and uploads the installer

3) Publish the draft release on GitHub when ready.

## Notes

- GitHub Actions workflows have been removed; no CI build/sign runs on tags.
- Keep `OutputDir=artifacts` in `ZLGetCertSetup.iss` so scripts can locate the installer.
- For SSL.com signing, set the required environment variables in your shell.

## Troubleshooting

- No installer found in `artifacts`: Compile the Inno script after updating the version and confirm `OutputDir=artifacts`.
- eSigner CLI missing: Install via `npm i -g @sslcom/esigner-codesign` or use `signtool.exe`.
- GitHub release errors: Ensure `gh auth status` is valid and `origin` points to GitHub.

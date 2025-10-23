# Code Signing Workflow (Deprecated GitHub Actions)

This document described the previous GitHub Actions-based signing flow. We have moved to a local, script-driven workflow. See `docs/development/LOCAL_RELEASE_WORKFLOW.md` for the current process.

## Workflows

### 1. Build and Release (Unsigned) — Deprecated

Previously handled by `.github/workflows/deploy.yml` on tag push.

#### What it does:
- Builds the application from the tagged commit
- Creates an unsigned installer (`ZLGetCertInstaller.exe`)
- Publishes a GitHub release with the unsigned installer
- **Does NOT sign the code** (saves SSL.com credits)

#### Usage:
```bash
# Create and push a tag
git tag v1.2.3
git push origin v1.2.3

# Workflow runs automatically
# Creates release with unsigned installer
```

### 2. Code Sign Existing Release — Deprecated

Previously handled by `.github/workflows/code-sign-release.yml` via manual dispatch.

#### What it does:
- Downloads the installer from an existing release
- Signs it with SSL.com code signing certificate
- Uploads signed version with suffix `-signed.exe`
- Optionally replaces the original unsigned installer
- Updates release notes with signing timestamp

#### Usage:

1. **Via GitHub UI** (Recommended):
   - Go to Actions → "Code Sign Existing Release"
   - Click "Run workflow"
   - Enter the release tag (e.g., `v1.2.3`)
   - Choose whether to replace the original unsigned installer
   - Click "Run workflow"

2. **Via GitHub CLI**:
   ```bash
   # Sign a release, keeping both signed and unsigned versions
   gh workflow run code-sign-release.yml \
     -f release_tag=v1.2.3 \
     -f replace_assets=false

   # Sign a release and replace unsigned with signed version
   gh workflow run code-sign-release.yml \
     -f release_tag=v1.2.3 \
     -f replace_assets=true
   ```

#### Options:

- **`release_tag`** (required): The release tag to sign (e.g., `v1.2.3`)
- **`replace_assets`** (required): 
  - `true`: Replaces `ZLGetCertInstaller.exe` with signed version
  - `false`: Keeps both `ZLGetCertInstaller.exe` (unsigned) and `ZLGetCertInstaller-X.X.X-signed.exe`

## Current Workflow

Use the local scripts documented in `LOCAL_RELEASE_WORKFLOW.md` to prepare, optionally sign, and publish releases.

## Benefits

- **Cost Savings**: Only use SSL.com credits for final production releases
- **Faster Iteration**: Test unsigned versions without delays or costs
- **Flexibility**: Can sign any existing release at any time
- **Safety**: Unsigned test releases are clearly marked

## Verification

After signing, verify the digital signature:

```powershell
# Check signature status
Get-AuthenticodeSignature "ZLGetCertInstaller.exe"

# Should show:
# Status        : Valid
# SignerCertificate : CN=...
```

## Secrets Required

The code signing workflow requires these GitHub secrets:

- `CODESIGN_USERNAME`: SSL.com eSigner username
- `CODESIGN_PASSWORD`: SSL.com eSigner password  
- `CODESIGN_CREDENTIAL_ID`: SSL.com credential ID
- `CODESIGN_TOTP_SECRET`: SSL.com TOTP secret for 2FA

## Troubleshooting

### "Asset not found" error
- Ensure the release tag exists and has a `ZLGetCertInstaller.exe` asset
- Check that you entered the correct tag name (with `v` prefix)

### Code signing timeout
- The workflow has a 10-minute timeout
- If signing fails, you can re-run the workflow
- Check SSL.com dashboard for service status

### Permission denied when replacing assets
- Ensure the GitHub token has `contents: write` permission
- Check repository settings → Actions → Workflow permissions

## Migration Notes

**Previous Behavior**: All tagged releases were automatically signed

**New Behavior**: 
- All tagged releases are unsigned by default
- Sign production releases manually when ready

This change was implemented to reduce SSL.com credit usage while maintaining the ability to test and iterate quickly.


# ZLGetCert Security Remediation Plan
**Created:** October 14, 2025  
**Status:** Planning Phase  
**Target Completion:** TBD

## Overview

This document provides a detailed, actionable plan for remediating the 15 security issues identified in the security review. Each issue includes specific code changes, files to modify, and acceptance criteria.

---

## PHASE 1: CRITICAL ISSUES (Must Fix Before Any Production Use)

### Issue #1: Remove Plaintext Default Passwords

**Estimated Effort:** 6 hours  
**Status:** ❌ Not Started  
**Dependencies:** None

#### Files to Modify:
1. `ZLGetCert/appsettings.json`
2. `ZLGetCert/Models/AppConfiguration.cs`
3. `ZLGetCert/Services/ConfigurationService.cs`
4. `ZLGetCert/Services/CertificateService.cs`
5. `ZLGetCert/ViewModels/SettingsViewModel.cs`
6. All example config files in `ZLGetCert/examples/`

#### Specific Changes:

**Step 1.1: Update AppConfiguration.cs**
```csharp
// REMOVE the DefaultPassword property or mark it obsolete
[Obsolete("DefaultPassword is deprecated for security reasons. Remove from configuration.")]
public string DefaultPassword
{
    get => null; // Always return null
    set 
    { 
        if (!string.IsNullOrEmpty(value))
        {
            // Log warning if someone tries to set it
            System.Diagnostics.Debug.WriteLine(
                "WARNING: DefaultPassword in configuration is deprecated and ignored for security reasons.");
        }
    }
}

// Add new property
public bool RequirePasswordEntry { get; set; } = true;
```

**Step 1.2: Update CertificateService.cs (line 869-887)**
```csharp
// OLD CODE:
private string GetPasswordFromSecureString(SecureString securePassword)
{
    if (securePassword == null)
        return _configService.GetConfiguration().DefaultSettings.DefaultPassword;
    // ... rest
}

// NEW CODE:
private string GetPasswordFromSecureString(SecureString securePassword)
{
    if (securePassword == null || securePassword.Length == 0)
    {
        throw new ArgumentException(
            "Password is required for certificate operations. " +
            "Default passwords are no longer supported for security reasons.");
    }
    
    var ptr = IntPtr.Zero;
    try
    {
        ptr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(securePassword);
        return System.Runtime.InteropServices.Marshal.PtrToStringUni(ptr);
    }
    finally
    {
        if (ptr != IntPtr.Zero)
        {
            System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(ptr);
        }
    }
}
```

**Step 1.3: Update all config files**
```json
// Remove or comment out DefaultPassword in:
// - appsettings.json
// - examples/development-config.json
// - examples/enterprise-ad-config.json
// - examples/client-auth-config.json
// - examples/code-signing-config.json

// Change to:
"DefaultPassword": "", // REMOVED: No default passwords for security
```

**Step 1.4: Add password strength validation**
```csharp
// Add to ValidationHelper.cs
public static bool IsPasswordAcceptable(string password, out List<string> errors)
{
    errors = new List<string>();
    
    if (string.IsNullOrEmpty(password))
    {
        errors.Add("Password is required");
        return false;
    }
    
    if (password.Length < 8)
        errors.Add("Password must be at least 8 characters");
    
    if (!password.Any(char.IsUpper))
        errors.Add("Password must contain at least one uppercase letter");
    
    if (!password.Any(char.IsLower))
        errors.Add("Password must contain at least one lowercase letter");
    
    if (!password.Any(char.IsDigit))
        errors.Add("Password must contain at least one number");
    
    // Check against common passwords
    string[] commonPasswords = { "password", "Password1", "123456", "admin", 
        "letmein", "welcome", "monkey", "password123", "test123" };
    if (commonPasswords.Contains(password, StringComparer.OrdinalIgnoreCase))
        errors.Add("Password is too common and easily guessed");
    
    return errors.Count == 0;
}
```

#### Testing Checklist:
- [ ] Application starts without DefaultPassword in config
- [ ] Error shown when trying to generate cert without password
- [ ] Weak passwords are rejected with helpful error message
- [ ] Strong passwords are accepted
- [ ] Old config files with DefaultPassword show warning

---

### Issue #2: Encrypt Exported Private Keys

**Estimated Effort:** 12 hours  
**Status:** ❌ Not Started  
**Dependencies:** None

#### Files to Modify:
1. `ZLGetCert/Services/PemExportService.cs`
2. `ZLGetCert/Services/FileManagementService.cs` (new methods)
3. `ZLGetCert/Models/CertificateRequest.cs`
4. `ZLGetCert/ViewModels/CertificateRequestViewModel.cs`
5. `ZLGetCert/Views/CertificateRequestView.xaml`

#### Specific Changes:

**Step 2.1: Add encrypted key export to PemExportService.cs**
```csharp
/// <summary>
/// Extract PEM certificate and KEY file (with optional encryption)
/// </summary>
public bool ExtractPemAndKey(string pfxPath, string password, string outputDir, 
    string certificateName, bool encryptKey = true, string keyPassword = null)
{
    try
    {
        _logger.LogInfo("Extracting PEM and KEY files from {0}", pfxPath);

        if (!File.Exists(pfxPath))
        {
            _logger.LogError("PFX file not found: {0}", pfxPath);
            return false;
        }

        var cert = new X509Certificate2(pfxPath, password, X509KeyStorageFlags.Exportable);

        if (!cert.HasPrivateKey)
        {
            _logger.LogError("Certificate does not contain a private key");
            return false;
        }

        var pemPath = Path.Combine(outputDir, $"{certificateName}.pem");
        var keyPath = Path.Combine(outputDir, $"{certificateName}.key");

        // Export certificate to PEM format
        ExportCertificateToPem(cert, pemPath);

        // Export private key (encrypted or unencrypted based on option)
        if (encryptKey)
        {
            if (string.IsNullOrEmpty(keyPassword))
            {
                _logger.LogWarning("Key encryption requested but no password provided, using PFX password");
                keyPassword = password;
            }
            ExportEncryptedPrivateKeyToKey(cert, keyPath, keyPassword);
            _logger.LogInfo("Private key exported with encryption (PKCS#8)");
        }
        else
        {
            // Show warning for unencrypted export
            _logger.LogWarning("SECURITY WARNING: Exporting private key without encryption!");
            ExportPrivateKeyToKey(cert, keyPath);
        }

        // Set restrictive file permissions
        SetRestrictiveFilePermissions(pemPath);
        SetRestrictiveFilePermissions(keyPath);

        _logger.LogInfo("Successfully extracted PEM and KEY files");
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error extracting PEM and KEY files");
        return false;
    }
}

/// <summary>
/// Export RSA private key in encrypted PKCS#8 format
/// </summary>
private void ExportEncryptedPrivateKeyToKey(X509Certificate2 cert, string keyPath, string password)
{
    using (var rsa = cert.GetRSAPrivateKey())
    {
        if (rsa != null)
        {
            // Export to encrypted PKCS#8 format
            var encryptedKey = rsa.ExportEncryptedPkcs8PrivateKey(
                password,
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes256Cbc,
                    HashAlgorithmName.SHA256,
                    iterationCount: 100000)); // PBKDF2 with 100k iterations

            var sb = new StringBuilder();
            sb.AppendLine("-----BEGIN ENCRYPTED PRIVATE KEY-----");
            sb.AppendLine(Convert.ToBase64String(encryptedKey, Base64FormattingOptions.InsertLineBreaks));
            sb.AppendLine("-----END ENCRYPTED PRIVATE KEY-----");
            
            File.WriteAllText(keyPath, sb.ToString(), Encoding.ASCII);
            _logger.LogInfo("Encrypted RSA private key exported (PKCS#8): {0}", keyPath);
            return;
        }
    }

    throw new NotSupportedException("Certificate private key type is not supported for encrypted export");
}

/// <summary>
/// Set restrictive NTFS permissions on file (owner-only access)
/// </summary>
private void SetRestrictiveFilePermissions(string filePath)
{
    try
    {
        var fileInfo = new FileInfo(filePath);
        var security = fileInfo.GetAccessControl();
        
        // Disable inheritance and remove existing rules
        security.SetAccessRuleProtection(true, false);
        
        // Remove all existing access rules
        var accessRules = security.GetAccessRules(true, false, typeof(System.Security.Principal.NTAccount));
        foreach (FileSystemAccessRule rule in accessRules)
        {
            security.RemoveAccessRule(rule);
        }
        
        // Add owner-only full control
        var owner = System.Security.Principal.WindowsIdentity.GetCurrent().User;
        var ownerRule = new FileSystemAccessRule(
            owner,
            FileSystemRights.FullControl,
            AccessControlType.Allow);
        security.AddAccessRule(ownerRule);
        
        // Add SYSTEM account (required for some operations)
        var systemSid = new System.Security.Principal.SecurityIdentifier(
            System.Security.Principal.WellKnownSidType.LocalSystemSid, null);
        var systemRule = new FileSystemAccessRule(
            systemSid,
            FileSystemRights.FullControl,
            AccessControlType.Allow);
        security.AddAccessRule(systemRule);
        
        fileInfo.SetAccessControl(security);
        
        _logger.LogDebug("Set restrictive permissions on file: {0}", filePath);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to set restrictive permissions on {0}", filePath);
        // Don't throw - file is still created, just with default permissions
    }
}
```

**Step 2.2: Update UI to add encryption option**
```xml
<!-- Add to CertificateRequestView.xaml -->
<CheckBox IsChecked="{Binding EncryptExportedKey}" 
          Content="Encrypt exported private key (PKCS#8)" 
          ToolTip="Strongly recommended: Encrypt the .key file with a password"
          IsEnabled="{Binding ExtractPemKey}"
          Margin="20,5,0,0"/>

<TextBlock Text="⚠ WARNING: Unencrypted private keys are a security risk!" 
           Foreground="Red"
           FontWeight="Bold"
           Visibility="{Binding ShowUnencryptedKeyWarning, Converter={StaticResource BoolToVisibilityConverter}}"
           Margin="20,5,0,5"/>
```

**Step 2.3: Add secure delete to FileManagementService.cs**
```csharp
/// <summary>
/// Securely delete a file by overwriting before deletion
/// Implements DoD 5220.22-M standard (3-pass overwrite)
/// </summary>
public void SecureDelete(string filePath)
{
    if (!File.Exists(filePath))
    {
        _logger.LogDebug("File does not exist for secure delete: {0}", filePath);
        return;
    }

    try
    {
        var fileInfo = new FileInfo(filePath);
        var length = fileInfo.Length;

        // Overwrite file 3 times with random data
        using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
        {
            for (int pass = 0; pass < 3; pass++)
            {
                stream.Position = 0;
                byte[] buffer = new byte[4096];
                long remaining = length;

                while (remaining > 0)
                {
                    int toWrite = (int)Math.Min(buffer.Length, remaining);
                    rng.GetBytes(buffer);
                    stream.Write(buffer, 0, toWrite);
                    remaining -= toWrite;
                }
                
                stream.Flush(true); // Flush to disk
            }
        }

        // Now delete the file
        File.Delete(filePath);
        _logger.LogDebug("Securely deleted file: {0}", filePath);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error securely deleting file: {0}", filePath);
        // Try normal delete as fallback
        try
        {
            File.Delete(filePath);
        }
        catch
        {
            // Ignore - we did our best
        }
    }
}
```

#### Testing Checklist:
- [ ] Exported keys are encrypted by default
- [ ] Encrypted keys can be used with OpenSSL (test with: `openssl rsa -in test.key -check`)
- [ ] File permissions are restricted to owner only
- [ ] Warning shown when exporting unencrypted keys
- [ ] Secure delete overwrites file before deletion

---

### Issue #3: Fix Command Injection Vulnerabilities

**Estimated Effort:** 10 hours  
**Status:** ❌ Not Started  
**Dependencies:** None

#### Files to Modify:
1. `ZLGetCert/Services/CertificateService.cs`
2. `ZLGetCert/Utilities/ValidationHelper.cs`

#### Specific Changes:

**Step 3.1: Add input validation helper**
```csharp
// Add to ValidationHelper.cs
public static class ProcessArgumentValidator
{
    /// <summary>
    /// Validate and sanitize file path for use in process arguments
    /// </summary>
    public static string ValidateFilePath(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException($"{parameterName} cannot be empty", parameterName);

        // Check for command injection characters
        char[] dangerousChars = { '&', '|', ';', '>', '<', '^', '`', '$', '(', ')', '{', '}' };
        if (path.IndexOfAny(dangerousChars) >= 0)
        {
            throw new ArgumentException(
                $"{parameterName} contains invalid characters that could be used for command injection", 
                parameterName);
        }

        // Normalize path
        try
        {
            path = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid file path: {ex.Message}", parameterName, ex);
        }

        return path;
    }

    /// <summary>
    /// Validate CA server name format
    /// </summary>
    public static string ValidateCAServerName(string serverName, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException($"{parameterName} cannot be empty", parameterName);

        // CA server should be a valid hostname/FQDN
        // Allow letters, numbers, dots, hyphens only
        var regex = new Regex(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$");
        
        if (!regex.IsMatch(serverName))
        {
            throw new ArgumentException(
                $"{parameterName} must be a valid hostname or FQDN", 
                parameterName);
        }

        if (serverName.Length > 253)
        {
            throw new ArgumentException(
                $"{parameterName} exceeds maximum length of 253 characters", 
                parameterName);
        }

        return serverName;
    }

    /// <summary>
    /// Validate certificate thumbprint format
    /// </summary>
    public static string ValidateThumbprint(string thumbprint, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(thumbprint))
            throw new ArgumentException($"{parameterName} cannot be empty", parameterName);

        // Thumbprint should be 40 hex characters
        var regex = new Regex(@"^[0-9A-Fa-f]{40}$");
        
        if (!regex.IsMatch(thumbprint))
        {
            throw new ArgumentException(
                $"{parameterName} must be a 40-character hexadecimal string", 
                parameterName);
        }

        return thumbprint.ToUpperInvariant();
    }
}
```

**Step 3.2: Update CertificateService.cs to use ArgumentList**
```csharp
// Update GetAvailableTemplates method (line 228-282)
public List<CertificateTemplate> GetAvailableTemplates(string caServer = null)
{
    var templates = new List<CertificateTemplate>();

    try
    {
        var config = _configService.GetConfiguration();
        var server = caServer ?? config.CertificateAuthority.Server;

        if (string.IsNullOrEmpty(server))
        {
            _logger.LogWarning("No CA server configured");
            return templates;
        }

        // VALIDATE input before using
        server = ValidationHelper.ProcessArgumentValidator.ValidateCAServerName(server, "CA Server");
        
        var caConfig = $"{server}\\{server.Split('.')[0].ToUpper()}";
        
        _logger.LogInfo("Querying available templates from CA: {0}", caConfig);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "certutil.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        // Use ArgumentList to prevent injection
        process.StartInfo.ArgumentList.Add("-CATemplates");
        process.StartInfo.ArgumentList.Add("-config");
        process.StartInfo.ArgumentList.Add(caConfig);

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            _logger.LogError("Template query timed out after 30 seconds");
            return templates;
        }

        if (process.ExitCode != 0)
        {
            _logger.LogError("Failed to query templates: {0}", error);
            return templates;
        }

        templates = ParseTemplateOutput(output);
        _logger.LogInfo("Found {0} available templates", templates.Count);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error querying certificate templates");
    }

    return templates;
}

// Update CreateCSR method (line 601-637)
private bool CreateCSR(string infPath, string csrPath)
{
    try
    {
        // Validate file paths
        infPath = ValidationHelper.ProcessArgumentValidator.ValidateFilePath(infPath, "INF Path");
        csrPath = ValidationHelper.ProcessArgumentValidator.ValidateFilePath(csrPath, "CSR Path");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "certreq.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        // Use ArgumentList
        process.StartInfo.ArgumentList.Add("-new");
        process.StartInfo.ArgumentList.Add(infPath);
        process.StartInfo.ArgumentList.Add(csrPath);

        _logger.LogDebug("Creating CSR with certreq.exe");
        process.Start();
        
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            _logger.LogError("CSR creation timed out");
            return false;
        }

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            _logger.LogError("Failed to create CSR: {0}", error);
            return false;
        }

        _logger.LogInfo("CSR created successfully: {0}", csrPath);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating CSR");
        return false;
    }
}
```

#### Testing Checklist:
- [ ] Normal operations work with valid inputs
- [ ] Injection attempts with `&`, `|`, `;` are rejected
- [ ] File paths with special characters are rejected
- [ ] CA server names with invalid characters are rejected
- [ ] Error messages are logged for invalid inputs

---

### Issue #4: Use SecureString in ViewModels

**Estimated Effort:** 8 hours  
**Status:** ❌ Not Started  
**Dependencies:** None

#### Files to Modify:
1. `ZLGetCert/ViewModels/CertificateRequestViewModel.cs`
2. `ZLGetCert/Views/CertificateRequestView.xaml`
3. `ZLGetCert/Views/CertificateRequestView.xaml.cs`

#### Specific Changes:

**Step 4.1: Update CertificateRequestViewModel.cs**
```csharp
// Change password properties to SecureString
private SecureString _pfxPassword;
private SecureString _confirmPassword;

public SecureString PfxPassword
{
    get => _pfxPassword;
    set
    {
        // Dispose old SecureString
        _pfxPassword?.Dispose();
        
        if (SetProperty(ref _pfxPassword, value))
        {
            OnPropertyChanged(nameof(PasswordStrength));
            OnPropertyChanged(nameof(CanGenerate));
        }
    }
}

public SecureString ConfirmPassword
{
    get => _confirmPassword;
    set
    {
        _confirmPassword?.Dispose();
        SetProperty(ref _confirmPassword, value);
        OnPropertyChanged(nameof(CanGenerate));
    }
}

// Update CanGenerate property
public bool CanGenerate
{
    get
    {
        if (string.IsNullOrWhiteSpace(CAServer) || string.IsNullOrWhiteSpace(Template))
            return false;

        if (Type == CertificateType.FromCSR)
        {
            return !string.IsNullOrWhiteSpace(CsrFilePath) && 
                   PfxPassword != null && PfxPassword.Length > 0;
        }

        bool passwordsMatch = true;
        if (ConfirmPassword != null && ConfirmPassword.Length > 0)
        {
            passwordsMatch = SecureStringHelper.SecureStringEquals(PfxPassword, ConfirmPassword);
        }

        return !string.IsNullOrWhiteSpace(HostName) &&
               !string.IsNullOrWhiteSpace(Location) &&
               !string.IsNullOrWhiteSpace(State) &&
               PfxPassword != null && PfxPassword.Length > 0 &&
               passwordsMatch;
    }
}

// Update ToCertificateRequest method
public CertificateRequest ToCertificateRequest()
{
    var request = new CertificateRequest
    {
        HostName = HostName,
        FQDN = FQDN,
        Location = Location,
        State = State,
        Company = Company,
        OU = OU,
        CAServer = CAServer,
        Template = Template,
        Type = Type,
        CsrFilePath = CsrFilePath,
        ExtractPemKey = ExtractPemKey,
        ExtractCaBundle = ExtractCaBundle,
        PfxPassword = PfxPassword?.Copy(), // Create a copy
        ConfirmPassword = ConfirmPassword != null && ConfirmPassword.Length > 0
    };

    // ... rest of method
    return request;
}

// Implement IDisposable
public void Dispose()
{
    _pfxPassword?.Dispose();
    _confirmPassword?.Dispose();
}
```

**Step 4.2: Update View to use PasswordBox**
```xml
<!-- Replace TextBox with PasswordBox in CertificateRequestView.xaml -->
<PasswordBox x:Name="PfxPasswordBox" 
             PasswordChanged="PfxPasswordBox_PasswordChanged"
             ToolTip="Enter a strong password to protect the certificate"/>

<PasswordBox x:Name="ConfirmPasswordBox" 
             PasswordChanged="ConfirmPasswordBox_PasswordChanged"
             ToolTip="Re-enter password to confirm"/>
```

**Step 4.3: Add code-behind handlers**
```csharp
// CertificateRequestView.xaml.cs
private void PfxPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
{
    var viewModel = DataContext as CertificateRequestViewModel;
    if (viewModel != null)
    {
        viewModel.PfxPassword = ((PasswordBox)sender).SecurePassword;
    }
}

private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
{
    var viewModel = DataContext as CertificateRequestViewModel;
    if (viewModel != null)
    {
        viewModel.ConfirmPassword = ((PasswordBox)sender).SecurePassword;
    }
}
```

#### Testing Checklist:
- [ ] Password entry works with PasswordBox control
- [ ] Passwords not visible in memory dumps
- [ ] Password confirmation works correctly
- [ ] SecureStrings are disposed on ViewModel disposal
- [ ] Password validation still works

---

## PHASE 2: HIGH PRIORITY ISSUES

### Issue #5: Improve SecureString Comparison

**Estimated Effort:** 4 hours  
**Status:** ❌ Not Started

*(See SECURITY_REVIEW.md for implementation details)*

---

### Issue #6: Implement File System Permissions

**Estimated Effort:** 8 hours  
**Status:** ❌ Not Started

*(Already included in Issue #2 implementation)*

---

### Issue #7: Increase Default Key Length

**Estimated Effort:** 4 hours  
**Status:** ❌ Not Started

#### Quick Implementation:
```csharp
// Update appsettings.json
"KeyLength": 4096,  // Changed from 2048

// Add validation in UI
if (keyLength < 3072)
{
    MessageBox.Show(
        "WARNING: Key length below 3072 bits may not provide adequate security for long-lived certificates, " +
        "especially in OT environments. 4096 bits is recommended.",
        "Security Warning",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
}
```

---

### Issue #8: Validate Certificates Before Import

**Estimated Effort:** 6 hours  
**Status:** ❌ Not Started

*(See SECURITY_REVIEW.md for full implementation)*

---

### Issue #9: Secure INF File Handling

**Estimated Effort:** 3 hours  
**Status:** ❌ Not Started

**Quick Fix:**
```csharp
// After writing INF file
SetRestrictiveFilePermissions(filePaths.InfPath);

// Ensure cleanup in finally block
finally
{
    if (config.DefaultSettings.AutoCleanup)
    {
        FileManagementService.Instance.SecureDelete(filePaths.InfPath);
    }
}
```

---

### Issue #10: Implement Audit Logging

**Estimated Effort:** 12 hours  
**Status:** ❌ Not Started

*(See SECURITY_REVIEW.md for full AuditService implementation)*

---

## PHASE 3: MEDIUM PRIORITY ISSUES

*(Issues #11-15: See SECURITY_REVIEW.md for details)*

---

## Progress Tracking

### Overall Progress
- **Phase 1:** 0% Complete (0/4 issues)
- **Phase 2:** 0% Complete (0/5 issues)  
- **Phase 3:** 0% Complete (0/5 issues)

### Time Estimates
- Phase 1: 36 hours
- Phase 2: 37 hours
- Phase 3: 40 hours
- **Total:** ~113 hours (2.8 weeks for one developer)

---

## Testing Strategy

### Unit Tests to Create
1. `ProcessArgumentValidatorTests.cs` - Test input validation
2. `SecureStringHelperTests.cs` - Test secure string operations
3. `PemExportServiceTests.cs` - Test encrypted key export
4. `FilePermissionTests.cs` - Test file ACL setting
5. `AuditServiceTests.cs` - Test audit logging

### Integration Tests
1. End-to-end certificate generation with validation
2. File permissions verification
3. Secure deletion verification
4. Password handling throughout workflow

### Security Tests
1. Attempt command injection with various payloads
2. Check memory for plaintext passwords
3. Verify file permissions are restrictive
4. Test with malformed certificates
5. Verify audit trail completeness

---

## Deployment Checklist

Before deploying to production:

- [ ] All Phase 1 (Critical) issues resolved
- [ ] Code review completed
- [ ] Security testing completed
- [ ] Unit tests passing
- [ ] Integration tests passing
- [ ] Documentation updated
- [ ] User guide includes security best practices
- [ ] Configuration examples updated
- [ ] Audit logging tested and verified
- [ ] Backup and recovery procedures documented

---

## Notes

- This is a living document - update as work progresses
- Mark items complete with ✅ and date
- Add notes about challenges or deviations from plan
- Track any additional issues discovered during implementation

---

**Last Updated:** October 14, 2025  
**Next Review:** TBD


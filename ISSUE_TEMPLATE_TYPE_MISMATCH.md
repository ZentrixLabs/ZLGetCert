# Security/Logic Issue: Template and Certificate Type Mismatch

**Discovered:** October 14, 2025  
**Severity:** MEDIUM-HIGH  
**Category:** Business Logic / Security  
**Status:** IDENTIFIED

---

## Problem Description

Currently, the application allows users to:
1. Select a **CA Template** (e.g., "WebServer", "CodeSigning", "User")
2. **Independently** select a **Certificate Type** (Standard, Wildcard, ClientAuth, CodeSigning, etc.)

This creates several problems:

### Security Issues:
- User could select "WebServer" template but "CodeSigning" type
- Wrong EnhancedKeyUsageOIDs could be sent to CA
- CA might reject the request, or worse, issue a certificate with incorrect usage
- Certificate might not work for its intended purpose
- Could violate certificate policy

### Current State:

**EnhancedKeyUsageOIDs are hardcoded in config:**
- `1.3.6.1.5.5.7.3.1` = Server Authentication (WebServer)
- `1.3.6.1.5.5.7.3.2` = Client Authentication (ClientAuth)
- `1.3.6.1.5.5.7.3.3` = Code Signing
- `1.3.6.1.5.5.7.3.4` = Email Protection (S/MIME)

**Certificate Types in Enum:**
```csharp
public enum CertificateType
{
    Standard,      // Should map to WebServer template
    Wildcard,      // Should map to WebServer template with wildcard CN
    ClientAuth,    // Should map to User/ClientAuth template
    CodeSigning,   // Should map to CodeSigning template
    Email,         // Should map to Email template
    Custom,        // User provides OIDs
    FromCSR        // CSR already has OIDs
}
```

**Problem:** These two selections are currently independent!

---

## Expected Behavior

The **CA Template should dictate the Certificate Type**, not the other way around:

1. User selects CA Server → queries available templates
2. User selects Template → **automatically determines Certificate Type and OIDs**
3. Application configures KeyUsage and EnhancedKeyUsageOIDs based on template
4. User cannot create mismatched combinations

### Template → Type Mapping:

| CA Template Name | Certificate Type | EnhancedKeyUsageOID | KeyUsage |
|-----------------|------------------|---------------------|----------|
| WebServer | Standard | 1.3.6.1.5.5.7.3.1 | 0xa0 |
| WebServer (with wildcard) | Wildcard | 1.3.6.1.5.5.7.3.1 | 0xa0 |
| User / ClientAuth | ClientAuth | 1.3.6.1.5.5.7.3.2 | 0x80 |
| CodeSigning | CodeSigning | 1.3.6.1.5.5.7.3.3 | 0x80 |
| EmailProtection | Email | 1.3.6.1.5.5.7.3.4 | 0xa0 |
| Custom | Custom | User specifies | User specifies |
| (from CSR file) | FromCSR | From CSR | From CSR |

---

## Remediation Plan

### Phase 1: Add Template Metadata
**File:** `Models/CertificateTemplate.cs`

Add properties to CertificateTemplate:
```csharp
public class CertificateTemplate
{
    // Existing properties...
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string OID { get; set; }
    public int Version { get; set; }
    
    // NEW: Template metadata
    public CertificateType SuggestedType { get; set; }
    public List<string> EnhancedKeyUsageOIDs { get; set; }
    public string KeyUsage { get; set; }
    
    // NEW: Auto-detect type from template name
    public static CertificateType DetectTypeFromTemplateName(string templateName)
    {
        if (string.IsNullOrEmpty(templateName))
            return CertificateType.Custom;
            
        templateName = templateName.ToLowerInvariant();
        
        // Server/Web certificates
        if (templateName.Contains("web") || templateName.Contains("server") || 
            templateName.Contains("ssl") || templateName.Contains("tls"))
            return CertificateType.Standard;
            
        // Code signing
        if (templateName.Contains("codesign") || templateName.Contains("code"))
            return CertificateType.CodeSigning;
            
        // Client authentication
        if (templateName.Contains("user") || templateName.Contains("client") ||
            templateName.Contains("auth") || templateName.Contains("workstation"))
            return CertificateType.ClientAuth;
            
        // Email
        if (templateName.Contains("email") || templateName.Contains("smime") ||
            templateName.Contains("mail"))
            return CertificateType.Email;
            
        // Default to custom if can't detect
        return CertificateType.Custom;
    }
}
```

### Phase 2: Update Template Selection Logic
**File:** `ViewModels/CertificateRequestViewModel.cs`

When template is selected, automatically set certificate type:
```csharp
private string _template;
public string Template
{
    get => _template;
    set
    {
        if (SetProperty(ref _template, value))
        {
            // Auto-configure certificate type based on template
            AutoConfigureFromTemplate(value);
            OnPropertyChanged(nameof(CanGenerate));
        }
    }
}

private void AutoConfigureFromTemplate(string templateName)
{
    if (string.IsNullOrEmpty(templateName))
        return;
        
    // Find the template object
    var template = AvailableTemplates?.FirstOrDefault(t => t.Name == templateName);
    if (template != null)
    {
        // Auto-detect and set certificate type
        Type = CertificateTemplate.DetectTypeFromTemplateName(templateName);
        
        _logger.LogInfo("Auto-configured certificate type to {0} based on template {1}", 
            Type, templateName);
    }
}
```

### Phase 3: Add Validation
**File:** `Utilities/ValidationHelper.cs`

Add validation to prevent mismatches:
```csharp
public static ValidationResult ValidateTemplateTypeMatch(
    string templateName, 
    CertificateType type,
    List<string> enhancedKeyUsageOIDs)
{
    var result = new ValidationResult();
    
    // Detect expected type from template
    var expectedType = CertificateTemplate.DetectTypeFromTemplateName(templateName);
    
    // If it's not Custom or FromCSR, validate match
    if (type != CertificateType.Custom && type != CertificateType.FromCSR)
    {
        if (expectedType != type && expectedType != CertificateType.Custom)
        {
            result.AddWarning(
                $"Template '{templateName}' suggests certificate type '{expectedType}', " +
                $"but '{type}' was selected. This may cause the CA to reject the request.");
        }
    }
    
    // Validate OIDs match certificate type
    if (enhancedKeyUsageOIDs != null && enhancedKeyUsageOIDs.Any())
    {
        switch (type)
        {
            case CertificateType.Standard:
            case CertificateType.Wildcard:
                if (!enhancedKeyUsageOIDs.Contains("1.3.6.1.5.5.7.3.1"))
                {
                    result.AddError(
                        "WebServer certificates require Server Authentication OID (1.3.6.1.5.5.7.3.1)");
                }
                break;
                
            case CertificateType.CodeSigning:
                if (!enhancedKeyUsageOIDs.Contains("1.3.6.1.5.5.7.3.3"))
                {
                    result.AddError(
                        "Code Signing certificates require Code Signing OID (1.3.6.1.5.5.7.3.3)");
                }
                break;
                
            case CertificateType.ClientAuth:
                if (!enhancedKeyUsageOIDs.Contains("1.3.6.1.5.5.7.3.2"))
                {
                    result.AddError(
                        "Client Auth certificates require Client Authentication OID (1.3.6.1.5.5.7.3.2)");
                }
                break;
                
            case CertificateType.Email:
                if (!enhancedKeyUsageOIDs.Contains("1.3.6.1.5.5.7.3.4"))
                {
                    result.AddError(
                        "Email certificates require Email Protection OID (1.3.6.1.5.5.7.3.4)");
                }
                break;
        }
    }
    
    return result;
}
```

### Phase 4: Update Certificate Generation
**File:** `Services/CertificateService.cs`

Add validation before generating certificate:
```csharp
public CertificateInfo GenerateCertificate(Models.CertificateRequest request)
{
    try
    {
        _logger.LogInfo("Starting certificate generation for {0} ({1})", 
            request.CertificateName, request.Type);

        // SECURITY: Validate template/type match
        var config = _configService.GetConfiguration();
        var validation = ValidationHelper.ValidateTemplateTypeMatch(
            request.Template,
            request.Type,
            config.CertificateParameters.EnhancedKeyUsageOIDs);
            
        if (!validation.IsValid)
        {
            _logger.LogError("Template/Type validation failed: {0}", 
                string.Join(", ", validation.Errors));
            return new CertificateInfo 
            { 
                IsValid = false, 
                ErrorMessage = validation.GetMessage() 
            };
        }
        
        if (validation.Warnings.Any())
        {
            foreach (var warning in validation.Warnings)
            {
                _logger.LogWarning(warning);
            }
        }

        // Continue with existing generation logic...
    }
    // ... rest of method
}
```

---

## UI Changes Required

### Before (Current):
```
Template: [Dropdown: WebServer, CodeSigning, User, ...]
Type: [Radio Buttons: Standard, Wildcard, ClientAuth, CodeSigning, ...]
```
**Problem:** User can select mismatched combinations

### After (Proposed):
```
Template: [Dropdown: WebServer, CodeSigning, User, ...]
Type: [Auto-configured based on template] (Read-only or grayed out)
```
**Benefit:** No mismatches possible

### Alternative (If manual override needed):
```
Template: [Dropdown: WebServer, CodeSigning, User, ...]
Type: [Auto-configured, but editable with warning icon]
[⚠️ Warning: Template suggests 'Standard' but 'CodeSigning' selected]
```

---

## Testing Requirements

After implementing fix:

1. **Test template selection:**
   - Select "WebServer" → verify Type = Standard, OID = 1.3.6.1.5.5.7.3.1
   - Select "CodeSigning" → verify Type = CodeSigning, OID = 1.3.6.1.5.5.7.3.3
   - Select "User" → verify Type = ClientAuth, OID = 1.3.6.1.5.5.7.3.2

2. **Test validation:**
   - Try to manually override with wrong type → verify warning/error
   - Verify CA accepts the generated certificate
   - Verify certificate works for intended purpose

3. **Test edge cases:**
   - Custom templates with unknown names
   - Templates from different CA vendors
   - FromCSR type (should bypass validation)

---

## Priority Assessment

**Priority:** HIGH  
**Complexity:** MEDIUM  
**Estimated Time:** 6-8 hours  
**Dependencies:** None (can implement independently)

**Why High Priority:**
- Prevents creation of invalid/unusable certificates
- Improves user experience (less confusion)
- Reduces CA rejections
- Ensures certificates work for intended purpose
- Could be a compliance issue (wrong cert types)

**Suggested Timeline:**
- After completing Week 1 Critical Issues
- Before Week 2 High Priority issues
- Ideally implement with Issue #2 or immediately after

---

## References

### Enhanced Key Usage OIDs (RFC 5280):
- `1.3.6.1.5.5.7.3.1` - Server Authentication (TLS/SSL servers)
- `1.3.6.1.5.5.7.3.2` - Client Authentication (TLS/SSL clients)
- `1.3.6.1.5.5.7.3.3` - Code Signing
- `1.3.6.1.5.5.7.3.4` - Email Protection (S/MIME)
- `1.3.6.1.5.5.7.3.8` - Time Stamping
- `1.3.6.1.5.5.7.3.9` - OCSP Signing

### Key Usage Flags:
- `0xa0` (160) = Digital Signature + Key Encipherment (web servers)
- `0x80` (128) = Digital Signature only (code signing, client auth)
- `0x20` (32) = Key Encipherment only
- `0x04` (4) = Key Agreement

---

## Conclusion

This is an important issue that should be addressed to prevent:
1. User confusion
2. Invalid certificate generation
3. CA rejections
4. Certificates that don't work for their intended purpose
5. Potential security/compliance violations

The fix is straightforward: **template selection should auto-configure certificate type and parameters**, with validation to prevent mismatches.

**Recommendation:** Implement after Week 1 Critical Issues, before moving to Week 2 High Priority work.

---

**Created:** October 14, 2025  
**Impact:** User Experience, Security, Certificate Validity  
**Risk if not fixed:** Medium - Invalid certificates, user confusion, CA rejections


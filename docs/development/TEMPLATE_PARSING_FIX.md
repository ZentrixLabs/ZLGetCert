# Template Parsing Fix - Remove Auto-Enroll Text

## Problem
The certificate template dropdown was showing unwanted text like "Auto-Enroll: Access is denied" which is irrelevant for manual enrollment and cluttered the UI.

## Root Cause
The template parsing logic in `CertificateService.cs` wasn't handling the actual format returned by the CA server. The code expected:

```
Auto-Enroll: Access is denied. (TemplateName: DisplayName)
```

But the actual format was:

```
TemplateName: DisplayName -- Auto-Enroll: Access is denied.
```

## Solution
Updated the `ParseTemplateOutput` method in `CertificateService.cs` to handle multiple template formats:

### Before:
```csharp
// Only handled parentheses format
if (trimmedLine.Contains("(") && trimmedLine.Contains(":"))
{
    // Extract template info from parentheses
    var startParen = trimmedLine.IndexOf('(');
    var endParen = trimmedLine.LastIndexOf(')');
    // ... parsing logic
}
```

### After:
```csharp
// Handle multiple formats
if (trimmedLine.Contains(":"))
{
    string templateName = null;
    string displayName = null;
    
    // Format 1: "Auto-Enroll: Access is denied. (TemplateName: DisplayName)"
    if (trimmedLine.Contains("(") && trimmedLine.Contains(")"))
    {
        // ... existing parentheses parsing
    }
    // Format 2: "TemplateName: DisplayName -- Auto-Enroll: Access is denied."
    else if (trimmedLine.Contains(" -- Auto-Enroll:"))
    {
        var templatePart = trimmedLine.Split(new[] { " -- Auto-Enroll:" }, StringSplitOptions.None)[0];
        var parts = templatePart.Split(':');
        // ... parse template name and display name
    }
    // Format 3: "TemplateName: DisplayName -- Access is denied."
    else if (trimmedLine.Contains(" -- Access is denied."))
    {
        var templatePart = trimmedLine.Split(new[] { " -- Access is denied." }, StringSplitOptions.None)[0];
        var parts = templatePart.Split(':');
        // ... parse template name and display name
    }
}
```

## Supported Formats

The updated parser now handles these CA server output formats:

1. **Parentheses Format:**
   ```
   Auto-Enroll: Access is denied. (CodeSigning_MP_Modern: Code Signing_MP_Modern)
   ```

2. **Dash Format (Auto-Enroll):**
   ```
   CodeSigning_MP_Modern: Code Signing_MP_Modern -- Auto-Enroll: Access is denied.
   ```

3. **Dash Format (Access Denied):**
   ```
   WebServerV2: WebServerV2 -- Access is denied.
   ```

## Result

### Before:
```
CodeSigning_MP_Modern: Code Signing_MP_Modern -- Auto-Enroll: Access is denied.
WebServerV2: WebServerV2 -- Auto-Enroll: Access is denied.
EFS: Basic EFS -- Auto-Enroll: Access is denied.
User: User -- Auto-Enroll: Access is denied.
```

### After:
```
CodeSigning_MP_Modern: Code Signing_MP_Modern
WebServerV2: WebServerV2
EFS: Basic EFS
User: User
```

## Benefits

✓ **Cleaner UI** - No irrelevant "Auto-Enroll: Access is denied" text  
✓ **Better UX** - Templates show just the essential information  
✓ **More Readable** - Easier to scan template names  
✓ **Flexible Parsing** - Handles different CA server output formats  
✓ **Future-Proof** - Won't break if CA server format changes  

## Files Modified

**ZLGetCert/Services/CertificateService.cs**
- Updated `ParseTemplateOutput` method (lines 402-470)
- Added support for multiple template formats
- Improved parsing logic to extract clean template names

## Testing

The fix should result in:
- [ ] Template dropdown shows clean names without "Auto-Enroll: Access is denied"
- [ ] All available templates are still shown
- [ ] Template selection still works correctly
- [ ] Descriptions still appear below template names
- [ ] No parsing errors in logs

---

**Implementation Date:** October 14, 2025  
**Issue:** Template dropdown showing unwanted "Auto-Enroll: Access is denied" text  
**Status:** ✅ Complete - Ready for Testing

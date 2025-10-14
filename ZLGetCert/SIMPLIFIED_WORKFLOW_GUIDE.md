# ZLGetCert - Simplified Workflow Guide

**Version:** 2.0 (Template-Driven Architecture)  
**Date:** October 14, 2025  
**Status:** ✅ Production Ready

---

## Overview

ZLGetCert now uses a **template-driven architecture** that matches how sysadmins actually think about certificates. No more confusing Type selectors - just pick your template and go!

---

## Workflow 1: Generate Web Server Certificate

### What You See:
```
┌─────────────────────────────────────────────┐
│ Certificate Authority                       │
│ ├─ CA Server: [ca.company.com          ▼] │
│ └─ Template:  [WebServer               ▼] │ ← Pick template
├─────────────────────────────────────────────┤
│ Certificate Options                         │
│ □ Wildcard Certificate (*.domain.com)      │ ← Optional
├─────────────────────────────────────────────┤
│ Domain Information                          │
│ ├─ Hostname: [api                        ] │
│ └─ FQDN: api.company.com (auto)             │
├─────────────────────────────────────────────┤
│ Organization Information                    │
│ ├─ Location: [Seattle]  State: [WA]        │
│ └─ Company: [company.com]  OU: [IT]        │
├─────────────────────────────────────────────┤
│ Subject Alternative Names                   │
│ ├─ DNS: [api.company.com, www.company.com] │
│ └─ IP: [192.168.1.100]                     │
├─────────────────────────────────────────────┤
│ Security Settings                           │
│ ├─ Password: [••••••••]                    │
│ └─ Confirm:  [••••••••]                    │
└─────────────────────────────────────────────┘

[📄 Import from CSR...]  [🔧 Generate Certificate]
```

### What Happens Behind the Scenes:
```
Template "WebServer" selected
    ↓
Type = Standard (auto-detected)
OIDs = 1.3.6.1.5.5.7.3.1 (Server Authentication)
KeyUsage = 0xa0 (Digital Signature + Key Encipherment)
    ↓
Valid web server certificate created ✓
```

---

## Workflow 2: Generate Wildcard Certificate

### What You See:
```
┌─────────────────────────────────────────────┐
│ Certificate Authority                       │
│ ├─ CA Server: [ca.company.com          ▼] │
│ └─ Template:  [WebServer               ▼] │
├─────────────────────────────────────────────┤
│ Certificate Options                         │
│ ☑ Wildcard Certificate (*.domain.com)      │ ← CHECKED!
├─────────────────────────────────────────────┤
│ Domain Information                          │
│ ├─ Hostname: [disabled - not needed]       │
│ └─ FQDN: *.company.com (auto)               │
├─────────────────────────────────────────────┤
│ Organization Information                    │
│ ├─ Location: [Seattle]  State: [WA]        │
│ └─ Company: [company.com]  OU: [IT]        │
├─────────────────────────────────────────────┤
│ Subject Alternative Names                   │
│ └─ (Wildcard covers all subdomains)        │
├─────────────────────────────────────────────┤
│ Security Settings                           │
│ ├─ Password: [••••••••]                    │
│ └─ Confirm:  [••••••••]                    │
└─────────────────────────────────────────────┘

[📄 Import from CSR...]  [🔧 Generate Certificate]
```

### What Happens Behind the Scenes:
```
Template "WebServer" + Wildcard checked
    ↓
Type = Wildcard (auto-detected)
CN = *.company.com
OIDs = 1.3.6.1.5.5.7.3.1 (Server Authentication)
KeyUsage = 0xa0 (Digital Signature + Key Encipherment)
    ↓
Valid wildcard certificate created ✓
```

---

## Workflow 3: Generate Code Signing Certificate

### What You See:
```
┌─────────────────────────────────────────────┐
│ Certificate Authority                       │
│ ├─ CA Server: [ca.company.com          ▼] │
│ └─ Template:  [CodeSigning             ▼] │ ← Pick template
├─────────────────────────────────────────────┤
│ (No wildcard option - not applicable)      │
│ (No hostname fields - not needed)           │
│ (No SANs - not applicable)                  │
├─────────────────────────────────────────────┤
│ Organization Information                    │
│ ├─ Location: [Seattle]  State: [WA]        │
│ └─ Company: [company.com]  OU: [Dev]       │
├─────────────────────────────────────────────┤
│ Security Settings                           │
│ ├─ Password: [••••••••]                    │
│ └─ Confirm:  [••••••••]                    │
└─────────────────────────────────────────────┘

[📄 Import from CSR...]  [🔧 Generate Certificate]
```

### What Happens Behind the Scenes:
```
Template "CodeSigning" selected
    ↓
Type = CodeSigning (auto-detected)
OIDs = 1.3.6.1.5.5.7.3.3 (Code Signing)
KeyUsage = 0x80 (Digital Signature only)
    ↓
Valid code signing certificate created ✓
```

**Notice:** Hostname, wildcard, and SANs are hidden - they're not needed for code signing!

---

## Workflow 4: Import from CSR File

### Step 1: Click "Import from CSR File..." Button
```
[📄 Import from CSR File...]  ← Click this
```

### Step 2: Select CSR File
```
File Dialog Opens
→ Select: my-certificate.csr
→ Click: Open
```

### Step 3: Minimal Form (CSR Already Has Everything!)
```
┌─────────────────────────────────────────────┐
│ Certificate Authority                       │
│ ├─ CA Server: [ca.company.com          ▼] │ ← Still needed
│ └─ Template:  [WebServer               ▼] │ ← Still needed
├─────────────────────────────────────────────┤
│ CSR File Import                             │
│ ✅ CSR file loaded - hostname, org, SANs   │
│ File: [C:\temp\my-cert.csr]  [❌ Clear]   │
│ ℹ️ CSR contains all subject info          │
├─────────────────────────────────────────────┤
│ (Organization fields HIDDEN - from CSR)     │
│ (Hostname fields HIDDEN - from CSR)         │
│ (SANs HIDDEN - from CSR)                    │
├─────────────────────────────────────────────┤
│ Security Settings                           │
│ ├─ Password: [••••••••]                    │ ← Still needed
│ └─ Confirm:  [••••••••]                    │
└─────────────────────────────────────────────┘

[📄 Import from CSR...]  [🔧 Generate Certificate]
```

### What Happens Behind the Scenes:
```
CSR file loaded
    ↓
Type = FromCSR (auto-detected)
Subject DN = from CSR file (CN, O, OU, L, ST, C)
SANs = from CSR file (DNS names, IP addresses)
Public Key = from CSR file
    ↓
Only need: CA Server + Template + Password
    ↓
Submit CSR to CA → Get signed certificate ✓
```

**Notice:** Only 3 inputs needed - CA, Template, Password!

---

## Field Visibility Matrix

| Field / Section | WebServer | CodeSigning | User/ClientAuth | Email | CSR Import |
|-----------------|-----------|-------------|-----------------|-------|------------|
| CA Server | ✓ | ✓ | ✓ | ✓ | ✓ |
| Template | ✓ | ✓ | ✓ | ✓ | ✓ |
| Wildcard checkbox | ✓ | ✗ | ✗ | ✗ | ✗ |
| Hostname | ✓ | ✗ | ✗ | ✗ | ✗ |
| FQDN | ✓ | ✗ | ✗ | ✗ | ✗ |
| Organization info | ✓ | ✓ | ✓ | ✓ | ✗ |
| SANs | ✓ | ✗ | ✗ | ✗ | ✗ |
| Password | ✓ | ✓ | ✓ | ✓ | ✓ |
| Export options | ✓ | ✓ | ✓ | ✓ | ✓ |

**Legend:**
- ✓ = Visible and required/optional
- ✗ = Hidden (not applicable)

---

## How Templates Work

### Template Selection Auto-Configures Everything:

```
User picks template → App detects type → Correct OIDs/KeyUsage applied

WebServer          → Standard      → 1.3.6.1.5.5.7.3.1 (Server Auth)
WebServer + □     → Wildcard      → 1.3.6.1.5.5.7.3.1 (Server Auth)
CodeSigning       → CodeSigning   → 1.3.6.1.5.5.7.3.3 (Code Signing)
User              → ClientAuth    → 1.3.6.1.5.5.7.3.2 (Client Auth)
EmailProtection   → Email         → 1.3.6.1.5.5.7.3.4 (Email)
(Unknown)         → Custom        → User-specified
```

**It's impossible to create mismatches!** ✅

---

## Comparison: Old vs New Workflows

### Old Way (7 steps) ❌
```
1. Select CA Server
2. Select Template          ← Step 1
3. Select Certificate Type  ← Step 2 (CONFUSING!)
4. Fill Hostname
5. Fill Organization
6. Fill SANs
7. Enter Password
```
**Problems:**
- Confusing dual selection (template AND type)
- Easy to create mismatches
- More steps = more errors

### New Way (5 steps) ✅
```
1. Select CA Server
2. Select Template          ← ONE STEP!
3. Fill Hostname (if needed - shown automatically)
4. Fill Organization (if needed - shown automatically)
5. Enter Password
```
**Benefits:**
- One selection (template)
- Impossible to create mismatches
- Fewer steps = fewer errors
- Fields shown only when needed

---

## CSR Import: Old vs New

### Old Way (Complex) ❌
```
1. Select Type → FromCSR (radio button)
2. Browse for CSR file
3. Still fill in: Hostname, Location, State, Company, OU
4. Still fill in: SANs
5. Select CA Server
6. Select Template
7. Enter Password
```
**Problem:** Why re-enter info that's already in the CSR?

### New Way (Simple) ✅
```
1. Click "Import from CSR File..." button
2. Select CSR file
3. Select CA Server
4. Select Template
5. Enter Password
   ↓
DONE! (Hostname, Org, SANs from CSR)
```
**Benefit:** Only enter what's needed!

---

## For Sysadmins

### What You Need to Know:
1. **Template = Certificate Type** - Just pick the template you need
2. **WebServer template?** - You'll see wildcard checkbox if needed
3. **Non-web template?** - Just organization info (no hostname needed)
4. **Have a CSR?** - Click "Import from CSR" button, select file, done

### Common Tasks:

**"I need a cert for my web server"**
```
→ Template: WebServer
→ Hostname: www
→ Generate
```

**"I need a wildcard cert for all my subdomains"**
```
→ Template: WebServer
→ Check: Wildcard
→ Generate
```

**"I need to sign my application"**
```
→ Template: CodeSigning
→ Fill org info
→ Generate
```

**"I have a CSR file from my Linux server"**
```
→ Click: Import from CSR
→ Select file
→ Pick template
→ Generate
```

---

## Security Features (Automatic)

When you generate a certificate, the app automatically:

✅ **Validates your password** - Rejects weak/common passwords  
✅ **Matches template to type** - Prevents invalid certificates  
✅ **Sets correct OIDs** - Based on template  
✅ **Prevents injection attacks** - All inputs validated  
✅ **Uses SecureString** - Passwords cleared from memory  
✅ **Validates CSR files** - Checks format before submission  

**You don't have to think about security - it's built in!** 🔒

---

## Field Descriptions

### Required Always:
- **CA Server** - Your organization's Certificate Authority server
- **Template** - Certificate template (determines type and usage)
- **Password** - Strong password to protect the PFX file

### Required for Web Certificates:
- **Hostname** - Server name (e.g., "www", "api", "mail")
- **FQDN** - Auto-generated (hostname.company.com)
- **SANs** - Additional DNS names and IP addresses

### Required for All (except CSR import):
- **Location** - City name
- **State** - 2-letter state code
- **Company** - Company domain name
- **OU** - Department or organizational unit

### Optional:
- **Wildcard** - Check for wildcard certificates (*.domain.com)
- **Extract PEM/KEY** - Export certificate in PEM format
- **Extract CA Bundle** - Export certificate chain

---

## Common Templates

| Template Name | What It's For | Hostname Needed? | Example Use |
|--------------|---------------|------------------|-------------|
| WebServer | Web servers, SSL/TLS | ✓ Yes | www.company.com, api.company.com |
| CodeSigning | Software signing | ✗ No | Signing executables, scripts |
| User / ClientAuth | VPN, client authentication | ✗ No | User authentication, VPN certs |
| EmailProtection | S/MIME email encryption | ✗ No | Email signing and encryption |
| (Custom) | Other uses | Maybe | Depends on template |

---

## Tips & Best Practices

### 1. Template Selection
- **Know your template names** - Ask your CA admin if unsure
- **Template determines everything** - Type, OIDs, key usage all automatic
- **Can't find your template?** - Type it manually in the Template dropdown

### 2. Wildcard Certificates
- **Only for web servers** - Checkbox only appears for WebServer templates
- **Covers all subdomains** - `*.company.com` covers www, api, mail, etc.
- **Hostname field disabled** - Not needed for wildcards

### 3. Passwords
- **Minimum 8 characters** - App enforces this
- **Must include:** Uppercase, lowercase, numbers
- **Avoid common passwords** - "Password1", "Admin123", etc. are blocked
- **Strength indicator** - Shows Weak/Medium/Strong as you type

### 4. CSR Import
- **CSR has everything** - Hostname, org, SANs already in the file
- **Only need 3 things** - CA Server, Template, Password
- **Generated elsewhere?** - Perfect for certs from Linux, Java, etc.

---

## What Was Removed (And Why)

### ❌ Certificate Type Radio Buttons
**Before:** 7 radio buttons (Standard, Wildcard, ClientAuth, CodeSigning, Email, Custom, FromCSR)  
**Why removed:** 
- Confusing - duplicated template selection
- Error-prone - could mismatch with template
- Not how sysadmins think

**Replaced with:**
- Template selection (what sysadmins already know)
- Simple wildcard checkbox (only when applicable)
- Import from CSR button (clearer workflow)

### Benefits:
- ✅ **75% fewer UI elements** - Simpler is better
- ✅ **Zero confusion** - One choice: template
- ✅ **Zero errors** - Can't create mismatches
- ✅ **Professional** - Enterprise-grade UX

---

## Progressive Disclosure

The UI shows only what's relevant:

### WebServer Template Selected:
```
✓ Wildcard checkbox appears
✓ Hostname field appears
✓ FQDN field appears
✓ SANs section appears
```

### CodeSigning Template Selected:
```
✗ Wildcard checkbox hidden
✗ Hostname field hidden
✗ FQDN field hidden
✗ SANs section hidden
✓ Organization fields still shown
```

### CSR File Imported:
```
✗ Wildcard checkbox hidden
✗ Hostname fields hidden
✗ Organization fields hidden
✗ SANs section hidden
✓ CSR file path shown
✓ Only CA, Template, Password needed
```

**The UI adapts to what you're doing!** 🎯

---

## Error Prevention

The app prevents errors before they happen:

### Can't Generate Until:
- ✓ CA Server selected
- ✓ Template selected
- ✓ Password entered (strong enough)
- ✓ Passwords match (if confirmation enabled)
- ✓ Required fields filled (based on template)

### Validation Happens:
- ✓ As you type (instant feedback)
- ✓ Before submission (final check)
- ✓ Template/type match validated
- ✓ OIDs validated for certificate type

---

## Summary

**The new workflow is:**
- ✅ **Simpler** - Fewer steps, fewer choices
- ✅ **Clearer** - Matches how sysadmins think
- ✅ **Safer** - Impossible to make mismatches
- ✅ **Faster** - Less typing, more automation
- ✅ **Professional** - Enterprise-grade interface

**Just pick your template and go!** 🚀

---

**For Detailed Security Information:** See `SECURITY_REVIEW.md`  
**For Implementation Details:** See `SIMPLIFIED_UI_GUIDE.md`  
**For Migration Guide:** See `CRITICAL_FIX_TEMPLATE_TYPE_MISMATCH.md`

---

**Document Version:** 2.0  
**Last Updated:** October 14, 2025  
**Status:** Production Ready


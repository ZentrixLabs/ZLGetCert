# Screenshot Guide for README

This guide describes what to capture for each screenshot needed in the README.

## Screenshot Checklist

### 1. `main-interface.png` - Main Certificate Generation Interface

**What to show:**
- Launch the application with an empty/default form
- Show the full main window
- Make sure these are visible:
  - Title bar with application name
  - Card-based layout with all sections
  - Certificate Template dropdown
  - Certificate Identity section
  - Organization Information section
  - Subject Alternative Names section
  - Password section
  - Generate Certificate button at bottom

**Tips:**
- Use a clean, default state
- Make sure Font Awesome icons are visible
- Window size should show everything without scrolling if possible

---

### 2. `validation-success.png` - Valid Form with Green Summary

**What to show:**
- Fill out the entire form with valid data:
  - Select a certificate template (e.g., "WebServer")
  - Enter hostname (e.g., "web01")
  - Fill in all organization fields (Location, State, Company, Department)
  - Add 2-3 SANs
  - Generate a strong password (use the Generate button)
- The validation summary card should be **GREEN** with checkmark
- Certificate subject preview should show the generated DN

**Tips:**
- Make sure the green validation summary is prominent
- Show the real-time certificate subject preview filled in
- Password strength meter should show "Strong" in green

---

### 3. `validation-errors.png` - Form with Validation Errors

**What to show:**
- Start with an empty form or partially filled
- Click in some fields and leave them empty (to trigger validation)
- Should show:
  - Red borders around invalid fields
  - Error messages below fields (e.g., "Hostname is required")
  - Red validation summary card with X icon
  - Required field indicators (asterisks)
  - List of issues in the validation summary

**Tips:**
- Don't fill in everything - show what errors look like
- Make sure at least 3-4 validation errors are visible
- Red validation summary should be prominent at top

---

### 4. `password-strength.png` - Password Generation & Strength

**What to show:**
- Focus on the Password section
- Click the "Generate" button to create a strong password
- Should show:
  - Generated password visible (use show/hide toggle if needed)
  - Password strength meter showing "Strong" in green/blue
  - The Generate button
  - Copy to clipboard button (if visible)
  - Password requirements text always visible

**Tips:**
- You might want to crop to just the Password section for clarity
- Show the strength meter prominently
- Generated password should be visible

---

### 5. `bulk-san-entry.png` - Bulk SAN Entry Dialog

**What to show:**
- Click "Add Multiple" button in the SAN section
- The dialog that opens should show:
  - Multi-line text box with several DNS/IP entries
  - Example: 
    ```
    www.example.com
    mail.example.com
    ftp.example.com
    192.168.1.100
    api.example.com
    ```
  - Add/OK button
  - Cancel button

**Tips:**
- Add 5-10 example entries to show the bulk capability
- Show the full dialog window
- Make it clear this is a separate dialog

---

### 6. `csr-import.png` - CSR Import Workflow

**What to show:**
- The main interface with focus on the CSR import section at the top
- Should show:
  - "Import & Sign CSR File..." button prominently
  - The explanatory text about importing existing CSRs
  - Either before import (showing the button) or after (showing fields hidden)

**Option A (Before):** Just show the prominent button at top  
**Option B (After):** Show how form fields are hidden after CSR import

**Tips:**
- Make it clear this is an alternate workflow from generating new certs
- The CSR import button should be easy to spot

---

### 7. `settings-panel.png` - Settings Configuration

**What to show:**
- Click the Settings button to open settings
- Show the Settings window/panel with:
  - Certificate Authority configuration section
  - File Paths section
  - Default Settings section
  - Logging section
  - Save/Cancel buttons

**Tips:**
- Show the full settings window
- Make sure all major configuration sections are visible
- You may need to scroll or expand sections

---

## General Screenshot Tips

1. **Resolution:** Take screenshots at 1920x1080 or higher for clarity
2. **Window Size:** Size the application window to show all content clearly
3. **Clean Desktop:** Close other applications, clean desktop background
4. **File Format:** Save as PNG for best quality
5. **File Naming:** Use exact filenames listed above
6. **No Personal Data:** Don't use real company names, real hostnames, or real passwords

## Example Test Data

Use these for consistency across screenshots:

- **Hostname:** `web01` or `testserver`
- **Domain:** `example.com`
- **Location:** `Dallas`
- **State:** `Texas`
- **Company:** `Example Corp`
- **Department:** `IT Department`
- **SANs:** 
  - `www.example.com`
  - `mail.example.com`
  - `ftp.example.com`
  - `192.168.1.100`

## After Taking Screenshots

1. Save all images to `docs/images/` directory
2. Verify file names match exactly (case-sensitive on Linux/Mac)
3. Check that images display correctly in the README (view on GitHub)
4. Optionally optimize images for web (keep under 500KB each)

## Optional: Optimize Images

After taking screenshots, you can optimize them:

```powershell
# Using Windows built-in tools or install ImageMagick
# Example with ImageMagick:
magick main-interface.png -quality 85 -resize 1920x main-interface.png
```

Or use online tools like TinyPNG for compression.


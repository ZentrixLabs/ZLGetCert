# ZLGetCert Configuration Examples

This directory contains example configuration files for different use cases and environments.

## Configuration Files

### enterprise-ad-config.json
**For Enterprise Active Directory environments**
- Uses network file shares for certificate storage
- Higher security settings (4096-bit keys, password confirmation required)
- Extended logging for audit trails
- Machine key set for server certificates

### development-config.json
**For Development environments**
- Local file storage in user documents
- Relaxed security settings for easier development
- Console logging enabled for debugging
- User key set for development certificates

### code-signing-config.json
**For Code Signing certificates**
- Code signing specific template and OIDs
- User-protected private keys
- Digital signature key usage only
- Secure storage location

### client-auth-config.json
**For Client Authentication certificates**
- User authentication specific template and OIDs
- User-protected private keys
- Client authentication OID
- User-specific storage location

## Usage

1. Copy the appropriate configuration file to your ZLGetCert directory
2. Rename it to `appsettings.json` (replacing the existing one)
3. Modify the values to match your environment:
   - Update CA server names
   - Change company information
   - Adjust file paths as needed
   - Modify certificate parameters for your requirements

## Environment Variables

The configuration files support environment variables in file paths:
- `%USERPROFILE%` - User's home directory
- `%APPDATA%` - Application data directory
- `%TEMP%` - Temporary files directory
- `%PROGRAMDATA%` - Program data directory

## Certificate Parameters

Each configuration includes different certificate parameters:

### Key Usage Flags
- `0xa0` - Digital Signature + Key Encipherment (Web Server)
- `0x80` - Digital Signature only (Code Signing, Client Auth)

### Enhanced Key Usage OIDs
- `1.3.6.1.5.5.7.3.1` - Server Authentication
- `1.3.6.1.5.5.7.3.2` - Client Authentication
- `1.3.6.1.5.5.7.3.3` - Code Signing
- `1.3.6.1.5.5.7.3.4` - Email Protection

### Key Specifications
- `1` - RSA
- `2` - DH (Diffie-Hellman)
- `3` - DSS (Digital Signature Standard)

## Customization

You can create your own configuration by:
1. Starting with one of the example files
2. Modifying the values to match your environment
3. Adding custom Enhanced Key Usage OIDs if needed
4. Adjusting security settings based on your requirements

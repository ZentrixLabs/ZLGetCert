# ZLGetCert

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)

A modern Windows WPF application that simplifies certificate requests from on-premises Certificate Authority (CA) without requiring PowerShell or command-line expertise. Features a clean, card-based UI with comprehensive configuration management.

**Built on .NET Framework 4.8** for maximum compatibility with legacy servers, OT (Operational Technology) environments, and air-gapped systems where newer .NET versions may not be available.

## Features

- **Modern UI**: Clean, card-based interface with professional Font Awesome icons
- **Multiple Certificate Types**: Support for Standard, Wildcard, and CSR-based certificates
- **Configurable Options**: Dynamic hash algorithms and log levels loaded from configuration
- **Centralized Logging**: Comprehensive logging to `C:\ProgramData\ZentrixLabs\ZLGetCert`
- **Environment Configuration**: Flexible configuration via `appsettings.json`
- **Built-in PEM/KEY Export**: Pure .NET implementation - no external dependencies required
- **Secure Password Handling**: User-configurable PFX passwords with secure storage
- **Certificate Chain Support**: Automatic root/intermediate certificate chain compilation
- **Settings Panel**: Toggleable settings with real-time configuration updates
- **JSON Validator**: Real-time validation with color-coded feedback and error details
- **Visual Feedback**: Status indicators and progress tracking
- **Legacy Compatibility**: .NET Framework 4.8 for OT/SCADA and older server environments

## Prerequisites

- **Windows Server 2016** or later (Windows Server 2012 R2 also supported)
- **.NET Framework 4.8** (included in Windows Server 2019+, downloadable for older versions)
- **Administrator privileges** (for certificate store operations)
- **No external dependencies** - PEM/KEY export built-in using .NET cryptography

### Why .NET Framework 4.8?

This application intentionally targets **.NET Framework 4.8** rather than modern .NET Core/.NET 6+ to ensure maximum compatibility with:

- **Legacy Servers**: Windows Server 2012 R2, 2016, 2019 environments
- **OT/SCADA Systems**: Operational Technology and industrial control systems
- **Air-Gapped Networks**: Environments where installing newer .NET runtimes is restricted
- **Enterprise Policies**: Organizations with strict server configuration requirements
- **Embedded Systems**: Windows-based controllers and specialized hardware

.NET Framework 4.8 ships with Windows Server 2019+ and is easily installed on older systems without requiring major OS updates.

## Quick Start

1. **Download**: Clone or download the repository
2. **Build**: Open `ZLGetCert.sln` in Visual Studio and build the solution
3. **Configure**: Run the application and configure your CA settings via Edit → Settings
4. **Generate**: Create your first certificate using the intuitive interface

## Installation

### Prerequisites
- Windows Server 2016 or later
- .NET Framework 4.8
- Visual Studio 2019 or later (for building from source)

### From Source
1. Clone the repository:
   ```bash
   git clone https://github.com/ZentrixLabs/ZLGetCert.git
   cd ZLGetCert
   ```

2. Open `ZLGetCert.sln` in Visual Studio 2019 or later

3. Build the solution:
   ```bash
   msbuild ZLGetCert.sln /p:Configuration=Release
   ```

4. Run the application from `ZLGetCert\bin\Release\ZLGetCert.exe`

### Pre-built Binaries
Download the latest release from the [Releases](https://github.com/ZentrixLabs/ZLGetCert/releases) page.

### Silent Installation (Enterprise Deployment)

The installer supports silent installation for automated deployments via tools like PDQ Deploy, SCCM, or Group Policy:

**Standard Silent Install (shows progress bar):**
```cmd
ZLGetCertInstaller.exe /SILENT /NORESTART
```

**Very Silent Install (no UI at all):**
```cmd
ZLGetCertInstaller.exe /VERYSILENT /NORESTART /SUPPRESSMSGBOXES
```

**Additional Options:**
- `/DIR="C:\Custom\Path"` - Specify custom install directory
- `/LOG="C:\Logs\install.log"` - Create installation log
- `/NOICONS` - Don't create start menu icons
- `/TASKS="desktopicon"` - Force create desktop icon

**Example Enterprise Deployment:**
```cmd
ZLGetCertInstaller.exe /VERYSILENT /NORESTART /SUPPRESSMSGBOXES /LOG="C:\Windows\Temp\ZLGetCert_install.log"
```

The installer requires administrator privileges, so ensure your deployment tools run with appropriate credentials.

## Configuration

The application uses `appsettings.json` for configuration. All UI options are dynamically loaded from configuration, eliminating hardcoded values:

```json
{
  "CertificateAuthority": {
    "Server": "your-ca-server.domain.com",
    "Template": "WebServerV2",
    "DefaultCompany": "your-domain.com",
    "DefaultOU": "IT"
  },
  "FilePaths": {
    "CertificateFolder": "C:\\ssl",
    "LogPath": "C:\\ProgramData\\ZentrixLabs\\ZLGetCert"
  },
  "DefaultSettings": {
    "KeyLength": 2048,
    "HashAlgorithm": "sha256",
    "DefaultPassword": "password",
    "RequirePasswordConfirmation": true,
    "AutoCleanup": true,
    "RememberPassword": false,
    "AvailableHashAlgorithms": ["sha256", "sha384", "sha512"]
  },
  "Logging": {
    "LogLevel": "Information",
    "LogToFile": true,
    "LogToConsole": false,
    "MaxLogFileSize": "10MB",
    "MaxLogFiles": 5,
    "AvailableLogLevels": ["Trace", "Debug", "Information", "Warning", "Error", "Fatal"]
  }
}
```

### Configuration Features
- **Dynamic Options**: Hash algorithms and log levels are loaded from configuration
- **No Hardcoded Values**: All UI options come from `appsettings.json`
- **Easy Customization**: Add/remove options by updating the configuration file
- **Environment-Specific**: Different settings for different deployment environments
- **JSON Validator**: Real-time validation with instant feedback and error details
- **Configuration Editor**: Direct JSON editing with syntax validation and safety checks

## Usage

### Getting Started
1. Launch ZLGetCert
2. Configure your CA settings using the Settings button
3. Select your certificate type and fill in the required information
4. Click "Generate Certificate"

### Standard Certificate Request
1. Select "Standard Certificate" radio button
2. Enter hostname in the Domain field
3. Add Subject Alternative Names (SANs) if needed
4. Configure organization information
5. Set PFX password
6. Click "Generate Certificate"

### Wildcard Certificate Request
1. Select "Wildcard Certificate" radio button
2. Enter wildcard domain (e.g., *.domain.com)
3. Configure location and company details
4. Set PFX password
5. Generate certificate

### CSR-Based Certificate Request
1. Select "From CSR" radio button
2. Browse to existing CSR file using the file picker
3. Set PFX password
4. Submit to CA

### Settings Configuration
- Click the Settings button to access configuration
- Modify CA server settings, file paths, and default values
- Configure logging options and hash algorithms
- Settings are saved automatically and applied immediately

### Advanced Configuration Editing
- Go to Edit → Configuration Editor... for direct JSON editing
- Real-time JSON validation with color-coded feedback:
  - ✅ **Green**: Valid JSON - Ready to save
  - ⚠️ **Yellow**: Configuration issues - JSON valid but has problems
  - ❌ **Red**: Invalid JSON - Syntax errors detected
- Detailed error messages with specific validation issues
- Safety checks prevent saving invalid configurations
- Restart notification when changes are applied

## Certificate Types Supported

- **Standard**: Regular hostname certificates with multiple SANs
- **Wildcard**: Wildcard domain certificates (*.domain.com)
- **From CSR**: Submit existing CSR files to CA

## PEM/KEY Export (Built-in)

The application includes a **pure .NET implementation** for PEM and KEY file extraction - **no external dependencies or OpenSSL required**!

Features:
- **Built-in .NET cryptography** - PEM and KEY extraction using native .NET Framework 4.8
- **Zero external dependencies** - works out of the box on any Windows system
- **Generate certificate chains** - automatic root/intermediate certificate bundle export
- **PKCS#1 format** - fully compatible with Apache, NGINX, HAProxy, and all web servers
- **RSA key support** - handles all common SSL/TLS certificate key sizes (2048-bit, 4096-bit)
- **Air-gap compatible** - perfect for isolated OT/SCADA environments

The pure .NET implementation ensures compatibility with air-gapped and restricted environments where installing third-party tools like OpenSSL is not permitted. All cryptographic operations are performed using trusted Microsoft .NET Framework libraries.

## Logging

All operations are logged to `C:\ProgramData\ZentrixLabs\ZLGetCert` with:
- Detailed operation logs
- Error tracking
- Audit trails
- Configurable log levels

## Security

- Passwords handled as `SecureString` in memory
- Secure password storage in configuration
- Password masking in UI and logs
- Automatic memory cleanup
- Built-in security warnings for unencrypted key files

## Deployment in Restricted Environments

ZLGetCert is specifically designed for deployment in secure and restricted environments:

### Air-Gapped Networks
- **No Internet Required**: All operations are performed locally
- **No External Dependencies**: Pure .NET Framework 4.8 - no third-party binaries
- **Offline Installation**: Deploy via file copy or internal package management

### OT/SCADA Environments
- **Minimal Footprint**: Small executable with no external dependencies
- **No Registry Changes**: (except for .NET Framework 4.8 if not already installed)
- **Predictable Behavior**: No automatic updates or telemetry
- **Logging Control**: All logs stored locally, configurable retention

### Enterprise Compliance
- **Approved Framework**: .NET Framework 4.8 is typically pre-approved in most enterprises
- **No Elevated Privileges for App**: Only requires admin for certificate store operations
- **Auditable**: Comprehensive logging of all operations
- **Configuration as Code**: JSON-based configuration for version control

### Installation in Secure Environments
1. Transfer the application files via approved methods (removable media, internal repository)
2. Verify .NET Framework 4.8 is installed (included in Windows Server 2019+)
3. Configure `appsettings.json` with your CA details
4. Run with administrator privileges for certificate operations

## Development

### Project Structure
```
ZLGetCert/
├── Models/           # Data models and entities (AppConfiguration, etc.)
├── ViewModels/      # MVVM ViewModels (MainViewModel, SettingsViewModel)
├── Views/           # WPF XAML views (MainWindow, AboutWindow)
├── Services/        # Business logic services (Configuration, Logging, etc.)
├── Utilities/       # Helper classes (VersionHelper, etc.)
├── Enums/           # Enumerations (LogLevel, CertificateType)
├── Styles/          # XAML styles and templates (CommonStyles.xaml)
├── Converters/      # Value converters for data binding
├── Fonts/           # Font Awesome icon fonts
└── appsettings.json # Application configuration
```

### Building
```bash
# Debug build
msbuild ZLGetCert.sln /p:Configuration=Debug

# Release build
msbuild ZLGetCert.sln /p:Configuration=Release
```

### UI Development
The application uses modern WPF patterns:
- **MVVM Architecture**: Clean separation of concerns
- **Data Binding**: Two-way binding with converters
- **Custom Styles**: Consistent theming via CommonStyles.xaml
- **Card-Based Layout**: Modern UI with visual hierarchy
- **Configuration-Driven**: All options loaded from appsettings.json
- **Professional Icons**: Font Awesome 7 Pro icon integration

### Key Technologies
- **.NET Framework 4.8**: Target framework (intentionally chosen for legacy system compatibility)
- **WPF**: Windows Presentation Foundation
- **MVVM Pattern**: Model-View-ViewModel architecture
- **System.Security.Cryptography**: Built-in .NET cryptography (RSA, X509 certificates)
- **Custom ASN.1/DER Encoder**: Pure .NET PKCS#1 private key encoding
- **Newtonsoft.Json**: Configuration serialization
- **NLog**: Logging framework
- **Font Awesome 7 Pro**: Professional icon library

## Documentation

- **[User Guides](docs/user-guides/)** - Feature-specific documentation and improvements
- **[Development Docs](docs/development/)** - Technical implementation details and architecture
- **[Configuration Examples](ZLGetCert/examples/)** - Sample configuration files for different environments

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

### Development Guidelines

- Follow the existing code style and patterns
- Add appropriate error handling and logging
- Update documentation for new features
- Test your changes thoroughly
- Ensure all existing tests pass

## Roadmap

- [ ] Support for additional certificate types
- [ ] Certificate renewal automation
- [ ] Certificate expiration monitoring and alerts
- [ ] Multi-language support
- [ ] Plugin architecture for custom validators
- [ ] Encrypted private key export (password-protected .key files)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Screenshots

The application features a modern, card-based interface with:
- Clean visual hierarchy with grouped form sections
- Professional Font Awesome icons throughout
- Toggleable settings panel with real-time configuration
- Visual status indicators and progress tracking
- Consistent styling across all UI elements

## Support

For issues and questions:
- Create an issue in the GitHub repository
- Check the logs in `C:\ProgramData\ZentrixLabs\ZLGetCert`
- Review the configuration in `appsettings.json`
- Verify .NET Framework 4.8 is installed on your system

## Recent Updates

- **Professional Icon Integration**: Font Awesome 7 Pro icons throughout the application
- **Pure .NET PEM/KEY Export**: Built-in certificate extraction - **zero external dependencies**!
  - Native .NET Framework 4.8 cryptography - no OpenSSL installation needed
  - PKCS#1 RSA private key encoding using custom ASN.1/DER implementation
  - Certificate chain extraction for intermediate/root certificates
  - Works out-of-the-box on any Windows system with .NET 4.8
  - Perfect for air-gapped, OT/SCADA, and restricted environments
- **UI Overhaul**: Modern card-based layout with improved visual hierarchy
- **Configuration Management**: All options now loaded from appsettings.json
- **Settings Panel**: Toggleable full-width settings with real-time updates
- **JSON Validator**: Real-time validation with color-coded feedback and error details
- **Configuration Editor**: Direct JSON editing with syntax validation and safety checks
- **Users Guide**: Comprehensive documentation with examples and troubleshooting
- **Visual Improvements**: Enhanced styling, better form grouping, and status indicators
- **Code Quality**: Removed hardcoded values, improved maintainability

## About

ZLGetCert is developed by [ZentrixLabs](https://zentrixlabs.net/) to simplify certificate management in enterprise and OT environments.

---

© 2025 ZentrixLabs. All rights reserved.
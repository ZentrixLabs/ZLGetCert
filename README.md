# ZLGetCert

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)

A modern Windows WPF application that simplifies certificate requests from on-premises Certificate Authority (CA) without requiring PowerShell or command-line expertise. Features a clean, card-based UI with comprehensive configuration management.

## Features

- **Modern UI**: Clean, card-based interface with improved UX and visual hierarchy
- **Multiple Certificate Types**: Support for Standard, Wildcard, and CSR-based certificates
- **Configurable Options**: Dynamic hash algorithms and log levels loaded from configuration
- **Centralized Logging**: Comprehensive logging to `C:\ProgramData\ZentrixLabs\ZLGetCert`
- **Environment Configuration**: Flexible configuration via `appsettings.json`
- **OpenSSL Integration**: Optional PEM/KEY extraction when OpenSSL is available
- **Secure Password Handling**: User-configurable PFX passwords with secure storage
- **Certificate Chain Support**: Automatic root/intermediate certificate chain compilation
- **Settings Panel**: Toggleable settings with real-time configuration updates
- **JSON Validator**: Real-time validation with color-coded feedback and error details
- **Visual Feedback**: Status indicators and progress tracking

## Prerequisites

- **Windows Server 2016** or later
- **.NET Framework 4.8**
- **OpenSSL for Windows** (optional, for PEM/KEY extraction)
- **Administrator privileges** (for certificate store operations)

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
  "OpenSSL": {
    "ExecutablePath": "",
    "AutoDetect": true,
    "CommonPaths": [
      "C:\\Program Files\\OpenSSL-Win64\\bin\\openssl.exe",
      "C:\\Program Files (x86)\\OpenSSL-Win32\\bin\\openssl.exe"
    ]
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
2. Configure your CA settings using the ⚙️ Settings button
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
- Click the ⚙️ Settings button to access configuration
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

## OpenSSL Integration

When OpenSSL is detected, the application can:
- Extract PEM and KEY files from PFX certificates
- Generate certificate chains (root/intermediate)
- Clean up temporary files automatically

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

### Key Technologies
- **.NET Framework 4.8**: Target framework
- **WPF**: Windows Presentation Foundation
- **MVVM Pattern**: Model-View-ViewModel architecture
- **Newtonsoft.Json**: Configuration serialization
- **NLog**: Logging framework

## Contributing

We welcome contributions! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Add tests for new functionality
5. Commit your changes (`git commit -m 'Add some amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Development Guidelines

- Follow the existing code style and patterns
- Add appropriate error handling and logging
- Update documentation for new features
- Test your changes thoroughly
- Ensure all existing tests pass

## Roadmap

- [ ] Support for additional certificate types
- [ ] Enhanced OpenSSL integration
- [ ] Certificate renewal automation
- [ ] Multi-language support
- [ ] Plugin architecture for custom validators

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Screenshots

The application features a modern, card-based interface with:
- Clean visual hierarchy with grouped form sections
- Toggleable settings panel with real-time configuration
- Visual status indicators and progress tracking
- Consistent styling across all UI elements

## Support

For issues and questions:
- Create an issue in the GitHub repository
- Check the logs in `C:\ProgramData\ZentrixLabs\ZLGetCert`
- Review the configuration in `appsettings.json`
- Verify OpenSSL installation if using PEM/KEY extraction

## Recent Updates

- **UI Overhaul**: Modern card-based layout with improved visual hierarchy
- **Configuration Management**: All options now loaded from appsettings.json
- **Settings Panel**: Toggleable full-width settings with real-time updates
- **JSON Validator**: Real-time validation with color-coded feedback and error details
- **Configuration Editor**: Direct JSON editing with syntax validation and safety checks
- **Users Guide**: Comprehensive documentation with examples and troubleshooting
- **Visual Improvements**: Enhanced styling, better form grouping, and status indicators
- **Code Quality**: Removed hardcoded values, improved maintainability

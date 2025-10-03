# ZLGetCert

A Windows WPF application that simplifies certificate requests from on-premises Certificate Authority (CA) without requiring PowerShell or command-line expertise.

## Features

- **User-Friendly GUI**: Intuitive WPF interface for certificate management
- **Multiple Certificate Types**: Support for Standard, Wildcard, and CSR-based certificates
- **Centralized Logging**: Comprehensive logging to `C:\ProgramData\ZentrixLabs\ZLGetCert`
- **Environment Configuration**: Flexible configuration via `appsettings.json`
- **OpenSSL Integration**: Optional PEM/KEY extraction when OpenSSL is available
- **Secure Password Handling**: User-configurable PFX passwords with secure storage
- **Certificate Chain Support**: Automatic root/intermediate certificate chain compilation

## Prerequisites

- **Windows Server 2016** or later
- **.NET Framework 4.8**
- **OpenSSL for Windows** (optional, for PEM/KEY extraction)
- **Administrator privileges** (for certificate store operations)

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/your-org/ZLGetCert.git
   ```

2. Open `ZLGetCert.sln` in Visual Studio 2019 or later

3. Build the solution:
   ```bash
   msbuild ZLGetCert.sln /p:Configuration=Release
   ```

4. Run the application from `ZLGetCert\bin\Release\ZLGetCert.exe`

## Configuration

The application uses `appsettings.json` for configuration. Key settings include:

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
    "AutoDetect": true
  }
}
```

## Usage

### Standard Certificate Request
1. Launch ZLGetCert
2. Select "Standard Certificate"
3. Enter hostname, location, and SANs
4. Set PFX password
5. Click "Generate Certificate"

### Wildcard Certificate Request
1. Select "Wildcard Certificate"
2. Enter wildcard domain (e.g., *.domain.com)
3. Configure location and company details
4. Set PFX password
5. Generate certificate

### CSR-Based Certificate Request
1. Select "From CSR"
2. Browse to existing CSR file
3. Set PFX password
4. Submit to CA

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
├── Models/           # Data models and entities
├── ViewModels/      # MVVM ViewModels
├── Views/           # WPF XAML views
├── Services/        # Business logic services
├── Utilities/       # Helper classes
├── Enums/           # Enumerations
└── Tests/           # Unit tests
```

### Building
```bash
# Debug build
msbuild ZLGetCert.sln /p:Configuration=Debug

# Release build
msbuild ZLGetCert.sln /p:Configuration=Release
```

### Testing
```bash
# Run unit tests
dotnet test ZLGetCert.Tests/ZLGetCert.Tests.csproj
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues and questions:
- Create an issue in the GitHub repository
- Check the logs in `C:\ProgramData\ZentrixLabs\ZLGetCert`
- Review the configuration in `appsettings.json`

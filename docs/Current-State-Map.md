# Current State Map (Pre-Refactor)

## Entry Points

- **WPF Application Entry Point**: `ZLGetCert/App.xaml` with `StartupUri="Views/MainWindow.xaml"` (see `ZLGetCert/App.xaml:4`)
- **Application Class**: `ZLGetCert/App.xaml.cs` - `App` class with `OnStartup` override that initializes logging and ensures directories exist (see `ZLGetCert/App.xaml.cs:12-33`)
- **Main Window**: `ZLGetCert/Views/MainWindow.xaml` and `ZLGetCert/Views/MainWindow.xaml.cs` - sets `DataContext = new MainViewModel()` (see `ZLGetCert/Views/MainWindow.xaml.cs:15`)
- **Main ViewModel**: `ZLGetCert/ViewModels/MainViewModel.cs` - orchestrates the UI and certificate generation flow (see `ZLGetCert/ViewModels/MainViewModel.cs:33-69`)

## Request Flow (As Implemented Today)

1. **User clicks "Generate Certificate" button** → `MainViewModel.GenerateCertificateCommand` executes (see `ZLGetCert/ViewModels/MainViewModel.cs:244,354-419`)

2. **Validation**: `MainViewModel.GenerateCertificate()` calls `ValidationHelper.ValidateCertificateRequest()` (see `ZLGetCert/ViewModels/MainViewModel.cs:362`)

3. **Certificate Request Model Creation**: `CertificateRequestViewModel.ToCertificateRequest()` converts ViewModel to `Models.CertificateRequest` (see `ZLGetCert/ViewModels/MainViewModel.cs:373`)

4. **Service Call**: `CertificateService.GenerateCertificate(request)` is invoked (see `ZLGetCert/ViewModels/MainViewModel.cs:378-379` and `ZLGetCert/Services/CertificateService.cs:584-682`)

5. **Certificate Type Routing**: Based on `CertificateRequest.Type`, calls one of:
   - `GenerateStandardCertificate()` (see `ZLGetCert/Services/CertificateService.cs:687-709`)
   - `GenerateWildcardCertificate()` (see `ZLGetCert/Services/CertificateService.cs:714-736`)
   - `GenerateFromCSR()` (see `ZLGetCert/Services/CertificateService.cs:741-758`)

6. **For Standard/Wildcard certificates**:
   - `GetFilePaths()` generates file paths (see `ZLGetCert/Services/CertificateService.cs:1404-1417`)
   - `GenerateInfContent()` or `GenerateWildcardInfContent()` creates INF file (see `ZLGetCert/Services/CertificateService.cs:763-856`)
   - `File.WriteAllText()` writes INF file (see `ZLGetCert/Services/CertificateService.cs:693,720`)
   - `CreateCSR()` invokes `certreq.exe -new` (see `ZLGetCert/Services/CertificateService.cs:861-921`)
   - `SubmitToCA()` invokes `certreq.exe -config -submit -attrib` (see `ZLGetCert/Services/CertificateService.cs:926-1017`)

7. **Certificate Processing**: `ProcessCertificate()` method (see `ZLGetCert/Services/CertificateService.cs:1022-1152`):
   - `ImportCertificate()` adds CER to LocalMachine\My store (see `ZLGetCert/Services/CertificateService.cs:1205-1221`)
   - `FindCertificateInStore()` locates certificate by thumbprint or subject (see `ZLGetCert/Services/CertificateService.cs:1226-1283`)
   - `RepairCertificate()` invokes `certutil.exe -repairstore` to associate private key (see `ZLGetCert/Services/CertificateService.cs:1288-1336`)
   - `ExportPfxCertificate()` exports PFX using `X509Certificate2.Export()` (see `ZLGetCert/Services/CertificateService.cs:1341-1361`)

8. **PEM/KEY Export** (if requested): `ExtractPemAndKeyFiles()` calls `PemExportService.ExtractPemAndKey()` (see `ZLGetCert/Services/CertificateService.cs:1366-1399` and `ZLGetCert/Services/PemExportService.cs:41-121`)

9. **Cleanup**: `CleanupTemporaryFiles()` deletes INF, CSR, RSP files if `AutoCleanup` is enabled (see `ZLGetCert/Services/CertificateService.cs:1453-1471`)

10. **Result Return**: `CertificateInfo` object returned to `MainViewModel`, which updates UI with status message (see `ZLGetCert/ViewModels/MainViewModel.cs:381-408`)

## CA Interaction

- **CA Discovery**: `CertificateService.GetAvailableCAs()` uses `certutil.exe -dump` (primary) or `certutil.exe -ADCA` (fallback) (see `ZLGetCert/Services/CertificateService.cs:156-216,221-269`)

- **Template Discovery**: `CertificateService.GetAvailableTemplates()` uses `certutil.exe -CATemplates -config <ca-config>` (see `ZLGetCert/Services/CertificateService.cs:373-450`)

- **CSR Creation**: `CertificateService.CreateCSR()` invokes `certreq.exe -new <inf-path> <csr-path>` (see `ZLGetCert/Services/CertificateService.cs:861-921`)

- **CA Submission**: `CertificateService.SubmitToCA()` invokes `certreq.exe -config <ca-config> -submit -attrib CertificateTemplate:<template> <csr-path> <cer-path> <pfx-path>` (see `ZLGetCert/Services/CertificateService.cs:926-1017`)
  - Inputs: CSR file path, CA server name, template name
  - Outputs: CER file and PFX file (written by certreq.exe)

- **Certificate Store Repair**: `CertificateService.RepairCertificate()` invokes `certutil.exe -repairstore my <thumbprint>` (see `ZLGetCert/Services/CertificateService.cs:1288-1336`)

- **Argument Validation**: All external process arguments are validated using `ProcessArgumentValidator` to prevent command injection (see `ZLGetCert/Services/CertificateService.cs:866-867,931-934,1288-1293`)

## Export Behavior

- **Output Folder**: Determined by `AppConfiguration.FilePaths.CertificateFolder` from configuration (see `ZLGetCert/Services/CertificateService.cs:1404-1417` and `ZLGetCert/Services/ConfigurationService.cs:109-114`)

- **File Naming**: Files named using `{certificateName}.{extension}` where `certificateName` comes from `CertificateRequest.CertificateName` (see `ZLGetCert/Services/CertificateService.cs:1404-1417`)

- **Overwrite Behavior**: 
  - `File.WriteAllText()` and `File.WriteAllBytes()` overwrite existing files without prompting (standard .NET behavior)
  - No explicit overwrite checks or prompts are performed
  - CER and PFX files are overwritten by certreq.exe during submission (see `ZLGetCert/Services/CertificateService.cs:702,729,751`)
  - PFX export uses `File.WriteAllBytes()` which overwrites (see `ZLGetCert/Services/CertificateService.cs:1352`)
  - PEM/KEY export uses `File.WriteAllText()` which overwrites (see `ZLGetCert/Services/PemExportService.cs:253,300`)

- **File Formats Written**:
  - **CER**: Written by certreq.exe during CA submission (see `ZLGetCert/Services/CertificateService.cs:702,729,751`)
  - **PFX**: Exported via `X509Certificate2.Export(X509ContentType.Pkcs12)` (see `ZLGetCert/Services/CertificateService.cs:1351`)
  - **PEM**: Extracted via `PemExportService.ExtractPemAndKey()` using pure .NET (see `ZLGetCert/Services/PemExportService.cs:246-255`)
  - **KEY**: Extracted as unencrypted RSA private key in PKCS#1 format (see `ZLGetCert/Services/PemExportService.cs:260-302`)
  - **CA Bundle**: Optional chain extraction via `PemExportService.ExtractCertificateChain()` (see `ZLGetCert/Services/PemExportService.cs:131-196`)

## Configuration Sources

- **Configuration Service**: `ZLGetCert/Services/ConfigurationService.cs` loads config with priority order (see `ZLGetCert/Services/ConfigurationService.cs:55-93`):
  1. User config: `%APPDATA%\ZentrixLabs\ZLGetCert\appsettings.json` (see `ZLGetCert/Services/ConfigurationService.cs:28-30`)
  2. Default config: `{ApplicationDirectory}\appsettings.json` (see `ZLGetCert/Services/ConfigurationService.cs:24-25`)
  3. Hardcoded defaults: `GetDefaultConfiguration()` method (see `ZLGetCert/Services/ConfigurationService.cs:128-167`)

- **Key Configuration Keys** (from `ZLGetCert/appsettings.json` and `ZLGetCert/Models/AppConfiguration.cs`):
  - **CA Settings**: `CertificateAuthority.Server`, `CertificateAuthority.Template`, `CertificateAuthority.DefaultCompany`, `CertificateAuthority.DefaultOU`, `CertificateAuthority.DefaultLocation`, `CertificateAuthority.DefaultState`
  - **Output Settings**: `FilePaths.CertificateFolder`, `FilePaths.LogPath`, `FilePaths.TempPath`
  - **Crypto Settings**: `DefaultSettings.KeyLength`, `DefaultSettings.HashAlgorithm`, `CertificateParameters.KeySpec`, `CertificateParameters.ProviderName`, `CertificateParameters.ProviderType`, `CertificateParameters.KeyUsage`, `CertificateParameters.EnhancedKeyUsageOIDs`, `CertificateParameters.Exportable`, `CertificateParameters.MachineKeySet`
  - **Other Settings**: `DefaultSettings.AutoCleanup`, `Logging.LogLevel`, `Logging.LogToFile`, `Logging.MaxLogFiles`

- **Environment Variable Expansion**: File paths support environment variables like `%USERPROFILE%`, `%APPDATA%`, `%TEMP%` (see `ZLGetCert/Services/ConfigurationService.cs:108-114,224-238`)

## Logging

- **Logging Service**: `ZLGetCert/Services/LoggingService.cs` - singleton using NLog (see `ZLGetCert/Services/LoggingService.cs:14-212`)

- **Initialization**: Logging initialized in `App.OnStartup()` (see `ZLGetCert/App.xaml.cs:17-18`) and configured via `ConfigurationService` (see `ZLGetCert/Services/LoggingService.cs:34-45`)

- **Log Destination**: 
  - File: `{LogPath}\ZLGetCert-{shortdate}.log` with daily archiving (see `ZLGetCert/Services/LoggingService.cs:58-67`)
  - Console: Optional via `Logging.LogToConsole` config (see `ZLGetCert/Services/LoggingService.cs:74-83`)

- **Log Structure**: Format `{longdate}|{level:uppercase=true}|{logger}|{message} {exception:format=tostring}` (see `ZLGetCert/Services/LoggingService.cs:61`)

- **Log Levels**: Trace, Debug, Information, Warning, Error, Fatal (see `ZLGetCert/Services/LoggingService.cs:110-129`)

- **Log Rotation**: Daily archiving with `MaxLogFiles` retention (see `ZLGetCert/Services/LoggingService.cs:63-65`)

- **NLog Config**: `ZLGetCert/NLog.config` file exists but logging is configured programmatically in code (see `ZLGetCert/Services/LoggingService.cs:50-87`)

## Notes / Risks

- **Certificate Store Dependency**: Core logic depends on Windows certificate store (`LocalMachine\My`). Extracting core will require abstracting store operations (see `ZLGetCert/Services/CertificateService.cs:1210,1230,1164`)

- **Process Invocation**: Multiple external process calls (`certreq.exe`, `certutil.exe`) are tightly integrated. Core extraction needs process abstraction layer (see `ZLGetCert/Services/CertificateService.cs:869-881,965-977,1295-1307`)

- **File System Operations**: Direct file I/O scattered throughout. Core extraction should centralize file operations behind an interface (see `ZLGetCert/Services/CertificateService.cs:693,1352`, `ZLGetCert/Services/PemExportService.cs:253,300`)

- **Configuration Service Coupling**: Configuration loaded via singleton `ConfigurationService.Instance`. Core extraction needs configuration injection/abstraction (see `ZLGetCert/Services/CertificateService.cs:25,32,590`)

- **Synchronous Process Execution**: All external processes use `Process.WaitForExit()` with timeouts. Core extraction should consider async patterns (see `ZLGetCert/Services/CertificateService.cs:887-898,983-994,1312-1323`)

- **Argument Escaping Logic**: Custom `EscapeArgument()` and `BuildArgumentString()` methods used throughout. Core extraction needs to preserve this security logic (see `ZLGetCert/Services/CertificateService.cs:41-67`)

- **Certificate Thumbprint Matching**: Complex logic for finding certificates by thumbprint vs. subject search. Critical for preventing SAN mismatches (see `ZLGetCert/Services/CertificateService.cs:1226-1283,1105-1132`)

- **Duplicate Certificate Removal**: `RemoveDuplicateCertificates()` deletes existing certs from store before generation to prevent SAN mismatches. Store-specific behavior to abstract (see `ZLGetCert/Services/CertificateService.cs:1160-1200`)

- **PFX Thumbprint Validation**: Post-export validation compares PFX thumbprint to CER thumbprint to catch certificate store mismatches. Security-critical validation to preserve (see `ZLGetCert/Services/CertificateService.cs:1105-1132`)

- **PEM Export Security**: Unencrypted private key export with restrictive file permissions. Security-sensitive operation that requires careful handling (see `ZLGetCert/Services/PemExportService.cs:88-89,470-515`)


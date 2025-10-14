using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using ZLGetCert.Models;
using ZLGetCert.Enums;

namespace ZLGetCert.Services
{
    /// <summary>
    /// Service for certificate generation and management
    /// </summary>
    public class CertificateService
    {
        private static readonly Lazy<CertificateService> _instance = new Lazy<CertificateService>(() => new CertificateService());
        public static CertificateService Instance => _instance.Value;

        private readonly LoggingService _logger;
        private readonly ConfigurationService _configService;
        private readonly OpenSSLService _openSSLService;

        private CertificateService()
        {
            _logger = LoggingService.Instance;
            _configService = ConfigurationService.Instance;
            _openSSLService = OpenSSLService.Instance;
        }

        /// <summary>
        /// Get available certificate templates from the CA
        /// </summary>
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

                var caConfig = $"{server}\\{server.Split('.')[0].ToUpper()}";
                
                _logger.LogInfo("Querying available templates from CA: {0}", caConfig);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "certutil.exe",
                        Arguments = $"-CATemplates -config \"{caConfig}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit(30000); // 30 second timeout

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Failed to query templates: {0}", error);
                    return templates;
                }

                // Parse the output
                templates = ParseTemplateOutput(output);
                
                _logger.LogInfo("Found {0} available templates", templates.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying certificate templates");
            }

            return templates;
        }

        /// <summary>
        /// Parse certutil template output
        /// </summary>
        private List<CertificateTemplate> ParseTemplateOutput(string output)
        {
            var templates = new List<CertificateTemplate>();

            try
            {
                // Split output into lines
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                CertificateTemplate currentTemplate = null;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Skip empty lines and separator lines
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("---"))
                        continue;

                    // Check for template name (usually starts at the beginning of a line)
                    // Format is typically: TemplateName -- DisplayName
                    if (!trimmedLine.StartsWith(" ") && trimmedLine.Contains("--"))
                    {
                        if (currentTemplate != null)
                        {
                            templates.Add(currentTemplate);
                        }

                        var parts = trimmedLine.Split(new[] { "--" }, StringSplitOptions.None);
                        currentTemplate = new CertificateTemplate
                        {
                            Name = parts[0].Trim(),
                            DisplayName = parts.Length > 1 ? parts[1].Trim() : parts[0].Trim()
                        };
                    }
                    // Alternative format: just template name
                    else if (!trimmedLine.StartsWith(" ") && currentTemplate == null)
                    {
                        currentTemplate = new CertificateTemplate
                        {
                            Name = trimmedLine,
                            DisplayName = trimmedLine
                        };
                    }
                    // Check for OID
                    else if (trimmedLine.StartsWith("Template OID:") || trimmedLine.Contains("OID ="))
                    {
                        var match = Regex.Match(trimmedLine, @"(\d+\.)+\d+");
                        if (match.Success && currentTemplate != null)
                        {
                            currentTemplate.OID = match.Value;
                        }
                    }
                    // Check for version
                    else if (trimmedLine.Contains("Version") && currentTemplate != null)
                    {
                        var match = Regex.Match(trimmedLine, @"\d+");
                        if (match.Success)
                        {
                            int.TryParse(match.Value, out int version);
                            currentTemplate.Version = version;
                        }
                    }
                }

                // Add the last template
                if (currentTemplate != null)
                {
                    templates.Add(currentTemplate);
                }

                // If the above parsing didn't work, try a simpler approach
                // Some certutil versions just list template names one per line
                if (templates.Count == 0)
                {
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmedLine) && 
                            !trimmedLine.Contains("===") &&
                            !trimmedLine.Contains("---") &&
                            !trimmedLine.StartsWith("Certificate") &&
                            !trimmedLine.StartsWith("Template"))
                        {
                            templates.Add(new CertificateTemplate
                            {
                                Name = trimmedLine,
                                DisplayName = trimmedLine
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing template output");
            }

            return templates;
        }

        /// <summary>
        /// Generate a certificate based on the request
        /// </summary>
        public CertificateInfo GenerateCertificate(Models.CertificateRequest request)
        {
            try
            {
                _logger.LogInfo("Starting certificate generation for {0} ({1})", request.CertificateName, request.Type);

                var config = _configService.GetConfiguration();
                var certificateInfo = new CertificateInfo();

                // Ensure certificate folder exists
                if (!Directory.Exists(config.FilePaths.CertificateFolder))
                {
                    Directory.CreateDirectory(config.FilePaths.CertificateFolder);
                }

                switch (request.Type)
                {
                    case CertificateType.Standard:
                        certificateInfo = GenerateStandardCertificate(request, config);
                        break;
                    case CertificateType.Wildcard:
                        certificateInfo = GenerateWildcardCertificate(request, config);
                        break;
                    case CertificateType.FromCSR:
                        certificateInfo = GenerateFromCSR(request, config);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported certificate type: {request.Type}");
                }

                if (certificateInfo.IsValid && request.ExtractPemKey)
                {
                    ExtractPemAndKeyFiles(certificateInfo, request);
                }

                _logger.LogInfo("Certificate generation completed successfully for {0}", request.CertificateName);
                return certificateInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate for {0}", request.CertificateName);
                return new CertificateInfo { IsValid = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Generate standard certificate
        /// </summary>
        private CertificateInfo GenerateStandardCertificate(Models.CertificateRequest request, AppConfiguration config)
        {
            var filePaths = GetFilePaths(request, config);
            
            // Generate INF file
            var infContent = GenerateInfContent(request, config);
            File.WriteAllText(filePaths.InfPath, infContent, Encoding.ASCII);

            // Create CSR
            if (!CreateCSR(filePaths.InfPath, filePaths.CsrPath))
            {
                return new CertificateInfo { IsValid = false, ErrorMessage = "Failed to create CSR" };
            }

            // Submit to CA
            if (!SubmitToCA(filePaths.CsrPath, filePaths.CerPath, filePaths.PfxPath, config))
            {
                return new CertificateInfo { IsValid = false, ErrorMessage = "Failed to submit to CA" };
            }

            // Import and process certificate
            return ProcessCertificate(request, filePaths, config);
        }

        /// <summary>
        /// Generate wildcard certificate
        /// </summary>
        private CertificateInfo GenerateWildcardCertificate(Models.CertificateRequest request, AppConfiguration config)
        {
            var filePaths = GetFilePaths(request, config);
            
            // Generate INF file for wildcard
            var infContent = GenerateWildcardInfContent(request, config);
            File.WriteAllText(filePaths.InfPath, infContent, Encoding.ASCII);

            // Create CSR
            if (!CreateCSR(filePaths.InfPath, filePaths.CsrPath))
            {
                return new CertificateInfo { IsValid = false, ErrorMessage = "Failed to create CSR" };
            }

            // Submit to CA
            if (!SubmitToCA(filePaths.CsrPath, filePaths.CerPath, filePaths.PfxPath, config))
            {
                return new CertificateInfo { IsValid = false, ErrorMessage = "Failed to submit to CA" };
            }

            // Import and process certificate
            return ProcessCertificate(request, filePaths, config);
        }

        /// <summary>
        /// Generate certificate from existing CSR
        /// </summary>
        private CertificateInfo GenerateFromCSR(Models.CertificateRequest request, AppConfiguration config)
        {
            var filePaths = GetFilePaths(request, config);

            if (!File.Exists(request.CsrFilePath))
            {
                return new CertificateInfo { IsValid = false, ErrorMessage = "CSR file not found" };
            }

            // Submit existing CSR to CA
            if (!SubmitToCA(request.CsrFilePath, filePaths.CerPath, filePaths.PfxPath, config))
            {
                return new CertificateInfo { IsValid = false, ErrorMessage = "Failed to submit CSR to CA" };
            }

            // Import and process certificate
            return ProcessCertificate(request, filePaths, config);
        }

        /// <summary>
        /// Generate INF file content for standard certificate
        /// </summary>
        private string GenerateInfContent(Models.CertificateRequest request, AppConfiguration config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Version]");
            sb.AppendLine("Signature=$Windows NT$");
            sb.AppendLine();
            sb.AppendLine("[NewRequest]");
            sb.AppendLine($"Subject = \"{request.Subject}\"");
            sb.AppendLine("KeySpec = 1");
            sb.AppendLine($"KeyLength = {config.DefaultSettings.KeyLength}");
            sb.AppendLine($"Hashalgorithm = {config.DefaultSettings.HashAlgorithm}");
            sb.AppendLine("Exportable = TRUE");
            sb.AppendLine($"FriendlyName = {request.HostName}");
            sb.AppendLine("MachineKeySet = TRUE");
            sb.AppendLine("SMIME = False");
            sb.AppendLine("PrivateKeyArchive = FALSE");
            sb.AppendLine("UserProtected = FALSE");
            sb.AppendLine("UseExistingKeySet = FALSE");
            sb.AppendLine("ProviderName = Microsoft RSA SChannel Cryptographic Provider");
            sb.AppendLine("ProviderType = 12");
            sb.AppendLine("RequestType = PKCS10");
            sb.AppendLine("KeyUsage = 0xa0");
            sb.AppendLine();
            sb.AppendLine("[EnhancedKeyUsageExtension]");
            sb.AppendLine("OID=1.3.6.1.5.5.7.3.1");
            sb.AppendLine();
            sb.AppendLine("[Extensions]");
            sb.AppendLine("2.5.29.17 = \"{text}\"");

            // Add DNS SANs
            foreach (var dns in request.DnsSans)
            {
                sb.AppendLine($"_continue_ = \"dns={dns.Value}&\"");
            }

            // Add IP SANs
            foreach (var ip in request.IpSans)
            {
                sb.AppendLine($"_continue_ = \"ipaddress={ip.Value}&\"");
            }

            sb.AppendLine();
            sb.AppendLine("[RequestAttributes]");
            sb.AppendLine($"CertificateTemplate= {config.CertificateAuthority.Template}");

            return sb.ToString();
        }

        /// <summary>
        /// Generate INF file content for wildcard certificate
        /// </summary>
        private string GenerateWildcardInfContent(Models.CertificateRequest request, AppConfiguration config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Version]");
            sb.AppendLine("Signature=$Windows NT$");
            sb.AppendLine();
            sb.AppendLine("[NewRequest]");
            sb.AppendLine($"Subject = \"{request.Subject}\"");
            sb.AppendLine("KeySpec = 1");
            sb.AppendLine($"KeyLength = {config.DefaultSettings.KeyLength}");
            sb.AppendLine($"HashAlgorithm = {config.DefaultSettings.HashAlgorithm}");
            sb.AppendLine("Exportable = TRUE");
            sb.AppendLine($"FriendlyName = {request.HostName}");
            sb.AppendLine("MachineKeySet = TRUE");
            sb.AppendLine("SMIME = False");
            sb.AppendLine("PrivateKeyArchive = FALSE");
            sb.AppendLine("UserProtected = FALSE");
            sb.AppendLine("UseExistingKeySet = FALSE");
            sb.AppendLine("ProviderName = Microsoft RSA SChannel Cryptographic Provider");
            sb.AppendLine("ProviderType = 12");
            sb.AppendLine("RequestType = PKCS10");
            sb.AppendLine("KeyUsage = 0xa0");
            sb.AppendLine();
            sb.AppendLine("[EnhancedKeyUsageExtension]");
            sb.AppendLine("OID=1.3.6.1.5.5.7.3.1");
            sb.AppendLine();
            sb.AppendLine("[Extensions]");
            sb.AppendLine("2.5.29.17 = \"{text}\"");
            sb.AppendLine($"_continue_ = \"dns={request.FQDN}\"");
            sb.AppendLine();
            sb.AppendLine("[RequestAttributes]");
            sb.AppendLine($"CertificateTemplate={config.CertificateAuthority.Template}");

            return sb.ToString();
        }

        /// <summary>
        /// Create CSR using certreq.exe
        /// </summary>
        private bool CreateCSR(string infPath, string csrPath)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "certreq.exe",
                        Arguments = $"-new \"{infPath}\" \"{csrPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _logger.LogDebug("Creating CSR: certreq.exe -new \"{0}\" \"{1}\"", infPath, csrPath);
                process.Start();
                process.WaitForExit(30000); // 30 second timeout

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

        /// <summary>
        /// Submit CSR to CA
        /// </summary>
        private bool SubmitToCA(string csrPath, string cerPath, string pfxPath, AppConfiguration config)
        {
            try
            {
                var caConfig = $"{config.CertificateAuthority.Server}\\{config.CertificateAuthority.Server.Split('.')[0].ToUpper()}";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "certreq.exe",
                        Arguments = $"-config \"{caConfig}\" -submit \"{csrPath}\" \"{cerPath}\" \"{pfxPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _logger.LogDebug("Submitting to CA: certreq.exe -config \"{0}\" -submit \"{1}\" \"{2}\" \"{3}\"", 
                    caConfig, csrPath, cerPath, pfxPath);
                process.Start();
                process.WaitForExit(60000); // 60 second timeout

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    _logger.LogError("Failed to submit to CA: {0}", error);
                    return false;
                }

                _logger.LogInfo("Successfully submitted to CA and received certificate");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting to CA");
                return false;
            }
        }

        /// <summary>
        /// Process certificate after CA submission
        /// </summary>
        private CertificateInfo ProcessCertificate(Models.CertificateRequest request, FilePaths filePaths, AppConfiguration config)
        {
            try
            {
                // Import certificate to store
                ImportCertificate(filePaths.CerPath);

                // Find certificate in store
                var cert = FindCertificateInStore(request);
                if (cert == null)
                {
                    return new CertificateInfo { IsValid = false, ErrorMessage = "Certificate not found in store" };
                }

                // Repair certificate store
                RepairCertificate(cert.Thumbprint);

                // Export PFX with password
                var password = GetPasswordFromSecureString(request.PfxPassword);
                ExportPfxCertificate(cert, filePaths.PfxPath, password);

                // Create certificate info
                var certInfo = CertificateInfo.FromX509Certificate(cert);
                certInfo.PfxPath = filePaths.PfxPath;
                certInfo.CerPath = filePaths.CerPath;

                // Cleanup temporary files
                if (config.DefaultSettings.AutoCleanup)
                {
                    CleanupTemporaryFiles(filePaths);
                }

                return certInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing certificate");
                return new CertificateInfo { IsValid = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Import certificate to local machine store
        /// </summary>
        private void ImportCertificate(string cerPath)
        {
            try
            {
                var cert = new X509Certificate2(cerPath);
                var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
                store.Close();
                _logger.LogInfo("Certificate imported to LocalMachine\\My store");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing certificate");
                throw;
            }
        }

        /// <summary>
        /// Find certificate in store
        /// </summary>
        private X509Certificate2 FindCertificateInStore(Models.CertificateRequest request)
        {
            try
            {
                var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);

                var cert = store.Certificates.Cast<X509Certificate2>()
                    .FirstOrDefault(c => c.Subject.Contains(request.FQDN) || 
                                       (request.Type == CertificateType.Wildcard && c.Subject.Contains("*.example.com")));

                store.Close();
                return cert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding certificate in store");
                return null;
            }
        }

        /// <summary>
        /// Repair certificate store
        /// </summary>
        private void RepairCertificate(string thumbprint)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "certutil.exe",
                        Arguments = $"-repairstore my \"{thumbprint}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit(10000); // 10 second timeout

                _logger.LogInfo("Certificate store repair completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error repairing certificate store");
            }
        }

        /// <summary>
        /// Export PFX certificate
        /// </summary>
        private void ExportPfxCertificate(X509Certificate2 cert, string pfxPath, string password)
        {
            try
            {
                var securePassword = new SecureString();
                foreach (char c in password)
                {
                    securePassword.AppendChar(c);
                }

                var pfxBytes = cert.Export(X509ContentType.Pkcs12, securePassword);
                File.WriteAllBytes(pfxPath, pfxBytes);

                _logger.LogInfo("PFX certificate exported: {0}", pfxPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting PFX certificate");
                throw;
            }
        }

        /// <summary>
        /// Extract PEM and KEY files
        /// </summary>
        private void ExtractPemAndKeyFiles(CertificateInfo certInfo, Models.CertificateRequest request)
        {
            if (!_openSSLService.IsAvailable)
            {
                _logger.LogWarning("OpenSSL not available, skipping PEM/KEY extraction");
                return;
            }

            var config = _configService.GetConfiguration();
            var password = GetPasswordFromSecureString(request.PfxPassword);

            if (_openSSLService.ExtractPemAndKey(certInfo.PfxPath, password, config.FilePaths.CertificateFolder, request.CertificateName))
            {
                certInfo.PemPath = Path.Combine(config.FilePaths.CertificateFolder, $"{request.CertificateName}.pem");
                certInfo.KeyPath = Path.Combine(config.FilePaths.CertificateFolder, $"{request.CertificateName}.key");

                // Extract certificate chain
                _openSSLService.ExtractCertificateChain(certInfo.PfxPath, password, config.FilePaths.CertificateFolder, "certificate-chain");
            }
        }

        /// <summary>
        /// Get file paths for certificate generation
        /// </summary>
        private FilePaths GetFilePaths(Models.CertificateRequest request, AppConfiguration config)
        {
            var basePath = config.FilePaths.CertificateFolder;
            var certName = request.CertificateName;

            return new FilePaths
            {
                InfPath = Path.Combine(basePath, $"{certName}.inf"),
                CsrPath = Path.Combine(basePath, $"{certName}.csr"),
                CerPath = Path.Combine(basePath, $"{certName}.cer"),
                PfxPath = Path.Combine(basePath, $"{certName}.pfx"),
                RspPath = Path.Combine(basePath, $"{certName}.rsp")
            };
        }

        /// <summary>
        /// Get password from SecureString
        /// </summary>
        private string GetPasswordFromSecureString(SecureString securePassword)
        {
            if (securePassword == null)
                return _configService.GetConfiguration().DefaultSettings.DefaultPassword;

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

        /// <summary>
        /// Cleanup temporary files
        /// </summary>
        private void CleanupTemporaryFiles(FilePaths filePaths)
        {
            try
            {
                var filesToDelete = new[] { filePaths.InfPath, filePaths.CsrPath, filePaths.RspPath };
                foreach (var file in filesToDelete)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        _logger.LogDebug("Deleted temporary file: {0}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error cleaning up temporary files: {0}", ex.Message);
            }
        }

        /// <summary>
        /// File paths for certificate generation
        /// </summary>
        private class FilePaths
        {
            public string InfPath { get; set; }
            public string CsrPath { get; set; }
            public string CerPath { get; set; }
            public string PfxPath { get; set; }
            public string RspPath { get; set; }
        }
    }
}

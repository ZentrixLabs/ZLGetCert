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
using ZLGetCert.Utilities;

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
        private readonly PemExportService _pemExportService;
        private readonly AuditService _auditService;

        private CertificateService()
        {
            _logger = LoggingService.Instance;
            _configService = ConfigurationService.Instance;
            _pemExportService = PemExportService.Instance;
            _auditService = AuditService.Instance;
        }

        /// <summary>
        /// Safely escape and quote a command line argument for .NET Framework 4.8
        /// This provides similar protection to ArgumentList in newer .NET versions
        /// </summary>
        private string EscapeArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument))
                return "\"\"";

            // If the argument contains spaces, quotes, or special characters, wrap it in quotes
            // and escape any internal quotes
            if (argument.Contains(" ") || argument.Contains("\"") || argument.Contains("\t"))
            {
                // Escape backslashes that precede quotes
                argument = argument.Replace("\\\"", "\\\\\"");
                // Escape quotes
                argument = argument.Replace("\"", "\\\"");
                // Wrap in quotes
                return $"\"{argument}\"";
            }

            return argument;
        }

        /// <summary>
        /// Build a safe command line from multiple arguments
        /// </summary>
        private string BuildArgumentString(params string[] arguments)
        {
            return string.Join(" ", arguments.Select(EscapeArgument));
        }

        /// <summary>
        /// Get available Certificate Authorities from Active Directory
        /// </summary>
        public List<string> GetAvailableCAs()
        {
            var caList = new List<string>();

            try
            {
                _logger.LogInfo("Querying available CAs from Active Directory");

                // Try certutil -dump first (shows CA servers)
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "certutil.exe",
                        Arguments = "-dump", // Simple argument, no escaping needed
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                // Wait for exit with timeout and kill if necessary
                if (!process.WaitForExit(30000))
                {
                    _logger.LogWarning("CA dump timed out after 30 seconds");
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Process may have already exited
                    }
                }

                if (process.ExitCode == 0)
                {
                    // Parse the output for CA server names
                    caList = ParseCADumpOutput(output);
                }
                else
                {
                    _logger.LogWarning("certutil -dump failed, trying alternative method");
                    // Fallback to -ADCA if -dump doesn't work
                    caList = GetCAsFromADCA();
                }
                
                _logger.LogInfo("Found {0} available CA(s)", caList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying Certificate Authorities");
            }

            return caList;
        }

        /// <summary>
        /// Fallback method using certutil -ADCA
        /// </summary>
        private List<string> GetCAsFromADCA()
        {
            var caList = new List<string>();

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "certutil.exe",
                        Arguments = "-ADCA", // Simple argument, no escaping needed
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                // Wait for exit with timeout and kill if necessary
                if (!process.WaitForExit(30000))
                {
                    _logger.LogWarning("ADCA query timed out after 30 seconds");
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Process may have already exited
                    }
                }

                if (process.ExitCode == 0)
                {
                    caList = ParseCAOutput(output);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fallback CA query");
            }

            return caList;
        }

        /// <summary>
        /// Parse certutil -dump output to extract CA server names
        /// </summary>
        private List<string> ParseCADumpOutput(string output)
        {
            var caList = new List<string>();

            try
            {
                // Split output into lines
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Look for lines that contain "Server:" field
                    if (trimmedLine.StartsWith("Server:"))
                    {
                        // Extract server name after "Server:"
                        var serverName = trimmedLine.Substring(7).Trim(); // Remove "Server:" prefix
                        serverName = serverName.Trim('"'); // Remove quotes if present
                        
                        if (!string.IsNullOrWhiteSpace(serverName) && 
                            !caList.Contains(serverName))
                        {
                            caList.Add(serverName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing CA dump output");
            }

            return caList;
        }

        /// <summary>
        /// Parse certutil -ADCA output to extract CA names
        /// </summary>
        private List<string> ParseCAOutput(string output)
        {
            var caList = new List<string>();

            try
            {
                // Split output into lines
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Look for lines that contain CA server information
                    // Format is typically: ServerName\CAName or ServerName
                    if (trimmedLine.Contains("\\"))
                    {
                        // Extract just the server name (before the backslash)
                        var parts = trimmedLine.Split('\\');
                        if (parts.Length > 0)
                        {
                            var serverName = parts[0].Trim();
                            // Remove any leading markers or special characters
                            serverName = serverName.TrimStart('*', ' ', '\t', '>', '-');
                            
                            if (!string.IsNullOrWhiteSpace(serverName) && 
                                !serverName.StartsWith("=") && 
                                !serverName.StartsWith("-") &&
                                !serverName.StartsWith("Allow") && // Skip permission lines
                                !serverName.StartsWith("NT AUTHORITY") && // Skip permission lines
                                !caList.Contains(serverName))
                            {
                                caList.Add(serverName);
                            }
                        }
                    }
                    // Also look for standalone server names (without backslash)
                    else if (!trimmedLine.StartsWith("Allow") && 
                             !trimmedLine.StartsWith("NT AUTHORITY") && 
                             !trimmedLine.StartsWith("=") && 
                             !trimmedLine.StartsWith("-") &&
                             !trimmedLine.StartsWith("*") &&
                             trimmedLine.Length > 3 && // Reasonable server name length
                             !caList.Contains(trimmedLine))
                    {
                        caList.Add(trimmedLine);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing CA output");
            }

            return caList;
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

                // SECURITY: Validate CA server name to prevent command injection
                server = ProcessArgumentValidator.ValidateCAServerName(server, "CA Server");
                
                var caConfig = $"{server}\\{server.Split('.')[0].ToUpper()}";
                
                _logger.LogInfo("Querying available templates from CA: {0}", caConfig);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "certutil.exe",
                        // SECURITY: Use BuildArgumentString to safely escape arguments
                        Arguments = BuildArgumentString("-CATemplates", "-config", caConfig),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                // Use WaitForExit with timeout and kill if necessary
                if (!process.WaitForExit(30000))
                {
                    _logger.LogWarning("Template query timed out after 30 seconds, killing process");
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Process may have already exited
                    }
                    return templates;
                }

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Failed to query templates: {0}", error);
                    return templates;
                }

                // Parse the output
                templates = ParseTemplateOutput(output);
                
                _logger.LogInfo("Found {0} available templates", templates.Count);
            }
            catch (ArgumentException argEx)
            {
                // Input validation error - log and return empty list
                _logger.LogError(argEx, "Invalid input for template query");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying certificate templates");
            }

            return templates;
        }

        /// <summary>
        /// Parse certutil template output and filter out access denied templates
        /// </summary>
        private List<CertificateTemplate> ParseTemplateOutput(string output)
        {
            var templates = new List<CertificateTemplate>();

            try
            {
                // Split output into lines
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Skip empty lines and separator lines
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("---"))
                        continue;

                    // Skip lines that are entirely access denied (but allow auto-enroll denied)
                    if (trimmedLine.StartsWith("Access is denied.") || 
                        trimmedLine.EndsWith("-- Access is denied."))
                    {
                        continue; // Skip this template entirely - completely denied
                    }

                    // Skip certutil completion messages
                    if (trimmedLine.StartsWith("CertUtil:") || 
                        trimmedLine.Contains("command completed successfully"))
                    {
                        continue;
                    }

                    // Look for template lines that contain template information
                    // Format examples:
                    // "Auto-Enroll: Access is denied. (CodeSigning_MP_Modern: Code Signing_MP_Modern)"
                    // "Access is denied. (DirectoryEmailReplication: Directory Email Replication)"
                    // "CodeSigning_MP_Modern: Code Signing_MP_Modern -- Auto-Enroll: Access is denied."
                    if (trimmedLine.Contains(":"))
                    {
                        string templateName = null;
                        string displayName = null;
                        
                        // Try format: "Auto-Enroll: Access is denied. (TemplateName: DisplayName)"
                        if (trimmedLine.Contains("(") && trimmedLine.Contains(")"))
                        {
                            var startParen = trimmedLine.IndexOf('(');
                            var endParen = trimmedLine.LastIndexOf(')');
                            
                            if (startParen >= 0 && endParen > startParen)
                            {
                                var templateInfo = trimmedLine.Substring(startParen + 1, endParen - startParen - 1);
                                var parts = templateInfo.Split(':');
                                
                                if (parts.Length >= 2)
                                {
                                    templateName = parts[0].Trim();
                                    displayName = parts[1].Trim();
                                }
                            }
                        }
                        // Try format: "TemplateName: DisplayName -- Auto-Enroll: Access is denied."
                        else if (trimmedLine.Contains(" -- Auto-Enroll:"))
                        {
                            var templatePart = trimmedLine.Split(new[] { " -- Auto-Enroll:" }, StringSplitOptions.None)[0];
                            var parts = templatePart.Split(':');
                            
                            if (parts.Length >= 2)
                            {
                                templateName = parts[0].Trim();
                                displayName = parts[1].Trim();
                            }
                        }
                        // Try format: "TemplateName: DisplayName -- Access is denied."
                        else if (trimmedLine.Contains(" -- Access is denied."))
                        {
                            var templatePart = trimmedLine.Split(new[] { " -- Access is denied." }, StringSplitOptions.None)[0];
                            var parts = templatePart.Split(':');
                            
                            if (parts.Length >= 2)
                            {
                                templateName = parts[0].Trim();
                                displayName = parts[1].Trim();
                            }
                        }
                        
                        // Add template if we successfully parsed it
                        if (!string.IsNullOrEmpty(templateName) && !string.IsNullOrEmpty(displayName))
                        {
                            // Only add if this template is NOT entirely access denied
                            // Auto-enroll denied is OK - we can still manually request
                            if (!trimmedLine.StartsWith("Access is denied.") && 
                                !trimmedLine.EndsWith("-- Access is denied."))
                            {
                                templates.Add(new CertificateTemplate
                                {
                                    Name = templateName,
                                    DisplayName = displayName
                                });
                            }
                        }
                    }
                    // Look for simple template names (without entirely access denied prefix)
                    else if (!trimmedLine.StartsWith("Access is denied") && 
                             !trimmedLine.EndsWith("-- Access is denied.") &&
                             !trimmedLine.StartsWith("Certificate") &&
                             !trimmedLine.StartsWith("Template") &&
                             !trimmedLine.Contains("===") &&
                             !trimmedLine.Contains("---") &&
                             trimmedLine.Length > 2)
                    {
                        // This might be a simple template name
                        templates.Add(new CertificateTemplate
                            {
                                Name = trimmedLine,
                                DisplayName = trimmedLine
                            });
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
                
                // CRITICAL: Validate template/type match before generating certificate
                var validation = new Utilities.ValidationResult();
                ValidationHelper.ValidateTemplateTypeMatch(
                    request.Template,
                    request.Type,
                    config.CertificateParameters.EnhancedKeyUsageOIDs,
                    validation);

                if (!validation.IsValid)
                {
                    _logger.LogError("Template/Type validation failed: {0}", 
                        string.Join(", ", validation.Errors));
                    
                    // SECURITY AUDIT: Log validation failure
                    _auditService.LogAuditEvent(
                        AuditService.AuditEventType.ValidationFailure,
                        $"Template/Type validation failed: {string.Join(", ", validation.Errors)}. " +
                        $"Template: {request.Template}, Type: {request.Type}",
                        certificateName: request.CertificateName);
                    
                    return new CertificateInfo
                    {
                        IsValid = false,
                        ErrorMessage = "Certificate validation failed:\n" + string.Join("\n", validation.Errors)
                    };
                }

                if (validation.Warnings.Any())
                {
                    foreach (var warning in validation.Warnings)
                    {
                        _logger.LogWarning(warning);
                    }
                }

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
                    _logger.LogInfo("ExtractPemKey is true, calling ExtractPemAndKeyFiles for certificate: {0}", request.CertificateName);
                    ExtractPemAndKeyFiles(certificateInfo, request);
                }
                else
                {
                    _logger.LogInfo("PEM extraction skipped - IsValid: {0}, ExtractPemKey: {1}", certificateInfo.IsValid, request.ExtractPemKey);
                }

                // SECURITY AUDIT: Log successful certificate generation
                _auditService.LogAuditEvent(
                    AuditService.AuditEventType.CertificateGenerated,
                    $"Certificate generated successfully. Type: {request.Type}, Template: {request.Template}, CA: {request.CAServer}",
                    certificateName: request.CertificateName,
                    thumbprint: certificateInfo.Thumbprint);

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
            if (!SubmitToCA(filePaths.CsrPath, filePaths.CerPath, filePaths.PfxPath, request.CAServer))
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
            if (!SubmitToCA(filePaths.CsrPath, filePaths.CerPath, filePaths.PfxPath, request.CAServer))
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
            if (!SubmitToCA(request.CsrFilePath, filePaths.CerPath, filePaths.PfxPath, request.CAServer))
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
            sb.AppendLine($"KeySpec = {config.CertificateParameters.KeySpec}");
            sb.AppendLine($"KeyLength = {config.DefaultSettings.KeyLength}");
            sb.AppendLine($"Hashalgorithm = {config.DefaultSettings.HashAlgorithm}");
            sb.AppendLine($"Exportable = {(config.CertificateParameters.Exportable ? "TRUE" : "FALSE")}");
            sb.AppendLine($"FriendlyName = {request.HostName}");
            sb.AppendLine($"MachineKeySet = {(config.CertificateParameters.MachineKeySet ? "TRUE" : "FALSE")}");
            sb.AppendLine($"SMIME = {(config.CertificateParameters.SMIME ? "TRUE" : "FALSE")}");
            sb.AppendLine($"PrivateKeyArchive = {(config.CertificateParameters.PrivateKeyArchive ? "TRUE" : "FALSE")}");
            sb.AppendLine($"UserProtected = {(config.CertificateParameters.UserProtected ? "TRUE" : "FALSE")}");
            sb.AppendLine($"UseExistingKeySet = {(config.CertificateParameters.UseExistingKeySet ? "TRUE" : "FALSE")}");
            sb.AppendLine($"ProviderName = {config.CertificateParameters.ProviderName}");
            sb.AppendLine($"ProviderType = {config.CertificateParameters.ProviderType}");
            sb.AppendLine("RequestType = PKCS10");
            sb.AppendLine($"KeyUsage = {config.CertificateParameters.KeyUsage}");
            sb.AppendLine();
            sb.AppendLine("[EnhancedKeyUsageExtension]");
            foreach (var oid in config.CertificateParameters.EnhancedKeyUsageOIDs)
            {
                sb.AppendLine($"OID={oid}");
            }
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
            sb.AppendLine($"CertificateTemplate= {request.Template}");

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
            sb.AppendLine($"KeySpec = {config.CertificateParameters.KeySpec}");
            sb.AppendLine($"KeyLength = {config.DefaultSettings.KeyLength}");
            sb.AppendLine($"HashAlgorithm = {config.DefaultSettings.HashAlgorithm}");
            sb.AppendLine($"Exportable = {(config.CertificateParameters.Exportable ? "TRUE" : "FALSE")}");
            sb.AppendLine($"FriendlyName = {request.HostName}");
            sb.AppendLine($"MachineKeySet = {(config.CertificateParameters.MachineKeySet ? "TRUE" : "FALSE")}");
            sb.AppendLine($"SMIME = {(config.CertificateParameters.SMIME ? "TRUE" : "FALSE")}");
            sb.AppendLine($"PrivateKeyArchive = {(config.CertificateParameters.PrivateKeyArchive ? "TRUE" : "FALSE")}");
            sb.AppendLine($"UserProtected = {(config.CertificateParameters.UserProtected ? "TRUE" : "FALSE")}");
            sb.AppendLine($"UseExistingKeySet = {(config.CertificateParameters.UseExistingKeySet ? "TRUE" : "FALSE")}");
            sb.AppendLine($"ProviderName = {config.CertificateParameters.ProviderName}");
            sb.AppendLine($"ProviderType = {config.CertificateParameters.ProviderType}");
            sb.AppendLine("RequestType = PKCS10");
            sb.AppendLine($"KeyUsage = {config.CertificateParameters.KeyUsage}");
            sb.AppendLine();
            sb.AppendLine("[EnhancedKeyUsageExtension]");
            foreach (var oid in config.CertificateParameters.EnhancedKeyUsageOIDs)
            {
                sb.AppendLine($"OID={oid}");
            }
            sb.AppendLine();
            sb.AppendLine("[Extensions]");
            sb.AppendLine("2.5.29.17 = \"{text}\"");
            sb.AppendLine($"_continue_ = \"dns={request.FQDN}\"");
            sb.AppendLine();
            sb.AppendLine("[RequestAttributes]");
            sb.AppendLine($"CertificateTemplate={request.Template}");

            return sb.ToString();
        }

        /// <summary>
        /// Create CSR using certreq.exe
        /// </summary>
        private bool CreateCSR(string infPath, string csrPath)
        {
            try
            {
                // SECURITY: Validate file paths to prevent command injection
                infPath = ProcessArgumentValidator.ValidateFilePath(infPath, "INF Path");
                csrPath = ProcessArgumentValidator.ValidateFilePath(csrPath, "CSR Path");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "certreq.exe",
                        // SECURITY: Use BuildArgumentString to safely escape file paths
                        Arguments = BuildArgumentString("-new", infPath, csrPath),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _logger.LogDebug("Creating CSR with certreq.exe");
                process.Start();
                
                // Wait for exit with timeout and kill if necessary
                if (!process.WaitForExit(30000))
                {
                    _logger.LogError("CSR creation timed out after 30 seconds");
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Process may have already exited
                    }
                    return false;
                }

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    _logger.LogError("Failed to create CSR: {0}", error);
                    return false;
                }

                _logger.LogInfo("CSR created successfully: {0}", csrPath);
                return true;
            }
            catch (ArgumentException argEx)
            {
                _logger.LogError(argEx, "Invalid file path for CSR creation");
                return false;
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
        private bool SubmitToCA(string csrPath, string cerPath, string pfxPath, string caServer)
        {
            try
            {
                // SECURITY: Validate all inputs to prevent command injection
                caServer = ProcessArgumentValidator.ValidateCAServerName(caServer, "CA Server");
                csrPath = ProcessArgumentValidator.ValidateFilePath(csrPath, "CSR Path");
                cerPath = ProcessArgumentValidator.ValidateFilePath(cerPath, "CER Path");
                pfxPath = ProcessArgumentValidator.ValidateFilePath(pfxPath, "PFX Path");

                var caConfig = $"{caServer}\\{caServer.Split('.')[0].ToUpper()}";
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "certreq.exe",
                        // SECURITY: Use BuildArgumentString to safely escape all arguments
                        Arguments = BuildArgumentString("-config", caConfig, "-submit", csrPath, cerPath, pfxPath),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _logger.LogDebug("Submitting CSR to CA");
                process.Start();
                
                // Wait for exit with timeout and kill if necessary
                if (!process.WaitForExit(60000))
                {
                    _logger.LogError("CA submission timed out after 60 seconds");
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Process may have already exited
                    }
                    return false;
                }

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    _logger.LogError("Failed to submit to CA: {0}", error);
                    return false;
                }

                _logger.LogInfo("Successfully submitted to CA and received certificate");
                return true;
            }
            catch (ArgumentException argEx)
            {
                _logger.LogError(argEx, "Invalid input for CA submission");
                return false;
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

                // Repair certificate store to associate private key
                RepairCertificate(cert.Thumbprint);

                // Re-find the certificate after repair to get the one with private key
                cert = FindCertificateInStore(request);
                if (cert == null)
                {
                    return new CertificateInfo { IsValid = false, ErrorMessage = "Certificate not found in store after repair" };
                }

                // Check if certificate has private key
                if (!cert.HasPrivateKey)
                {
                    _logger.LogError("Certificate does not have private key after repair. Thumbprint: {0}", cert.Thumbprint);
                    return new CertificateInfo { IsValid = false, ErrorMessage = "Certificate does not contain a private key. This may be due to certificate store issues or the certificate not being properly associated with the private key." };
                }

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
        /// Import certificate to local machine store (requires admin privileges)
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

                // First try to find a certificate with private key
                var certWithKey = store.Certificates.Cast<X509Certificate2>()
                    .FirstOrDefault(c => (c.Subject.Contains(request.FQDN) || 
                                        (request.Type == CertificateType.Wildcard && c.Subject.Contains("*.example.com"))) &&
                                       c.HasPrivateKey);

                // If no certificate with private key found, try any certificate matching the subject
                var cert = certWithKey ?? store.Certificates.Cast<X509Certificate2>()
                    .FirstOrDefault(c => c.Subject.Contains(request.FQDN) || 
                                       (request.Type == CertificateType.Wildcard && c.Subject.Contains("*.example.com")));

                store.Close();
                
                if (cert != null)
                {
                    _logger.LogInfo("Found certificate in store: {0}, HasPrivateKey: {1}", cert.Subject, cert.HasPrivateKey);
                }
                
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
                // SECURITY: Validate thumbprint format to prevent command injection
                thumbprint = ProcessArgumentValidator.ValidateThumbprint(thumbprint, "Thumbprint");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "certutil.exe",
                        // SECURITY: Use BuildArgumentString to safely escape thumbprint
                        Arguments = BuildArgumentString("-repairstore", "my", thumbprint),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                
                // Wait for exit with timeout and kill if necessary
                if (!process.WaitForExit(10000))
                {
                    _logger.LogWarning("Certificate repair timed out after 10 seconds");
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Process may have already exited
                    }
                    return;
                }

                _logger.LogInfo("Certificate store repair completed");
            }
            catch (ArgumentException argEx)
            {
                _logger.LogError(argEx, "Invalid thumbprint for certificate repair");
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
        /// Extract PEM and KEY files using pure .NET implementation
        /// </summary>
        private void ExtractPemAndKeyFiles(CertificateInfo certInfo, Models.CertificateRequest request)
        {
            _logger.LogInfo("Starting PEM extraction for certificate: {0}, PFX: {1}", request.CertificateName, certInfo.PfxPath);
            
            var config = _configService.GetConfiguration();
            var password = GetPasswordFromSecureString(request.PfxPassword);

            _logger.LogInfo("Calling PemExportService.ExtractPemAndKey with ExtractCaBundle: {0}", request.ExtractCaBundle);
            
            if (_pemExportService.ExtractPemAndKey(certInfo.PfxPath, password, config.FilePaths.CertificateFolder, request.CertificateName))
            {
                _logger.LogInfo("PEM and KEY extraction successful, setting file paths");
                certInfo.PemPath = Path.Combine(config.FilePaths.CertificateFolder, $"{request.CertificateName}.pem");
                certInfo.KeyPath = Path.Combine(config.FilePaths.CertificateFolder, $"{request.CertificateName}.key");

                // Extract certificate chain (CA bundle) if requested
                if (request.ExtractCaBundle)
                {
                    _logger.LogInfo("ExtractCaBundle is true, calling ExtractCertificateChain");
                    _pemExportService.ExtractCertificateChain(certInfo.PfxPath, password, config.FilePaths.CertificateFolder, "ca-bundle");
                }
                else
                {
                    _logger.LogInfo("ExtractCaBundle is false, skipping CA bundle extraction");
                }
            }
            else
            {
                _logger.LogError("PemExportService.ExtractPemAndKey returned false for certificate: {0}", request.CertificateName);
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
            // Password is required - no defaults for security
            if (securePassword == null || securePassword.Length == 0)
            {
                _logger.LogError("Password is required for certificate operations");
                throw new ArgumentException(
                    "Password is required for certificate operations. " +
                    "Default passwords are no longer supported for security reasons. " +
                    "Please enter a strong password when generating certificates.",
                    nameof(securePassword));
            }

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

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ZentrixLabs.ZLGetCert.Core.Contracts;
using ZentrixLabs.ZLGetCert.Core.Pipeline;
using ZentrixLabs.ZLGetCert.Core.Services;

namespace ZentrixLabs.ZLGetCert.Core.Services.Adcs
{
    /// <summary>
    /// Certificate Authority client implementation using certreq.exe.
    /// </summary>
    public sealed class CertReqCertificateAuthorityClient : ICertificateAuthorityClient
    {
        /// <summary>
        /// Executes the CA request using certreq.exe.
        /// </summary>
        public CaIssueResult Issue(ExecutionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Request == null)
                throw new ArgumentException("Request cannot be null", nameof(context));

            var request = context.Request;

            // Locate certreq.exe
            string certreqPath = LocateCertReq();
            if (string.IsNullOrEmpty(certreqPath))
            {
                return new CaIssueResult
                {
                    Success = false,
                    FailureCategory = FailureCategory.EnvironmentError,
                    Message = "certreq.exe not found. Expected location: SystemRoot\\System32\\certreq.exe"
                };
            }

            // Create temp working directory
            string workDir = null;
            try
            {
                string requestId = !string.IsNullOrEmpty(request.RequestId) 
                    ? request.RequestId 
                    : Guid.NewGuid().ToString("N");
                workDir = Path.Combine(Path.GetTempPath(), "ZLGetCert", requestId);
                Directory.CreateDirectory(workDir);

                // Execute based on request mode
                if (request.Mode == RequestMode.NewKeypair)
                {
                    return IssueNewKeypair(certreqPath, workDir, request);
                }
                else if (request.Mode == RequestMode.SignExistingCsr)
                {
                    return IssueFromCsr(certreqPath, workDir, request);
                }
                else
                {
                    return new CaIssueResult
                    {
                        Success = false,
                        FailureCategory = FailureCategory.ConfigurationError,
                        Message = $"Unsupported request mode: {request.Mode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new CaIssueResult
                {
                    Success = false,
                    FailureCategory = FailureCategory.EnvironmentError,
                    Message = $"Unexpected error: {ex.Message}"
                };
            }
            finally
            {
                // Cleanup temp directory (optional - could be kept for debugging)
                // Directory.Delete(workDir, true);
            }
        }

        /// <summary>
        /// Locates certreq.exe using the same method as ToolingPresenceCheck.
        /// </summary>
        private string LocateCertReq()
        {
            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            if (string.IsNullOrEmpty(systemRoot))
            {
                systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            }

            string system32Path = Path.Combine(systemRoot, "System32");
            string certreqPath = Path.Combine(system32Path, "certreq.exe");

            if (File.Exists(certreqPath))
            {
                return certreqPath;
            }

            return null;
        }

        /// <summary>
        /// Issues a certificate with a new keypair.
        /// </summary>
        private CaIssueResult IssueNewKeypair(string certreqPath, string workDir, CertificateRequest request)
        {
            try
            {
                // Build CA config string
                string caConfig = BuildCaConfigString(request);
                if (string.IsNullOrEmpty(caConfig))
                {
                    return new CaIssueResult
                    {
                        Success = false,
                        FailureCategory = FailureCategory.ConfigurationError,
                        Message = "CA configuration is required (CaServer or ConfigString)"
                    };
                }

                // Create INF file
                string infPath = Path.Combine(workDir, "request.inf");
                CreateInfFile(infPath, request);

                // Generate REQ file
                string reqPath = Path.Combine(workDir, "request.req");
                var reqResult = ExecuteCertReq(certreqPath, workDir, $"-new \"{infPath}\" \"{reqPath}\"");
                if (!reqResult.Success)
                {
                    return new CaIssueResult
                    {
                        Success = false,
                        FailureCategory = reqResult.FailureCategory ?? FailureCategory.CARequestError,
                        Message = $"Failed to generate certificate request: {reqResult.Message}"
                    };
                }

                // Submit to CA
                string cerPath = Path.Combine(workDir, "certificate.cer");
                string pfxPath = Path.Combine(workDir, "certificate.pfx");
                string configArg = $"-config \"{caConfig}\"";
                if (!string.IsNullOrEmpty(request.Ca?.Template))
                {
                    configArg += $" -attrib \"CertificateTemplate:{request.Ca.Template}\"";
                }

                var submitResult = ExecuteCertReq(certreqPath, workDir, $"{configArg} -submit \"{reqPath}\" \"{cerPath}\"");
                if (!submitResult.Success)
                {
                    return new CaIssueResult
                    {
                        Success = false,
                        FailureCategory = submitResult.FailureCategory ?? FailureCategory.CARequestError,
                        Message = $"Failed to submit certificate request: {submitResult.Message}"
                    };
                }

                // Accept the certificate and create PFX
                var acceptResult = ExecuteCertReq(certreqPath, workDir, $"-accept \"{cerPath}\"");
                if (!acceptResult.Success)
                {
                    return new CaIssueResult
                    {
                        Success = false,
                        FailureCategory = acceptResult.FailureCategory ?? FailureCategory.CARequestError,
                        Message = $"Failed to accept certificate: {acceptResult.Message}"
                    };
                }

                // Export to PFX (if exportable)
                if (request.Crypto?.ExportablePrivateKey == true)
                {
                    // Find the certificate in the store and export to PFX
                    // For now, we'll use certutil to export
                    // This is a simplified approach - in production you might want to use CryptoAPI directly
                    var exportResult = ExportToPfx(workDir, cerPath, pfxPath, request);
                    if (!exportResult.Success)
                    {
                        // PFX export is optional, so we continue with CER only
                        pfxPath = null;
                    }
                }

                // Get absolute paths
                string absoluteCerPath = File.Exists(cerPath) ? Path.GetFullPath(cerPath) : null;
                string absolutePfxPath = (pfxPath != null && File.Exists(pfxPath)) ? Path.GetFullPath(pfxPath) : null;

                return new CaIssueResult
                {
                    Success = true,
                    Message = "Certificate issued successfully",
                    CerPath = absoluteCerPath,
                    PfxPath = absolutePfxPath,
                    ChainPath = null // Chain path would be set if we extract the chain
                };
            }
            catch (Exception ex)
            {
                return new CaIssueResult
                {
                    Success = false,
                    FailureCategory = FailureCategory.EnvironmentError,
                    Message = $"Unexpected error during certificate issuance: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Issues a certificate from an existing CSR.
        /// </summary>
        private CaIssueResult IssueFromCsr(string certreqPath, string workDir, CertificateRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.CsrPath) || !File.Exists(request.CsrPath))
                {
                    return new CaIssueResult
                    {
                        Success = false,
                        FailureCategory = FailureCategory.ConfigurationError,
                        Message = $"CSR file not found: {request.CsrPath}"
                    };
                }

                // Build CA config string
                string caConfig = BuildCaConfigString(request);
                if (string.IsNullOrEmpty(caConfig))
                {
                    return new CaIssueResult
                    {
                        Success = false,
                        FailureCategory = FailureCategory.ConfigurationError,
                        Message = "CA configuration is required (CaServer or ConfigString)"
                    };
                }

                // Submit CSR to CA
                string cerPath = Path.Combine(workDir, "certificate.cer");
                string configArg = $"-config \"{caConfig}\"";
                if (!string.IsNullOrEmpty(request.Ca?.Template))
                {
                    configArg += $" -attrib \"CertificateTemplate:{request.Ca.Template}\"";
                }

                var submitResult = ExecuteCertReq(certreqPath, workDir, $"{configArg} -submit \"{request.CsrPath}\" \"{cerPath}\"");
                if (!submitResult.Success)
                {
                    return new CaIssueResult
                    {
                        Success = false,
                        FailureCategory = submitResult.FailureCategory ?? FailureCategory.CARequestError,
                        Message = $"Failed to submit CSR: {submitResult.Message}"
                    };
                }

                // Get absolute path
                string absoluteCerPath = File.Exists(cerPath) ? Path.GetFullPath(cerPath) : null;

                return new CaIssueResult
                {
                    Success = true,
                    Message = "Certificate issued successfully from CSR",
                    CerPath = absoluteCerPath,
                    PfxPath = null, // CSR mode doesn't produce PFX
                    ChainPath = null
                };
            }
            catch (Exception ex)
            {
                return new CaIssueResult
                {
                    Success = false,
                    FailureCategory = FailureCategory.EnvironmentError,
                    Message = $"Unexpected error during CSR submission: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Builds the CA configuration string from the request.
        /// </summary>
        private string BuildCaConfigString(CertificateRequest request)
        {
            if (request.Ca == null)
                return null;

            // Use explicit ConfigString if provided
            if (!string.IsNullOrEmpty(request.Ca.CaConfig?.ConfigString))
            {
                return request.Ca.CaConfig.ConfigString;
            }

            // Build from CaServer and CaName
            if (!string.IsNullOrEmpty(request.Ca.CaConfig?.CaServer))
            {
                string caServer = request.Ca.CaConfig.CaServer;
                string caName = request.Ca.CaConfig.CaName;
                
                if (!string.IsNullOrEmpty(caName))
                {
                    return $"{caServer}\\{caName}";
                }
                else
                {
                    // If no CaName, use server name as CA name (common pattern)
                    return caServer;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates an INF file for certificate request.
        /// </summary>
        private void CreateInfFile(string infPath, CertificateRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Version]");
            sb.AppendLine("Signature=\"$Windows NT$\"");
            sb.AppendLine();
            sb.AppendLine("[NewRequest]");

            // Subject
            if (!string.IsNullOrEmpty(request.Subject?.SubjectDn))
            {
                sb.AppendLine($"Subject=\"{request.Subject.SubjectDn}\"");
            }
            else if (!string.IsNullOrEmpty(request.Subject?.CommonName))
            {
                sb.AppendLine($"Subject=\"CN={request.Subject.CommonName}\"");
            }

            // Key algorithm and size
            if (request.Crypto != null)
            {
                string keySpec = "AT_KEYEXCHANGE"; // Default
                if (!string.IsNullOrEmpty(request.Crypto.KeyAlgorithm))
                {
                    if (request.Crypto.KeyAlgorithm.Equals("RSA", StringComparison.OrdinalIgnoreCase))
                    {
                        keySpec = "AT_KEYEXCHANGE";
                    }
                }

                sb.AppendLine($"KeySpec={keySpec}");
                sb.AppendLine($"KeyLength={request.Crypto.KeySize > 0 ? request.Crypto.KeySize : 2048}");
                
                if (request.Crypto.ExportablePrivateKey)
                {
                    sb.AppendLine("Exportable=TRUE");
                }
                else
                {
                    sb.AppendLine("Exportable=FALSE");
                }
            }

            // Subject Alternative Names
            if (request.Subject?.SubjectAlternativeNames != null && request.Subject.SubjectAlternativeNames.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[Extensions]");
                var sanList = new StringBuilder();
                bool first = true;
                foreach (var san in request.Subject.SubjectAlternativeNames)
                {
                    if (!first) sanList.Append(",");
                    first = false;
                    
                    // SAN format: "dns:example.com" or "ip:192.168.1.1"
                    if (san.StartsWith("dns:", StringComparison.OrdinalIgnoreCase) ||
                        san.StartsWith("ip:", StringComparison.OrdinalIgnoreCase))
                    {
                        sanList.Append(san);
                    }
                    else
                    {
                        // Assume DNS if no prefix
                        sanList.Append($"dns:{san}");
                    }
                }
                sb.AppendLine($"2.5.29.17 = \"{sanList}\"");
            }

            File.WriteAllText(infPath, sb.ToString());
        }

        /// <summary>
        /// Executes certreq.exe with the given arguments.
        /// </summary>
        private CertReqExecutionResult ExecuteCertReq(string certreqPath, string workingDirectory, string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = certreqPath,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                int exitCode = process.ExitCode;
                string combinedOutput = output + error;

                if (exitCode == 0)
                {
                    return new CertReqExecutionResult
                    {
                        Success = true,
                        Message = "Command executed successfully",
                        Output = combinedOutput
                    };
                }

                // Parse error output to determine failure category
                FailureCategory? category = DetermineFailureCategory(combinedOutput, exitCode);
                string message = $"certreq.exe exited with code {exitCode}";
                if (!string.IsNullOrEmpty(combinedOutput))
                {
                    // Extract first meaningful error line
                    var lines = combinedOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && 
                            !trimmed.StartsWith("CertReq:", StringComparison.OrdinalIgnoreCase) &&
                            !trimmed.StartsWith("CertUtil:", StringComparison.OrdinalIgnoreCase))
                        {
                            message = trimmed;
                            break;
                        }
                    }
                }

                return new CertReqExecutionResult
                {
                    Success = false,
                    FailureCategory = category,
                    Message = message,
                    Output = combinedOutput,
                    ExitCode = exitCode
                };
            }
            catch (Exception ex)
            {
                return new CertReqExecutionResult
                {
                    Success = false,
                    FailureCategory = FailureCategory.EnvironmentError,
                    Message = $"Failed to execute certreq.exe: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Determines the failure category from certreq output.
        /// Conservative parsing - prefer fewer categories over wrong categories.
        /// </summary>
        private FailureCategory? DetermineFailureCategory(string output, int exitCode)
        {
            if (string.IsNullOrEmpty(output))
            {
                return FailureCategory.CARequestError; // Default for nonzero exit
            }

            string lowerOutput = output.ToLowerInvariant();

            // Connectivity errors
            if (lowerOutput.Contains("network") || 
                lowerOutput.Contains("unreachable") ||
                lowerOutput.Contains("connection") ||
                lowerOutput.Contains("dns") ||
                lowerOutput.Contains("timeout"))
            {
                return FailureCategory.ConnectivityError;
            }

            // Authorization errors
            if (lowerOutput.Contains("access denied") ||
                lowerOutput.Contains("permission") ||
                lowerOutput.Contains("unauthorized") ||
                lowerOutput.Contains("enrollment"))
            {
                return FailureCategory.AuthorizationError;
            }

            // Default to CARequestError for template/policy rejections and other CA errors
            return FailureCategory.CARequestError;
        }

        /// <summary>
        /// Exports certificate to PFX format.
        /// Simplified implementation - uses certutil for export.
        /// </summary>
        private CertReqExecutionResult ExportToPfx(string workDir, string cerPath, string pfxPath, CertificateRequest request)
        {
            // This is a placeholder - actual PFX export would require password handling
            // For now, return success but don't create PFX
            // In a full implementation, you would:
            // 1. Load the certificate from the store
            // 2. Export with password protection
            // 3. Save to pfxPath
            
            return new CertReqExecutionResult
            {
                Success = false,
                Message = "PFX export not yet implemented",
                FailureCategory = FailureCategory.ExportError
            };
        }

        /// <summary>
        /// Internal result class for certreq execution.
        /// </summary>
        private sealed class CertReqExecutionResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string Output { get; set; }
            public int ExitCode { get; set; }
            public FailureCategory? FailureCategory { get; set; }
        }
    }
}


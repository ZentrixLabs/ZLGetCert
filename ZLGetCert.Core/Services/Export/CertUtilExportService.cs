using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ZentrixLabs.ZLGetCert.Core.Contracts;
using ZentrixLabs.ZLGetCert.Core.Pipeline;
using ZentrixLabs.ZLGetCert.Core.Services;

namespace ZentrixLabs.ZLGetCert.Core.Services.Export
{
    /// <summary>
    /// Export service implementation using certutil.exe for certificate conversions.
    /// </summary>
    public sealed class CertUtilExportService : IExportService
    {
        /// <summary>
        /// Performs exports (PEM etc) based on request + issued artifacts.
        /// Enforces overwrite rules and produces ExportedArtifact records.
        /// </summary>
        public ExportResult Export(ExecutionContext context, CaIssueResult issued)
        {
            var request = context.Request;
            var exported = new List<ExportedArtifact>();
            var errors = new List<string>();

            // Enforce invariants
            var invariantResult = EnforceInvariants(request, issued);
            if (invariantResult != null)
            {
                return invariantResult;
            }

            // Locate certutil.exe
            string certutilPath = LocateCertUtil();
            if (certutilPath == null)
            {
                return new ExportResult
                {
                    Success = false,
                    FailureCategory = FailureCategory.EnvironmentError,
                    Message = "certutil.exe not found. Expected location: SystemRoot\\System32\\certutil.exe",
                    Exported = exported
                };
            }

            // Process each export target
            bool allSucceeded = true;

            // LeafPem export
            if (request.Exports?.LeafPem != null && request.Exports.LeafPem.Enabled)
            {
                try
                {
                    var result = ExportLeafPem(request, issued, certutilPath);
                    exported.Add(result);
                    if (!result.Written)
                    {
                        allSucceeded = false;
                        if (string.IsNullOrWhiteSpace(issued.CerPath) || !File.Exists(issued.CerPath))
                        {
                            errors.Add("No issued CER path to export");
                        }
                        else
                        {
                            errors.Add("LeafPem export failed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    allSucceeded = false;
                    errors.Add($"LeafPem export failed: {ex.Message}");
                    exported.Add(new ExportedArtifact
                    {
                        Name = "leaf.pem",
                        Path = ResolvePath(request.Exports.LeafPem.Path),
                        Written = false
                    });
                }
            }

            // CaBundlePem export
            if (request.Exports?.CaBundlePem != null && request.Exports.CaBundlePem.Enabled)
            {
                try
                {
                    var result = ExportCaBundlePem(request, issued, certutilPath);
                    exported.Add(result);
                    if (!result.Written)
                    {
                        allSucceeded = false;
                        if (string.IsNullOrWhiteSpace(issued.ChainPath) || !File.Exists(issued.ChainPath))
                        {
                            errors.Add("No chain path available. Chain must be provided by CA issuance.");
                        }
                        else
                        {
                            errors.Add("CaBundlePem export failed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    allSucceeded = false;
                    errors.Add($"CaBundlePem export failed: {ex.Message}");
                    exported.Add(new ExportedArtifact
                    {
                        Name = "ca-bundle.pem",
                        Path = ResolvePath(request.Exports.CaBundlePem.Path),
                        Written = false
                    });
                }
            }

            // KeyPem export
            if (request.Exports?.KeyPem != null && request.Exports.KeyPem.Enabled)
            {
                try
                {
                    var result = ExportKeyPem(request, issued, certutilPath);
                    exported.Add(result);
                    if (!result.Written)
                    {
                        allSucceeded = false;
                        // Check if it's the "not implemented" case
                        if (request.Crypto != null && request.Crypto.ExportablePrivateKey && 
                            !string.IsNullOrWhiteSpace(issued.PfxPath) && File.Exists(issued.PfxPath))
                        {
                            errors.Add("Key PEM export not implemented yet");
                        }
                        else if (request.Crypto == null || !request.Crypto.ExportablePrivateKey)
                        {
                            errors.Add("KeyPem export failed: ExportablePrivateKey must be true");
                        }
                        else
                        {
                            errors.Add("KeyPem export failed: No PFX path available");
                        }
                    }
                }
                catch (Exception ex)
                {
                    allSucceeded = false;
                    errors.Add($"KeyPem export failed: {ex.Message}");
                    exported.Add(new ExportedArtifact
                    {
                        Name = "leaf.key.pem",
                        Path = ResolvePath(request.Exports.KeyPem.Path),
                        Written = false
                    });
                }
            }

            // Determine failure category based on error types
            FailureCategory? failureCategory = null;
            if (!allSucceeded)
            {
                // Check if any errors are FormatError (missing input artifacts)
                bool hasFormatError = errors.Any(e => 
                    e.Contains("No issued CER path") || 
                    e.Contains("No chain path") || 
                    e.Contains("No PFX path"));
                
                if (hasFormatError)
                {
                    failureCategory = FailureCategory.FormatError;
                }
                else
                {
                    failureCategory = FailureCategory.ExportError;
                }
            }

            return new ExportResult
            {
                Success = allSucceeded,
                Message = allSucceeded ? "All exports completed successfully" : string.Join("; ", errors),
                Exported = exported,
                FailureCategory = failureCategory
            };
        }

        /// <summary>
        /// Enforces export invariants before processing.
        /// </summary>
        private ExportResult EnforceInvariants(CertificateRequest request, CaIssueResult issued)
        {
            // If CA issuance failed, return failure
            if (!issued.Success)
            {
                return new ExportResult
                {
                    Success = false,
                    FailureCategory = issued.FailureCategory ?? FailureCategory.CARequestError,
                    Message = issued.Message ?? "CA issuance failed",
                    Exported = new List<ExportedArtifact>()
                };
            }

            // Check export targets: if enabled, path must be non-empty
            // Also check overwrite rules
            if (request.Exports != null)
            {
                if (request.Exports.LeafPem != null && request.Exports.LeafPem.Enabled)
                {
                    if (string.IsNullOrWhiteSpace(request.Exports.LeafPem.Path))
                    {
                        return new ExportResult
                        {
                            Success = false,
                            FailureCategory = FailureCategory.ExportError,
                            Message = "LeafPem export is enabled but Path is empty",
                            Exported = new List<ExportedArtifact>()
                        };
                    }
                    // Check overwrite
                    string leafPath = ResolvePath(request.Exports.LeafPem.Path);
                    if (File.Exists(leafPath) && !request.Overwrite)
                    {
                        return new ExportResult
                        {
                            Success = false,
                            FailureCategory = FailureCategory.ExportError,
                            Message = $"LeafPem export target already exists and overwrite is false: {leafPath}",
                            Exported = new List<ExportedArtifact>()
                        };
                    }
                }

                if (request.Exports.CaBundlePem != null && request.Exports.CaBundlePem.Enabled)
                {
                    if (string.IsNullOrWhiteSpace(request.Exports.CaBundlePem.Path))
                    {
                        return new ExportResult
                        {
                            Success = false,
                            FailureCategory = FailureCategory.ExportError,
                            Message = "CaBundlePem export is enabled but Path is empty",
                            Exported = new List<ExportedArtifact>()
                        };
                    }
                    // Check overwrite
                    string bundlePath = ResolvePath(request.Exports.CaBundlePem.Path);
                    if (File.Exists(bundlePath) && !request.Overwrite)
                    {
                        return new ExportResult
                        {
                            Success = false,
                            FailureCategory = FailureCategory.ExportError,
                            Message = $"CaBundlePem export target already exists and overwrite is false: {bundlePath}",
                            Exported = new List<ExportedArtifact>()
                        };
                    }
                }

                if (request.Exports.KeyPem != null && request.Exports.KeyPem.Enabled)
                {
                    if (string.IsNullOrWhiteSpace(request.Exports.KeyPem.Path))
                    {
                        return new ExportResult
                        {
                            Success = false,
                            FailureCategory = FailureCategory.ExportError,
                            Message = "KeyPem export is enabled but Path is empty",
                            Exported = new List<ExportedArtifact>()
                        };
                    }
                    // Check overwrite
                    string keyPath = ResolvePath(request.Exports.KeyPem.Path);
                    if (File.Exists(keyPath) && !request.Overwrite)
                    {
                        return new ExportResult
                        {
                            Success = false,
                            FailureCategory = FailureCategory.ExportError,
                            Message = $"KeyPem export target already exists and overwrite is false: {keyPath}",
                            Exported = new List<ExportedArtifact>()
                        };
                    }
                }
            }

            return null; // All invariants passed
        }

        /// <summary>
        /// Locates certutil.exe using the same approach as ToolingPresenceCheck.
        /// </summary>
        private string LocateCertUtil()
        {
            var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            if (string.IsNullOrEmpty(systemRoot))
            {
                systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            }

            var system32Path = Path.Combine(systemRoot, "System32");
            var certutilPath = Path.Combine(system32Path, "certutil.exe");

            if (File.Exists(certutilPath))
            {
                return certutilPath;
            }

            return null;
        }

        /// <summary>
        /// Exports the leaf certificate as PEM.
        /// </summary>
        private ExportedArtifact ExportLeafPem(CertificateRequest request, CaIssueResult issued, string certutilPath)
        {
            var artifact = new ExportedArtifact
            {
                Name = "leaf.pem",
                Written = false
            };

            try
            {
                // Check if CerPath exists
                if (string.IsNullOrWhiteSpace(issued.CerPath) || !File.Exists(issued.CerPath))
                {
                    artifact.Path = ResolvePath(request.Exports.LeafPem.Path);
                    return artifact;
                }

                // Resolve output path
                string outputPath = ResolvePath(request.Exports.LeafPem.Path);
                artifact.Path = outputPath;

                // Check overwrite
                if (File.Exists(outputPath) && !request.Overwrite)
                {
                    return artifact;
                }

                // Convert CER to PEM using certutil
                // certutil -encode <input.cer> <output.pem>
                string tempPath = GetTempPath(outputPath);
                bool success = RunCertUtilEncode(issued.CerPath, tempPath, certutilPath);

                if (success && File.Exists(tempPath))
                {
                    // Verify it contains BEGIN CERTIFICATE
                    string content = File.ReadAllText(tempPath);
                    if (content.Contains("BEGIN CERTIFICATE"))
                    {
                        // Atomic move
                        if (File.Exists(outputPath))
                        {
                            File.Delete(outputPath);
                        }
                        File.Move(tempPath, outputPath);

                        // Compute metadata
                        var fileInfo = new FileInfo(outputPath);
                        artifact.Written = true;
                        artifact.SizeBytes = fileInfo.Length;
                        artifact.Sha256 = ComputeSha256(outputPath);
                    }
                    else
                    {
                        // Clean up temp file
                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // ExportError with evidence
                throw new InvalidOperationException($"LeafPem export failed: {ex.Message}", ex);
            }

            return artifact;
        }

        /// <summary>
        /// Exports the CA bundle as PEM.
        /// </summary>
        private ExportedArtifact ExportCaBundlePem(CertificateRequest request, CaIssueResult issued, string certutilPath)
        {
            var artifact = new ExportedArtifact
            {
                Name = "ca-bundle.pem",
                Written = false
            };

            try
            {
                // Check if ChainPath exists
                if (string.IsNullOrWhiteSpace(issued.ChainPath) || !File.Exists(issued.ChainPath))
                {
                    artifact.Path = ResolvePath(request.Exports.CaBundlePem.Path);
                    return artifact;
                }

                // Resolve output path
                string outputPath = ResolvePath(request.Exports.CaBundlePem.Path);
                artifact.Path = outputPath;

                // Check overwrite
                if (File.Exists(outputPath) && !request.Overwrite)
                {
                    return artifact;
                }

                // Convert chain to PEM using certutil
                string tempPath = GetTempPath(outputPath);
                bool success = RunCertUtilEncode(issued.ChainPath, tempPath, certutilPath);

                if (success && File.Exists(tempPath))
                {
                    // Verify it contains BEGIN CERTIFICATE
                    string content = File.ReadAllText(tempPath);
                    if (content.Contains("BEGIN CERTIFICATE"))
                    {
                        // Atomic move
                        if (File.Exists(outputPath))
                        {
                            File.Delete(outputPath);
                        }
                        File.Move(tempPath, outputPath);

                        // Compute metadata
                        var fileInfo = new FileInfo(outputPath);
                        artifact.Written = true;
                        artifact.SizeBytes = fileInfo.Length;
                        artifact.Sha256 = ComputeSha256(outputPath);
                        artifact.CertificateCount = CountCertificates(content);
                    }
                    else
                    {
                        // Clean up temp file
                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // ExportError with evidence
                throw new InvalidOperationException($"CaBundlePem export failed: {ex.Message}", ex);
            }

            return artifact;
        }

        /// <summary>
        /// Exports the private key as PEM.
        /// </summary>
        private ExportedArtifact ExportKeyPem(CertificateRequest request, CaIssueResult issued, string certutilPath)
        {
            var artifact = new ExportedArtifact
            {
                Name = "leaf.key.pem",
                Written = false
            };

            try
            {
                // Resolve output path
                string outputPath = ResolvePath(request.Exports.KeyPem.Path);
                artifact.Path = outputPath;

                // Require ExportablePrivateKey == true
                if (request.Crypto == null || !request.Crypto.ExportablePrivateKey)
                {
                    return artifact;
                }

                // Check if PfxPath exists
                if (string.IsNullOrWhiteSpace(issued.PfxPath) || !File.Exists(issued.PfxPath))
                {
                    return artifact;
                }

                // Check overwrite
                if (File.Exists(outputPath) && !request.Overwrite)
                {
                    return artifact;
                }

                // Key PEM export is not feasible with certutil alone
                // certutil cannot extract private keys from PFX without password
                // Return artifact with Written=false (already set)
                // The error will be captured in the ExportResult message
                return artifact;
            }
            catch (Exception ex)
            {
                // ExportError with evidence
                throw new InvalidOperationException($"KeyPem export failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Resolves a path to an absolute path.
        /// </summary>
        private string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        /// <summary>
        /// Gets a temporary file path in the same directory as the target.
        /// </summary>
        private string GetTempPath(string targetPath)
        {
            string directory = Path.GetDirectoryName(targetPath);
            string fileName = Path.GetFileNameWithoutExtension(targetPath);
            string extension = Path.GetExtension(targetPath);
            string guid = Guid.NewGuid().ToString("N");
            return Path.Combine(directory, $"{fileName}.tmp-{guid}{extension}");
        }

        /// <summary>
        /// Runs certutil -encode to convert a certificate file to PEM format.
        /// </summary>
        private bool RunCertUtilEncode(string inputPath, string outputPath, string certutilPath)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = certutilPath,
                        Arguments = $"-encode \"{inputPath}\" \"{outputPath}\"",
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

                return process.ExitCode == 0 && File.Exists(outputPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Computes SHA-256 hash of a file.
        /// </summary>
        private string ComputeSha256(string filePath)
        {
            try
            {
                using (var sha256 = SHA256Managed.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Counts the number of certificates in a PEM bundle by counting "BEGIN CERTIFICATE" occurrences.
        /// </summary>
        private int? CountCertificates(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            try
            {
                int count = 0;
                int index = 0;
                string pattern = "-----BEGIN CERTIFICATE-----";
                while ((index = content.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    count++;
                    index += pattern.Length;
                }
                return count > 0 ? (int?)count : null;
            }
            catch
            {
                return null;
            }
        }
    }
}


using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ZLGetCert.Models;

namespace ZLGetCert.Services
{
    /// <summary>
    /// Service for OpenSSL integration and detection
    /// </summary>
    public class OpenSSLService
    {
        private static readonly Lazy<OpenSSLService> _instance = new Lazy<OpenSSLService>(() => new OpenSSLService());
        public static OpenSSLService Instance => _instance.Value;

        private readonly LoggingService _logger;
        private OpenSSLConfig _config;

        private OpenSSLService()
        {
            _logger = LoggingService.Instance;
            InitializeOpenSSL();
        }

        /// <summary>
        /// Initialize OpenSSL detection and configuration
        /// </summary>
        private void InitializeOpenSSL()
        {
            try
            {
                var appConfig = ConfigurationService.Instance.GetConfiguration();
                _config = appConfig.OpenSSL;

                if (_config.AutoDetect)
                {
                    DetectOpenSSL();
                }
                else if (!string.IsNullOrEmpty(_config.ExecutablePath))
                {
                    ValidateOpenSSLPath(_config.ExecutablePath);
                }

                _logger.LogInfo("OpenSSL service initialized. Available: {0}, Path: {1}", 
                    _config.IsAvailable, _config.ExecutablePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize OpenSSL service");
                _config.IsAvailable = false;
            }
        }

        /// <summary>
        /// Detect OpenSSL installation
        /// </summary>
        private void DetectOpenSSL()
        {
            var commonPaths = new[]
            {
                "C:\\Program Files\\OpenSSL-Win64\\bin\\openssl.exe",
                "C:\\Program Files (x86)\\OpenSSL-Win32\\bin\\openssl.exe",
                "C:\\OpenSSL-Win64\\bin\\openssl.exe",
                "C:\\OpenSSL-Win32\\bin\\openssl.exe",
                "C:\\OpenSSL\\bin\\openssl.exe"
            };

            // Check PATH environment variable first
            if (DetectOpenSSLInPath())
                return;

            // Check common installation paths
            foreach (var path in commonPaths)
            {
                if (File.Exists(path) && ValidateOpenSSLPath(path))
                {
                    _config.ExecutablePath = path;
                    _config.IsAvailable = true;
                    return;
                }
            }

            _config.IsAvailable = false;
            _logger.LogWarning("OpenSSL not found in common installation paths");
        }

        /// <summary>
        /// Detect OpenSSL in system PATH
        /// </summary>
        private bool DetectOpenSSLInPath()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "openssl",
                        Arguments = "version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit(5000); // 5 second timeout

                if (process.ExitCode == 0)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    if (output.Contains("OpenSSL"))
                    {
                        _config.ExecutablePath = "openssl";
                        _config.IsAvailable = true;
                        ExtractVersionFromOutput(output);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("OpenSSL not found in PATH: {0}", ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Validate OpenSSL executable path
        /// </summary>
        private bool ValidateOpenSSLPath(string path)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit(5000); // 5 second timeout

                if (process.ExitCode == 0)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    if (output.Contains("OpenSSL"))
                    {
                        _config.IsAvailable = true;
                        ExtractVersionFromOutput(output);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Failed to validate OpenSSL path {0}: {1}", path, ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Extract version information from OpenSSL output
        /// </summary>
        private void ExtractVersionFromOutput(string output)
        {
            try
            {
                var match = Regex.Match(output, @"OpenSSL\s+([\d\.]+[a-z]*)");
                if (match.Success)
                {
                    _config.Version = match.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Failed to extract OpenSSL version: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Check if OpenSSL is available
        /// </summary>
        public bool IsAvailable => _config?.IsAvailable ?? false;

        /// <summary>
        /// Get OpenSSL configuration
        /// </summary>
        public OpenSSLConfig GetConfig() => _config;

        /// <summary>
        /// Extract PEM and KEY files from PFX
        /// </summary>
        public bool ExtractPemAndKey(string pfxPath, string password, string outputDir, string certificateName)
        {
            if (!IsAvailable)
            {
                _logger.LogError("OpenSSL is not available for PEM/KEY extraction");
                return false;
            }

            try
            {
                var pemPath = Path.Combine(outputDir, $"{certificateName}.pem");
                var keyPath = Path.Combine(outputDir, $"{certificateName}.key");
                var tempKeyPath = Path.Combine(outputDir, "temp_key.pem");

                _logger.LogInfo("Extracting PEM and KEY files from {0}", pfxPath);

                // Extract private key (with password)
                if (!RunOpenSSLCommand($"pkcs12 -passin pass:{password} -in \"{pfxPath}\" -nocerts -passout pass:{password} -out \"{tempKeyPath}\""))
                {
                    _logger.LogError("Failed to extract private key from PFX");
                    return false;
                }

                // Extract certificate
                if (!RunOpenSSLCommand($"pkcs12 -passin pass:{password} -in \"{pfxPath}\" -clcerts -nokeys -out \"{pemPath}\""))
                {
                    _logger.LogError("Failed to extract certificate from PFX");
                    return false;
                }

                // Remove password from private key
                if (!RunOpenSSLCommand($"rsa -passin pass:{password} -in \"{tempKeyPath}\" -out \"{keyPath}\""))
                {
                    _logger.LogError("Failed to remove password from private key");
                    return false;
                }

                // Clean up temporary file
                if (File.Exists(tempKeyPath))
                {
                    File.Delete(tempKeyPath);
                }

                _logger.LogInfo("Successfully extracted PEM and KEY files");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting PEM and KEY files");
                return false;
            }
        }

        /// <summary>
        /// Extract certificate chain (root/intermediate) from PFX
        /// </summary>
        public bool ExtractCertificateChain(string pfxPath, string password, string outputDir, string chainName = "certificate-chain")
        {
            if (!IsAvailable)
            {
                _logger.LogError("OpenSSL is not available for certificate chain extraction");
                return false;
            }

            try
            {
                var chainPath = Path.Combine(outputDir, $"{chainName}.pem");

                _logger.LogInfo("Extracting certificate chain from {0}", pfxPath);

                // Extract certificate chain
                if (!RunOpenSSLCommand($"pkcs12 -in \"{pfxPath}\" -nodes -nokeys -cacerts -out \"{chainPath}\""))
                {
                    _logger.LogError("Failed to extract certificate chain from PFX");
                    return false;
                }

                _logger.LogInfo("Successfully extracted certificate chain to {0}", chainPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting certificate chain");
                return false;
            }
        }

        /// <summary>
        /// Run OpenSSL command
        /// </summary>
        private bool RunOpenSSLCommand(string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _config.ExecutablePath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit(30000); // 30 second timeout

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    _logger.LogError("OpenSSL command failed: {0}", error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running OpenSSL command: {0}", arguments);
                return false;
            }
        }

        /// <summary>
        /// Reinitialize OpenSSL detection
        /// </summary>
        public void Reinitialize()
        {
            InitializeOpenSSL();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Runtime.InteropServices;

namespace ZLGetCert.Services
{
    /// <summary>
    /// Service for exporting PEM and KEY files from PFX certificates using pure .NET
    /// No external dependencies (OpenSSL) required
    /// </summary>
    public class PemExportService
    {
        private static readonly Lazy<PemExportService> _instance = new Lazy<PemExportService>(() => new PemExportService());
        public static PemExportService Instance => _instance.Value;

        private readonly LoggingService _logger;
        private readonly AuditService _auditService;

        private PemExportService()
        {
            _logger = LoggingService.Instance;
            _auditService = AuditService.Instance;
        }

        /// <summary>
        /// Extract PEM certificate and unencrypted KEY file from PFX
        /// </summary>
        /// <param name="pfxPath">Path to PFX file</param>
        /// <param name="password">PFX password</param>
        /// <param name="outputDir">Output directory</param>
        /// <param name="certificateName">Base name for output files</param>
        /// <returns>True if successful</returns>
        public bool ExtractPemAndKey(string pfxPath, string password, string outputDir, string certificateName)
        {
            try
            {
                _logger.LogInfo("Extracting PEM and KEY files from {0} using native .NET", pfxPath);
                _logger.LogInfo("Output directory: {0}, Certificate name: {1}", outputDir, certificateName);

                if (!File.Exists(pfxPath))
                {
                    _logger.LogError("PFX file not found: {0}", pfxPath);
                    return false;
                }

                // Load the PFX certificate with private key
                var cert = new X509Certificate2(pfxPath, password, X509KeyStorageFlags.Exportable);

                if (!cert.HasPrivateKey)
                {
                    _logger.LogError("Certificate does not contain a private key");
                    return false;
                }

                var pemPath = Path.Combine(outputDir, $"{certificateName}.pem");
                var keyPath = Path.Combine(outputDir, $"{certificateName}.key");

                // Export certificate to PEM format
                ExportCertificateToPem(cert, pemPath);

                // Export private key to KEY format (unencrypted - required for web servers)
                ExportPrivateKeyToKey(cert, keyPath);

                // SECURITY: Set restrictive file permissions on both files
                SetRestrictiveFilePermissions(pemPath);
                SetRestrictiveFilePermissions(keyPath);

                // SECURITY: Log warning about unencrypted key
                _logger.LogWarning(
                    "SECURITY: Unencrypted private key exported to {0}. " +
                    "File permissions set to owner-only. " +
                    "Ensure file is protected and securely deleted after deployment to server.", 
                    keyPath);

                // SECURITY AUDIT: Log private key export (security-critical event)
                _auditService.LogAuditEvent(
                    AuditService.AuditEventType.PrivateKeyExportedUnencrypted,
                    $"Unencrypted private key exported to: {keyPath}. " +
                    $"File permissions set to owner-only. " +
                    $"PEM certificate exported to: {pemPath}",
                    certificateName: certificateName,
                    thumbprint: cert.Thumbprint,
                    isSecurityCritical: true);

                _logger.LogInfo("Successfully extracted PEM and KEY files with restricted permissions");
                return true;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Cryptographic error extracting PEM and KEY files. Check password.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting PEM and KEY files");
                return false;
            }
        }

        /// <summary>
        /// Extract certificate chain from PFX with proper ordering (ICA → Root CA)
        /// </summary>
        /// <param name="pfxPath">Path to PFX file</param>
        /// <param name="password">PFX password</param>
        /// <param name="outputDir">Output directory</param>
        /// <param name="chainName">Base name for chain file</param>
        /// <returns>True if successful</returns>
        public bool ExtractCertificateChain(string pfxPath, string password, string outputDir, string chainName = "ca-bundle")
        {
            try
            {
                _logger.LogInfo("Extracting certificate chain (CA bundle) from {0}", pfxPath);
                _logger.LogInfo("Output directory: {0}, Chain name: {1}", outputDir, chainName);

                if (!File.Exists(pfxPath))
                {
                    _logger.LogError("PFX file not found: {0}", pfxPath);
                    return false;
                }

                // Load the certificate with private key from PFX
                var cert = new X509Certificate2(pfxPath, password, X509KeyStorageFlags.Exportable);

                // Build the certificate chain using Windows chain building
                using (var chain = new X509Chain())
                {
                    // Configure chain building options
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // Skip revocation checking for chain building
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    
                    // Build the chain
                    bool chainBuilt = chain.Build(cert);
                    
                    if (!chainBuilt)
                    {
                        _logger.LogWarning("Certificate chain building failed, but continuing with available certificates");
                    }

                    // Extract intermediate and root certificates (skip the leaf certificate at index 0)
                    var chainCerts = new List<X509Certificate2>();
                    for (int i = 1; i < chain.ChainElements.Count; i++)
                    {
                        chainCerts.Add(chain.ChainElements[i].Certificate);
                    }

                    if (chainCerts.Count == 0)
                    {
                        _logger.LogWarning("No intermediate/root certificates found in chain (this is normal for self-signed or direct CA certs)");
                        return true; // Not an error - some certs don't have chains
                    }

                    // Export the chain to PEM format
                    var chainPath = Path.Combine(outputDir, $"{chainName}.pem");
                    var chainBuilder = new StringBuilder();

                    foreach (var chainCert in chainCerts)
                    {
                        chainBuilder.AppendLine("-----BEGIN CERTIFICATE-----");
                        chainBuilder.AppendLine(Convert.ToBase64String(chainCert.RawData, Base64FormattingOptions.InsertLineBreaks));
                        chainBuilder.AppendLine("-----END CERTIFICATE-----");
                    }

                    File.WriteAllText(chainPath, chainBuilder.ToString(), Encoding.ASCII);
                    _logger.LogInfo("Successfully extracted {0} certificate(s) to CA bundle: {1}", chainCerts.Count, chainPath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting certificate chain");
                return false;
            }
        }

        /// <summary>
        /// Order certificate chain from ICA (closest to leaf) to Root CA
        /// </summary>
        private List<X509Certificate2> OrderCertificateChain(X509Certificate2 leafCert, List<X509Certificate2> chainCerts)
        {
            var orderedChain = new List<X509Certificate2>();
            
            if (leafCert == null || chainCerts.Count == 0)
                return orderedChain;

            // Create a lookup by subject for quick access
            var certsBySubject = new Dictionary<string, X509Certificate2>(StringComparer.OrdinalIgnoreCase);
            foreach (var cert in chainCerts)
            {
                certsBySubject[cert.Subject] = cert;
            }

            // Start with the leaf cert's issuer and work up the chain
            var currentIssuer = leafCert.Issuer;
            
            while (certsBySubject.ContainsKey(currentIssuer))
            {
                var issuerCert = certsBySubject[currentIssuer];
                orderedChain.Add(issuerCert);
                
                // Move to the next level (this issuer's issuer)
                currentIssuer = issuerCert.Issuer;
                
                // Stop if we've reached a self-signed cert (root CA)
                if (issuerCert.Subject.Equals(issuerCert.Issuer, StringComparison.OrdinalIgnoreCase))
                    break;
            }

            // Add any remaining certs that weren't in the direct chain (rare, but possible)
            foreach (var cert in chainCerts)
            {
                if (!orderedChain.Contains(cert))
                {
                    orderedChain.Add(cert);
                }
            }

            return orderedChain;
        }

        /// <summary>
        /// Export certificate to PEM format
        /// </summary>
        private void ExportCertificateToPem(X509Certificate2 cert, string pemPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-----BEGIN CERTIFICATE-----");
            sb.AppendLine(Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks));
            sb.AppendLine("-----END CERTIFICATE-----");

            File.WriteAllText(pemPath, sb.ToString(), Encoding.ASCII);
            _logger.LogInfo("Certificate exported to PEM: {0}", pemPath);
        }

        /// <summary>
        /// Export private key to unencrypted KEY format (PKCS#8)
        /// </summary>
        private void ExportPrivateKeyToKey(X509Certificate2 cert, string keyPath)
        {
            // Get the private key as RSA (most common for SSL certificates)
            using (var rsa = cert.GetRSAPrivateKey())
            {
                if (rsa != null)
                {
                    ExportRsaPrivateKey(rsa, keyPath);
                    return;
                }
            }

            // Try ECDsa (for EC certificates)
            using (var ecdsa = cert.GetECDsaPrivateKey())
            {
                if (ecdsa != null)
                {
                    ExportEcPrivateKey(ecdsa, keyPath);
                    return;
                }
            }

            throw new NotSupportedException("Certificate private key type is not supported (not RSA or ECDSA)");
        }

        /// <summary>
        /// Export RSA private key to PKCS#1 format (traditional RSA format) - .NET Framework 4.8 compatible
        /// </summary>
        private void ExportRsaPrivateKey(RSA rsa, string keyPath)
        {
            // Export RSA parameters
            RSAParameters parameters = rsa.ExportParameters(true);
            
            // Encode to PKCS#1 DER format
            byte[] privateKeyBytes = EncodeRsaPrivateKeyToPkcs1(parameters);
            
            var sb = new StringBuilder();
            sb.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
            sb.AppendLine(Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks));
            sb.AppendLine("-----END RSA PRIVATE KEY-----");
            File.WriteAllText(keyPath, sb.ToString(), Encoding.ASCII);
            _logger.LogInfo("RSA private key exported to KEY (PKCS#1): {0}", keyPath);
        }

        /// <summary>
        /// Export EC private key - .NET Framework 4.8 compatible
        /// </summary>
        private void ExportEcPrivateKey(ECDsa ecdsa, string keyPath)
        {
            // For .NET Framework 4.8, EC key export is complex
            // For now, throw a helpful exception
            throw new NotSupportedException(
                "EC (Elliptic Curve) certificates are not commonly used for SSL/TLS. " +
                "This certificate appears to use an EC key. Most SSL certificates use RSA keys. " +
                "If you need EC support, please contact support.");
        }

        /// <summary>
        /// Encode RSA private key parameters to PKCS#1 DER format
        /// Based on RFC 3447 and ASN.1 encoding
        /// </summary>
        private byte[] EncodeRsaPrivateKeyToPkcs1(RSAParameters parameters)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // PKCS#1 RSAPrivateKey ::= SEQUENCE {
                //   version           Version (0 for two-prime RSA),
                //   modulus           INTEGER,
                //   publicExponent    INTEGER,
                //   privateExponent   INTEGER,
                //   prime1            INTEGER,
                //   prime2            INTEGER,
                //   exponent1         INTEGER,
                //   exponent2         INTEGER,
                //   coefficient       INTEGER
                // }

                // Write SEQUENCE tag
                var sequenceData = EncodeRsaParameters(parameters);
                writer.Write((byte)0x30); // SEQUENCE tag
                WriteDerLength(writer, sequenceData.Length);
                writer.Write(sequenceData);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Encode RSA parameters as DER integers
        /// </summary>
        private byte[] EncodeRsaParameters(RSAParameters parameters)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // Version (0)
                WriteInteger(writer, new byte[] { 0 });
                
                // Modulus
                WriteInteger(writer, parameters.Modulus);
                
                // Public exponent
                WriteInteger(writer, parameters.Exponent);
                
                // Private exponent
                WriteInteger(writer, parameters.D);
                
                // Prime1 (P)
                WriteInteger(writer, parameters.P);
                
                // Prime2 (Q)
                WriteInteger(writer, parameters.Q);
                
                // Exponent1 (DP)
                WriteInteger(writer, parameters.DP);
                
                // Exponent2 (DQ)
                WriteInteger(writer, parameters.DQ);
                
                // Coefficient (InverseQ)
                WriteInteger(writer, parameters.InverseQ);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Write a DER-encoded INTEGER
        /// </summary>
        private void WriteInteger(BinaryWriter writer, byte[] value)
        {
            writer.Write((byte)0x02); // INTEGER tag
            
            // Remove leading zeros, but keep one if the high bit is set (to indicate positive number)
            byte[] data = value;
            int startIndex = 0;
            
            // Skip leading zeros
            while (startIndex < data.Length && data[startIndex] == 0)
                startIndex++;
            
            // If all zeros or high bit is set, we need to keep/add a zero byte
            bool needsLeadingZero = false;
            if (startIndex >= data.Length)
            {
                // All zeros - write single zero byte
                data = new byte[] { 0 };
                startIndex = 0;
            }
            else if (data[startIndex] >= 0x80)
            {
                // High bit set - need leading zero to indicate positive number
                needsLeadingZero = true;
            }
            
            int length = data.Length - startIndex + (needsLeadingZero ? 1 : 0);
            WriteDerLength(writer, length);
            
            if (needsLeadingZero)
                writer.Write((byte)0);
                
            writer.Write(data, startIndex, data.Length - startIndex);
        }

        /// <summary>
        /// Write DER length encoding
        /// </summary>
        private void WriteDerLength(BinaryWriter writer, int length)
        {
            if (length < 0x80)
            {
                // Short form - length in one byte
                writer.Write((byte)length);
            }
            else
            {
                // Long form - first byte indicates number of length bytes
                byte[] lengthBytes;
                if (length <= 0xFF)
                {
                    lengthBytes = new byte[] { (byte)length };
                }
                else if (length <= 0xFFFF)
                {
                    lengthBytes = new byte[] { (byte)(length >> 8), (byte)(length & 0xFF) };
                }
                else if (length <= 0xFFFFFF)
                {
                    lengthBytes = new byte[] { (byte)(length >> 16), (byte)((length >> 8) & 0xFF), (byte)(length & 0xFF) };
                }
                else
                {
                    lengthBytes = new byte[] { 
                        (byte)(length >> 24), 
                        (byte)((length >> 16) & 0xFF), 
                        (byte)((length >> 8) & 0xFF), 
                        (byte)(length & 0xFF) 
                    };
                }
                
                writer.Write((byte)(0x80 | lengthBytes.Length));
                writer.Write(lengthBytes);
            }
        }

        /// <summary>
        /// Set restrictive NTFS permissions on file (owner + SYSTEM only)
        /// CRITICAL: Protects unencrypted private key files
        /// </summary>
        private void SetRestrictiveFilePermissions(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var security = fileInfo.GetAccessControl();

                // Disable inheritance - don't inherit permissions from parent folder
                security.SetAccessRuleProtection(true, false);

                // Remove all existing access rules
                var accessRules = security.GetAccessRules(true, false, typeof(NTAccount));
                foreach (FileSystemAccessRule rule in accessRules)
                {
                    security.RemoveAccessRule(rule);
                }

                // Add owner (current user) full control
                var owner = WindowsIdentity.GetCurrent().User;
                var ownerRule = new FileSystemAccessRule(
                    owner,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow);
                security.AddAccessRule(ownerRule);

                // Add SYSTEM account full control (required for services/scheduled tasks)
                var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                var systemRule = new FileSystemAccessRule(
                    systemSid,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow);
                security.AddAccessRule(systemRule);

                // Apply the security settings
                fileInfo.SetAccessControl(security);

                _logger.LogInfo("SECURITY: Set owner-only permissions on file: {0}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "SECURITY WARNING: Failed to set restrictive permissions on {0}. " +
                    "Manually set file permissions to owner-only! Error: {1}", 
                    filePath, ex.Message);
            }
        }

        /// <summary>
        /// Check if PEM export is available (always true for this implementation)
        /// </summary>
        public bool IsAvailable => true;
    }
}


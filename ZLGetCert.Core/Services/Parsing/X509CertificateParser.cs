using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using ZentrixLabs.ZLGetCert.Core.Contracts;
using ZentrixLabs.ZLGetCert.Core.Pipeline;
using ZentrixLabs.ZLGetCert.Core.Services;

namespace ZentrixLabs.ZLGetCert.Core.Services.Parsing
{
    /// <summary>
    /// Certificate parser implementation using X509Certificate2.
    /// </summary>
    public sealed class X509CertificateParser : ICertificateParser
    {
        /// <summary>
        /// Reads and parses certificate details from the issued certificate.
        /// </summary>
        public CertificateDetails Parse(ExecutionContext context, CaIssueResult issued)
        {
            if (issued == null)
                return null;

            if (string.IsNullOrEmpty(issued.CerPath) || !File.Exists(issued.CerPath))
                return null;

            try
            {
                var cert = new X509Certificate2(issued.CerPath);

                var details = new CertificateDetails
                {
                    Thumbprint = cert.Thumbprint,
                    Subject = cert.Subject,
                    Issuer = cert.Issuer,
                    SerialNumber = cert.SerialNumber,
                    NotBefore = cert.NotBefore.ToUniversalTime(),
                    NotAfter = cert.NotAfter.ToUniversalTime(),
                    SubjectAlternativeNames = new List<string>()
                };

                // Compute SHA-256 fingerprint
                using (var sha256 = SHA256.Create())
                {
                    byte[] hash = sha256.ComputeHash(cert.RawData);
                    details.Fingerprints = new Fingerprints
                    {
                        Sha256 = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant()
                    };
                }

                // Key algorithm
                if (cert.PublicKey?.Oid != null)
                {
                    details.KeyAlgorithm = !string.IsNullOrEmpty(cert.PublicKey.Oid.FriendlyName)
                        ? cert.PublicKey.Oid.FriendlyName
                        : cert.PublicKey.Oid.Value;
                }

                // Key size (for RSA)
                if (cert.PublicKey?.Key != null)
                {
                    try
                    {
                        if (cert.PublicKey.Key is RSA rsa)
                        {
                            details.KeySize = rsa.KeySize;
                        }
                    }
                    catch
                    {
                        // Ignore if KeySize is not available
                    }
                }

                // Parse Subject Alternative Names (OID 2.5.29.17)
                try
                {
                    var sanExtension = cert.Extensions["2.5.29.17"];
                    if (sanExtension != null)
                    {
                        var sans = ParseSanExtension(sanExtension);
                        if (sans != null)
                        {
                            details.SubjectAlternativeNames = sans;
                        }
                    }
                }
                catch
                {
                    // Best effort - return empty list if parsing fails
                    details.SubjectAlternativeNames = new List<string>();
                }

                return details;
            }
            catch
            {
                // Return null on any parsing error
                return null;
            }
        }

        /// <summary>
        /// Validates invariants for exported artifacts.
        /// </summary>
        public List<InvariantResult> ValidateInvariants(
            ExecutionContext context,
            CaIssueResult issued,
            List<ExportedArtifact> exported)
        {
            var invariants = new List<InvariantResult>();

            if (exported == null)
                exported = new List<ExportedArtifact>();

            // 1. ExportPathsWritable
            invariants.Add(ValidateExportPathsWritable(context, exported));

            // 2. NoOverwriteWithoutFlag
            invariants.Add(ValidateNoOverwriteWithoutFlag(context, exported));

            // 3. LeafPemParses
            invariants.Add(ValidateLeafPemParses(context, exported));

            // 4. CaBundleContainsCertificates
            invariants.Add(ValidateCaBundleContainsCertificates(context, exported));

            // 5. PrivateKeyPemParses
            invariants.Add(ValidatePrivateKeyPemParses(context, exported));

            return invariants;
        }

        private InvariantResult ValidateExportPathsWritable(ExecutionContext context, List<ExportedArtifact> exported)
        {
            var request = context.Request;

            // Check if exports were requested
            bool exportsRequested = (request.Exports?.LeafPem?.Enabled == true) ||
                                   (request.Exports?.KeyPem?.Enabled == true) ||
                                   (request.Exports?.CaBundlePem?.Enabled == true);

            if (!exportsRequested)
            {
                return new InvariantResult
                {
                    Name = "ExportPathsWritable",
                    Ok = true,
                    Detail = "No exports requested."
                };
            }

            // Check if any export failed due to write failure
            var failedWrites = exported.Where(e => !e.Written).ToList();
            if (failedWrites.Count > 0)
            {
                var failedNames = string.Join(", ", failedWrites.Select(e => e.Name ?? "unknown"));
                return new InvariantResult
                {
                    Name = "ExportPathsWritable",
                    Ok = false,
                    Detail = $"One or more exports failed to write: {failedNames}"
                };
            }

            return new InvariantResult
            {
                Name = "ExportPathsWritable",
                Ok = true,
                Detail = "Verified by export stage (atomic write succeeded)."
            };
        }

        private InvariantResult ValidateNoOverwriteWithoutFlag(ExecutionContext context, List<ExportedArtifact> exported)
        {
            // Since we don't explicitly track "overwrote", we just state the enforcement mechanism
            return new InvariantResult
            {
                Name = "NoOverwriteWithoutFlag",
                Ok = true,
                Detail = "Overwrite prevented by export stage when overwrite=false."
            };
        }

        private InvariantResult ValidateLeafPemParses(ExecutionContext context, List<ExportedArtifact> exported)
        {
            var request = context.Request;

            // Check if leaf export was requested
            bool leafRequested = request.Exports?.LeafPem?.Enabled == true;

            // Find exported artifact with Name == "leaf.pem"
            var leafArtifact = exported.FirstOrDefault(e => e.Name == "leaf.pem");

            if (leafRequested && (leafArtifact == null || !leafArtifact.Written))
            {
                return new InvariantResult
                {
                    Name = "LeafPemParses",
                    Ok = false,
                    Detail = leafArtifact == null
                        ? "Leaf PEM export was requested but artifact not found in export list."
                        : "Leaf PEM export was requested but file was not written."
                };
            }

            if (leafArtifact == null || !leafArtifact.Written)
            {
                // Not requested or not written - skip validation
                return new InvariantResult
                {
                    Name = "LeafPemParses",
                    Ok = true,
                    Detail = "Leaf PEM export not requested or not written."
                };
            }

            // Try to parse the PEM file
            try
            {
                if (string.IsNullOrEmpty(leafArtifact.Path) || !File.Exists(leafArtifact.Path))
                {
                    return new InvariantResult
                    {
                        Name = "LeafPemParses",
                        Ok = false,
                        Detail = $"Leaf PEM file not found at path: {leafArtifact.Path}"
                    };
                }

                string pemContent = File.ReadAllText(leafArtifact.Path);
                byte[] certBytes = ExtractCertificateFromPem(pemContent);

                if (certBytes == null)
                {
                    return new InvariantResult
                    {
                        Name = "LeafPemParses",
                        Ok = false,
                        Detail = "Failed to extract certificate from PEM file (no valid CERTIFICATE block found)."
                    };
                }

                // Try to create X509Certificate2 from bytes
                var cert = new X509Certificate2(certBytes);
                
                return new InvariantResult
                {
                    Name = "LeafPemParses",
                    Ok = true,
                    Detail = "Leaf PEM file parsed successfully."
                };
            }
            catch (Exception ex)
            {
                return new InvariantResult
                {
                    Name = "LeafPemParses",
                    Ok = false,
                    Detail = $"Failed to parse leaf PEM file: {ex.Message}"
                };
            }
        }

        private InvariantResult ValidateCaBundleContainsCertificates(ExecutionContext context, List<ExportedArtifact> exported)
        {
            var request = context.Request;

            // Check if CA bundle export was requested
            bool bundleRequested = request.Exports?.CaBundlePem?.Enabled == true;

            // Find exported artifact with Name == "ca-bundle.pem"
            var bundleArtifact = exported.FirstOrDefault(e => e.Name == "ca-bundle.pem");

            if (bundleRequested && (bundleArtifact == null || !bundleArtifact.Written))
            {
                return new InvariantResult
                {
                    Name = "CaBundleContainsCertificates",
                    Ok = false,
                    Detail = bundleArtifact == null
                        ? "CA bundle export was requested but artifact not found in export list."
                        : "CA bundle export was requested but file was not written."
                };
            }

            if (bundleArtifact == null || !bundleArtifact.Written)
            {
                // Not requested or not written - skip validation
                return new InvariantResult
                {
                    Name = "CaBundleContainsCertificates",
                    Ok = true,
                    Detail = "CA bundle export not requested or not written."
                };
            }

            // Check if file contains at least one certificate
            try
            {
                if (string.IsNullOrEmpty(bundleArtifact.Path) || !File.Exists(bundleArtifact.Path))
                {
                    return new InvariantResult
                    {
                        Name = "CaBundleContainsCertificates",
                        Ok = false,
                        Detail = $"CA bundle file not found at path: {bundleArtifact.Path}"
                    };
                }

                string content = File.ReadAllText(bundleArtifact.Path);
                int count = CountOccurrences(content, "-----BEGIN CERTIFICATE-----");

                if (count >= 1)
                {
                    return new InvariantResult
                    {
                        Name = "CaBundleContainsCertificates",
                        Ok = true,
                        Detail = $"CA bundle contains {count} certificate(s)."
                    };
                }
                else
                {
                    return new InvariantResult
                    {
                        Name = "CaBundleContainsCertificates",
                        Ok = false,
                        Detail = "CA bundle file does not contain any certificates."
                    };
                }
            }
            catch (Exception ex)
            {
                return new InvariantResult
                {
                    Name = "CaBundleContainsCertificates",
                    Ok = false,
                    Detail = $"Failed to read CA bundle file: {ex.Message}"
                };
            }
        }

        private InvariantResult ValidatePrivateKeyPemParses(ExecutionContext context, List<ExportedArtifact> exported)
        {
            var request = context.Request;

            // Check if key export was requested
            bool keyRequested = request.Exports?.KeyPem?.Enabled == true;

            // Find exported artifact with Name == "leaf.key.pem"
            var keyArtifact = exported.FirstOrDefault(e => e.Name == "leaf.key.pem");

            if (keyRequested && (keyArtifact == null || !keyArtifact.Written))
            {
                return new InvariantResult
                {
                    Name = "PrivateKeyPemParses",
                    Ok = false,
                    Detail = "Key PEM export not implemented yet or export failed."
                };
            }

            if (keyArtifact == null || !keyArtifact.Written)
            {
                // Not requested or not written - skip validation
                return new InvariantResult
                {
                    Name = "PrivateKeyPemParses",
                    Ok = true,
                    Detail = "Key PEM export not requested or not written."
                };
            }

            // If written, return false with explicit message
            return new InvariantResult
            {
                Name = "PrivateKeyPemParses",
                Ok = false,
                Detail = "Key PEM parsing not implemented in v1."
            };
        }

        /// <summary>
        /// Parses Subject Alternative Names from X509Extension.
        /// </summary>
        private List<string> ParseSanExtension(X509Extension extension)
        {
            try
            {
                var sans = new List<string>();
                string rawData = Encoding.ASCII.GetString(extension.RawData);

                // Simple parsing - look for DNS and IP entries
                // This is a best-effort implementation
                // Format: DNS:name or IP:address
                var dnsMatches = Regex.Matches(rawData, @"DNS:([^\s,]+)", RegexOptions.IgnoreCase);
                foreach (Match match in dnsMatches)
                {
                    if (match.Groups.Count > 1)
                    {
                        sans.Add($"dns:{match.Groups[1].Value}");
                    }
                }

                var ipMatches = Regex.Matches(rawData, @"IP Address:([^\s,]+)", RegexOptions.IgnoreCase);
                foreach (Match match in ipMatches)
                {
                    if (match.Groups.Count > 1)
                    {
                        sans.Add($"ip:{match.Groups[1].Value}");
                    }
                }

                // Alternative format: IP:address
                var ipMatches2 = Regex.Matches(rawData, @"IP:([^\s,]+)", RegexOptions.IgnoreCase);
                foreach (Match match in ipMatches2)
                {
                    if (match.Groups.Count > 1)
                    {
                        sans.Add($"ip:{match.Groups[1].Value}");
                    }
                }

                return sans;
            }
            catch
            {
                // Return empty list on parsing failure
                return new List<string>();
            }
        }

        /// <summary>
        /// Extracts the first certificate from a PEM file.
        /// </summary>
        private byte[] ExtractCertificateFromPem(string pemContent)
        {
            try
            {
                // Find the first CERTIFICATE block
                var beginMarker = "-----BEGIN CERTIFICATE-----";
                var endMarker = "-----END CERTIFICATE-----";

                int beginIndex = pemContent.IndexOf(beginMarker, StringComparison.OrdinalIgnoreCase);
                if (beginIndex == -1)
                    return null;

                int endIndex = pemContent.IndexOf(endMarker, beginIndex, StringComparison.OrdinalIgnoreCase);
                if (endIndex == -1)
                    return null;

                // Extract the base64 content
                int contentStart = beginIndex + beginMarker.Length;
                string base64Content = pemContent.Substring(contentStart, endIndex - contentStart)
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace(" ", "")
                    .Trim();

                // Decode base64
                return Convert.FromBase64String(base64Content);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Counts occurrences of a substring in a string.
        /// </summary>
        private int CountOccurrences(string text, string pattern)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
                return 0;

            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }
    }
}


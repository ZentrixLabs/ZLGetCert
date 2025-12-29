using System;
using System.Collections.Generic;

namespace ZentrixLabs.ZLGetCert.Core.Contracts
{
    /// <summary>
    /// Represents a certificate request with all required parameters.
    /// </summary>
    public sealed class CertificateRequest
    {
        /// <summary>
        /// Caller-supplied correlation identifier.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Output format: "text" or "json".
        /// </summary>
        public string OutputFormat { get; set; }

        /// <summary>
        /// Subject identity information.
        /// </summary>
        public SubjectIdentity Subject { get; set; }

        /// <summary>
        /// Request mode: NewKeypair or SignExistingCsr.
        /// </summary>
        public RequestMode Mode { get; set; }

        /// <summary>
        /// Path to existing CSR file. Required when Mode == SignExistingCsr.
        /// </summary>
        public string CsrPath { get; set; }

        /// <summary>
        /// Certificate Authority target configuration.
        /// </summary>
        public CaTarget Ca { get; set; }

        /// <summary>
        /// Cryptographic profile settings.
        /// </summary>
        public CryptoProfile Crypto { get; set; }

        /// <summary>
        /// Authentication mode. Currently supports "currentUser".
        /// </summary>
        public string AuthMode { get; set; }

        /// <summary>
        /// Export plan configuration.
        /// </summary>
        public ExportPlan Exports { get; set; }

        /// <summary>
        /// Whether to overwrite existing files. Default: false.
        /// </summary>
        public bool Overwrite { get; set; }
    }

    /// <summary>
    /// Subject identity information for the certificate.
    /// </summary>
    public sealed class SubjectIdentity
    {
        /// <summary>
        /// Common Name (CN). Required unless csrPath is provided.
        /// </summary>
        public string CommonName { get; set; }

        /// <summary>
        /// Full X.500 distinguished name override. If present, must be a complete DN string.
        /// </summary>
        public string SubjectDn { get; set; }

        /// <summary>
        /// List of Subject Alternative Names. Each entry should be explicitly typed (e.g., "dns:example.com", "ip:192.168.1.1").
        /// </summary>
        public List<string> SubjectAlternativeNames { get; set; }

        /// <summary>
        /// Indicates wildcard intent. Validity is enforced against CA template rules.
        /// </summary>
        public bool Wildcard { get; set; }
    }

    /// <summary>
    /// Request mode for certificate generation.
    /// </summary>
    public enum RequestMode
    {
        /// <summary>
        /// Generate a new keypair during the request.
        /// </summary>
        NewKeypair,

        /// <summary>
        /// Submit an existing CSR for signing.
        /// </summary>
        SignExistingCsr
    }

    /// <summary>
    /// Certificate Authority target configuration.
    /// </summary>
    public sealed class CaTarget
    {
        /// <summary>
        /// CA configuration details.
        /// </summary>
        public CaConfig CaConfig { get; set; }

        /// <summary>
        /// Certificate template name as defined on the CA.
        /// </summary>
        public string Template { get; set; }
    }

    /// <summary>
    /// Certificate Authority configuration.
    /// </summary>
    public sealed class CaConfig
    {
        /// <summary>
        /// CA server hostname or FQDN.
        /// </summary>
        public string CaServer { get; set; }

        /// <summary>
        /// CA display name.
        /// </summary>
        public string CaName { get; set; }

        /// <summary>
        /// Explicit config string in "server\CAName" form. Alternative to CaServer/CaName.
        /// </summary>
        public string ConfigString { get; set; }
    }

    /// <summary>
    /// Cryptographic profile settings.
    /// </summary>
    public sealed class CryptoProfile
    {
        /// <summary>
        /// Key algorithm (e.g., "RSA").
        /// </summary>
        public string KeyAlgorithm { get; set; }

        /// <summary>
        /// Key size in bits (e.g., 2048, 4096).
        /// </summary>
        public int KeySize { get; set; }

        /// <summary>
        /// Hash algorithm (e.g., "sha256", "sha384", "sha512").
        /// </summary>
        public string HashAlgorithm { get; set; }

        /// <summary>
        /// Whether the private key is exportable. Must be explicitly set to true to allow export. Default: false.
        /// </summary>
        public bool ExportablePrivateKey { get; set; }
    }

    /// <summary>
    /// Export plan configuration for certificate artifacts.
    /// </summary>
    public sealed class ExportPlan
    {
        /// <summary>
        /// Leaf certificate PEM export target.
        /// </summary>
        public ExportTarget LeafPem { get; set; }

        /// <summary>
        /// Private key PEM export target.
        /// </summary>
        public ExportTarget KeyPem { get; set; }

        /// <summary>
        /// CA bundle PEM export target.
        /// </summary>
        public ExportTarget CaBundlePem { get; set; }
    }

    /// <summary>
    /// Export target configuration.
    /// </summary>
    public sealed class ExportTarget
    {
        /// <summary>
        /// Whether this export is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Explicit file path for the export destination.
        /// </summary>
        public string Path { get; set; }
    }
}


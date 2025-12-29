using System;
using System.Collections.Generic;

namespace ZentrixLabs.ZLGetCert.Core.Contracts
{
    /// <summary>
    /// Result of a certificate request operation.
    /// </summary>
    public sealed class CertificateResult
    {
        /// <summary>
        /// Status of the operation: "success" or "failed".
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Failure category. Present only if Status == "failed".
        /// </summary>
        public FailureCategory? FailureCategory { get; set; }

        /// <summary>
        /// Human-readable summary of the outcome.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Request ID echoed from the request if provided.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Timestamp of the operation in UTC.
        /// </summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        /// Certificate details. Null on failure.
        /// </summary>
        public CertificateDetails Certificate { get; set; }

        /// <summary>
        /// Artifact report. Always present, but may be empty on failure.
        /// </summary>
        public ArtifactReport Artifacts { get; set; }

        /// <summary>
        /// List of invariant proofs. May be empty on failure.
        /// </summary>
        public List<InvariantResult> Invariants { get; set; }
    }

    /// <summary>
    /// Details about the issued certificate.
    /// </summary>
    public sealed class CertificateDetails
    {
        /// <summary>
        /// Certificate thumbprint.
        /// </summary>
        public string Thumbprint { get; set; }

        /// <summary>
        /// Certificate fingerprints.
        /// </summary>
        public Fingerprints Fingerprints { get; set; }

        /// <summary>
        /// Certificate subject.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// List of Subject Alternative Names.
        /// </summary>
        public List<string> SubjectAlternativeNames { get; set; }

        /// <summary>
        /// Certificate issuer.
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// Certificate serial number.
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Certificate validity start date.
        /// </summary>
        public DateTime? NotBefore { get; set; }

        /// <summary>
        /// Certificate validity end date.
        /// </summary>
        public DateTime? NotAfter { get; set; }

        /// <summary>
        /// Key algorithm used.
        /// </summary>
        public string KeyAlgorithm { get; set; }

        /// <summary>
        /// Key size in bits.
        /// </summary>
        public int? KeySize { get; set; }
    }

    /// <summary>
    /// Certificate fingerprints.
    /// </summary>
    public sealed class Fingerprints
    {
        /// <summary>
        /// SHA-256 fingerprint.
        /// </summary>
        public string Sha256 { get; set; }
    }

    /// <summary>
    /// Report of all artifacts produced during the operation.
    /// </summary>
    public sealed class ArtifactReport
    {
        /// <summary>
        /// Native artifacts returned by the certificate operation.
        /// </summary>
        public NativeArtifacts Native { get; set; }

        /// <summary>
        /// List of exported artifacts.
        /// </summary>
        public List<ExportedArtifact> Exported { get; set; }
    }

    /// <summary>
    /// Native artifacts returned by the certificate operation.
    /// </summary>
    public sealed class NativeArtifacts
    {
        /// <summary>
        /// Path to the .cer file if produced.
        /// </summary>
        public string CerPath { get; set; }

        /// <summary>
        /// Path to the .pfx file if produced.
        /// </summary>
        public string PfxPath { get; set; }

        /// <summary>
        /// Path to the certificate chain file if produced.
        /// </summary>
        public string ChainPath { get; set; }
    }

    /// <summary>
    /// Information about an exported artifact.
    /// </summary>
    public sealed class ExportedArtifact
    {
        /// <summary>
        /// Name of the artifact (e.g., "leaf.pem", "leaf.key.pem", "ca-bundle.pem").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Path where the artifact was written.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Whether the artifact was successfully written.
        /// </summary>
        public bool Written { get; set; }

        /// <summary>
        /// Size of the artifact in bytes.
        /// </summary>
        public long? SizeBytes { get; set; }

        /// <summary>
        /// SHA-256 hash of the artifact.
        /// </summary>
        public string Sha256 { get; set; }

        /// <summary>
        /// Number of certificates in the bundle. Applicable for CA bundle only.
        /// </summary>
        public int? CertificateCount { get; set; }
    }

    /// <summary>
    /// Result of an invariant check.
    /// </summary>
    public sealed class InvariantResult
    {
        /// <summary>
        /// Name of the invariant (e.g., "ExportPathsWritable", "NoOverwriteWithoutFlag", "LeafPemParses").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether the invariant check passed.
        /// </summary>
        public bool Ok { get; set; }

        /// <summary>
        /// Detailed information about the invariant check result.
        /// </summary>
        public string Detail { get; set; }
    }
}


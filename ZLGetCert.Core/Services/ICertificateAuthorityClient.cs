using ZentrixLabs.ZLGetCert.Core.Pipeline;

namespace ZentrixLabs.ZLGetCert.Core.Services
{
    /// <summary>
    /// Interface for certificate authority client operations.
    /// </summary>
    public interface ICertificateAuthorityClient
    {
        /// <summary>
        /// Executes the CA request (issue or submit CSR).
        /// Returns a native artifact bundle (opaque to contract for now).
        /// </summary>
        CaIssueResult Issue(ExecutionContext context);
    }

    /// <summary>
    /// Result of a certificate authority issue operation.
    /// </summary>
    public sealed class CaIssueResult
    {
        /// <summary>
        /// Whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable message describing the result.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Optional native artifacts (paths or raw text); keep flexible.
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

        /// <summary>
        /// If failure, allow a category hint (CARequestError, AuthorizationError, ConnectivityError).
        /// </summary>
        public ZentrixLabs.ZLGetCert.Core.Contracts.FailureCategory? FailureCategory { get; set; }
    }
}

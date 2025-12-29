using System;
using ZentrixLabs.ZLGetCert.Core.Services;

namespace ZentrixLabs.ZLGetCert.Core.Pipeline
{
    /// <summary>
    /// Executor for certificate requests. This is the canonical path for CLI and WPF.
    /// </summary>
    public sealed class CertificateRequestExecutor
    {
        private readonly ICertificateAuthorityClient _ca;
        private readonly IExportService _export;
        private readonly ICertificateParser _parser;

        /// <summary>
        /// Initializes a new instance of the CertificateRequestExecutor class.
        /// </summary>
        /// <param name="ca">The certificate authority client.</param>
        /// <param name="export">The export service.</param>
        /// <param name="parser">The certificate parser.</param>
        public CertificateRequestExecutor(
            ICertificateAuthorityClient ca,
            IExportService export,
            ICertificateParser parser)
        {
            _ca = ca ?? throw new ArgumentNullException(nameof(ca));
            _export = export ?? throw new ArgumentNullException(nameof(export));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        /// <summary>
        /// This method is the future canonical path for CLI and WPF.
        /// For now it performs validation + returns a structured failure stating "Not implemented".
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>The certificate result.</returns>
        public ZentrixLabs.ZLGetCert.Core.Contracts.CertificateResult Execute(ExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.Request == null)
            {
                return Fail(
                    ZentrixLabs.ZLGetCert.Core.Contracts.FailureCategory.ConfigurationError,
                    "Request was null",
                    context);
            }

            // NOTE: Doctor should be run by hosts before calling Execute.
            // Execute will still enforce critical invariants later.

            // Placeholder until CA client + export service are extracted from WPF:
            return Fail(
                ZentrixLabs.ZLGetCert.Core.Contracts.FailureCategory.EnvironmentError,
                "Certificate request execution is not implemented yet (core pipeline scaffolded).",
                context);
        }

        private static ZentrixLabs.ZLGetCert.Core.Contracts.CertificateResult Fail(
            ZentrixLabs.ZLGetCert.Core.Contracts.FailureCategory category,
            string message,
            ExecutionContext context)
        {
            return new ZentrixLabs.ZLGetCert.Core.Contracts.CertificateResult
            {
                Status = "failed",
                FailureCategory = category,
                Message = message,
                RequestId = context?.Request?.RequestId,
                TimestampUtc = DateTime.UtcNow,
                Certificate = null,
                Artifacts = new ZentrixLabs.ZLGetCert.Core.Contracts.ArtifactReport
                {
                    Native = new ZentrixLabs.ZLGetCert.Core.Contracts.NativeArtifacts(),
                    Exported = new System.Collections.Generic.List<ZentrixLabs.ZLGetCert.Core.Contracts.ExportedArtifact>()
                },
                Invariants = new System.Collections.Generic.List<ZentrixLabs.ZLGetCert.Core.Contracts.InvariantResult>()
            };
        }
    }
}

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
        /// Executes the full certificate request flow: CA issue -> export -> parse -> validate invariants.
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

            // Step 1: Issue certificate from CA
            var issued = _ca.Issue(context);
            if (!issued.Success)
            {
                return new ZentrixLabs.ZLGetCert.Core.Contracts.CertificateResult
                {
                    Status = "failed",
                    FailureCategory = issued.FailureCategory ?? ZentrixLabs.ZLGetCert.Core.Contracts.FailureCategory.CARequestError,
                    Message = issued.Message,
                    RequestId = context.Request.RequestId,
                    TimestampUtc = DateTime.UtcNow,
                    Certificate = null,
                    Artifacts = new ZentrixLabs.ZLGetCert.Core.Contracts.ArtifactReport
                    {
                        Native = new ZentrixLabs.ZLGetCert.Core.Contracts.NativeArtifacts
                        {
                            CerPath = issued.CerPath,
                            PfxPath = issued.PfxPath,
                            ChainPath = issued.ChainPath
                        },
                        Exported = new System.Collections.Generic.List<ZentrixLabs.ZLGetCert.Core.Contracts.ExportedArtifact>()
                    },
                    Invariants = new System.Collections.Generic.List<ZentrixLabs.ZLGetCert.Core.Contracts.InvariantResult>()
                };
            }

            // Step 2: Export artifacts
            var exportResult = _export.Export(context, issued);
            if (!exportResult.Success)
            {
                // Best effort: validate invariants even on export failure
                var invariants = _parser.ValidateInvariants(context, issued, exportResult.Exported ?? new System.Collections.Generic.List<ZentrixLabs.ZLGetCert.Core.Contracts.ExportedArtifact>());

                return new ZentrixLabs.ZLGetCert.Core.Contracts.CertificateResult
                {
                    Status = "failed",
                    FailureCategory = exportResult.FailureCategory ?? ZentrixLabs.ZLGetCert.Core.Contracts.FailureCategory.ExportError,
                    Message = exportResult.Message,
                    RequestId = context.Request.RequestId,
                    TimestampUtc = DateTime.UtcNow,
                    Certificate = null,
                    Artifacts = new ZentrixLabs.ZLGetCert.Core.Contracts.ArtifactReport
                    {
                        Native = new ZentrixLabs.ZLGetCert.Core.Contracts.NativeArtifacts
                        {
                            CerPath = issued.CerPath,
                            PfxPath = issued.PfxPath,
                            ChainPath = issued.ChainPath
                        },
                        Exported = exportResult.Exported ?? new System.Collections.Generic.List<ZentrixLabs.ZLGetCert.Core.Contracts.ExportedArtifact>()
                    },
                    Invariants = invariants
                };
            }

            // Step 3: Parse certificate details
            var certificateDetails = _parser.Parse(context, issued);
            if (certificateDetails == null)
            {
                // Parsing failed - return FormatError
                return new ZentrixLabs.ZLGetCert.Core.Contracts.CertificateResult
                {
                    Status = "failed",
                    FailureCategory = ZentrixLabs.ZLGetCert.Core.Contracts.FailureCategory.FormatError,
                    Message = "Failed to parse issued certificate",
                    RequestId = context.Request.RequestId,
                    TimestampUtc = DateTime.UtcNow,
                    Certificate = null,
                    Artifacts = new ZentrixLabs.ZLGetCert.Core.Contracts.ArtifactReport
                    {
                        Native = new ZentrixLabs.ZLGetCert.Core.Contracts.NativeArtifacts
                        {
                            CerPath = issued.CerPath,
                            PfxPath = issued.PfxPath,
                            ChainPath = issued.ChainPath
                        },
                        Exported = exportResult.Exported ?? new System.Collections.Generic.List<ZentrixLabs.ZLGetCert.Core.Contracts.ExportedArtifact>()
                    },
                    Invariants = new System.Collections.Generic.List<ZentrixLabs.ZLGetCert.Core.Contracts.InvariantResult>()
                };
            }

            // Step 4: Validate invariants
            var invariantsList = _parser.ValidateInvariants(context, issued, exportResult.Exported ?? new System.Collections.Generic.List<ZentrixLabs.ZLGetCert.Core.Contracts.ExportedArtifact>());

            // Step 5: Return success result
            return new ZentrixLabs.ZLGetCert.Core.Contracts.CertificateResult
            {
                Status = "success",
                Message = "Certificate issued successfully",
                RequestId = context.Request.RequestId,
                TimestampUtc = DateTime.UtcNow,
                Certificate = certificateDetails,
                Artifacts = new ZentrixLabs.ZLGetCert.Core.Contracts.ArtifactReport
                {
                    Native = new ZentrixLabs.ZLGetCert.Core.Contracts.NativeArtifacts
                    {
                        CerPath = issued.CerPath,
                        PfxPath = issued.PfxPath,
                        ChainPath = issued.ChainPath
                    },
                    Exported = exportResult.Exported ?? new System.Collections.Generic.List<ZentrixLabs.ZLGetCert.Core.Contracts.ExportedArtifact>()
                },
                Invariants = invariantsList
            };
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

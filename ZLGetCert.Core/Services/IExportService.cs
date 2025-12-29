using System.Collections.Generic;
using ZentrixLabs.ZLGetCert.Core.Pipeline;

namespace ZentrixLabs.ZLGetCert.Core.Services
{
    /// <summary>
    /// Interface for certificate export operations.
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Perform exports (PEM etc) based on request + issued artifacts.
        /// Must enforce overwrite rules and produce ExportedArtifact records.
        /// </summary>
        ExportResult Export(ExecutionContext context, CaIssueResult issued);
    }

    /// <summary>
    /// Result of an export operation.
    /// </summary>
    public sealed class ExportResult
    {
        /// <summary>
        /// Whether the export operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable message describing the result.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// List of exported artifacts.
        /// </summary>
        public List<ZentrixLabs.ZLGetCert.Core.Contracts.ExportedArtifact> Exported { get; set; }

        /// <summary>
        /// Failure category if the operation failed.
        /// </summary>
        public ZentrixLabs.ZLGetCert.Core.Contracts.FailureCategory? FailureCategory { get; set; }
    }
}

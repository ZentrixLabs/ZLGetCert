using System.Collections.Generic;
using ZentrixLabs.ZLGetCert.Core.Pipeline;

namespace ZentrixLabs.ZLGetCert.Core.Services
{
    /// <summary>
    /// Interface for certificate parsing and validation operations.
    /// </summary>
    public interface ICertificateParser
    {
        /// <summary>
        /// Reads and parses certificate details for reporting/invariants.
        /// </summary>
        ZentrixLabs.ZLGetCert.Core.Contracts.CertificateDetails Parse(ExecutionContext context, CaIssueResult issued);

        /// <summary>
        /// Validates PEM exports (leaf, key, bundle) and returns invariant results.
        /// </summary>
        List<ZentrixLabs.ZLGetCert.Core.Contracts.InvariantResult> ValidateInvariants(
            ExecutionContext context,
            CaIssueResult issued,
            List<ZentrixLabs.ZLGetCert.Core.Contracts.ExportedArtifact> exported);
    }
}

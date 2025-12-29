namespace ZentrixLabs.ZLGetCert.Core.Contracts
{
    /// <summary>
    /// Categories of failures that can occur during certificate operations.
    /// </summary>
    public enum FailureCategory
    {
        /// <summary>
        /// Missing inputs, invalid combinations, malformed SANs.
        /// </summary>
        ConfigurationError,

        /// <summary>
        /// Missing OS capability, missing tooling, insufficient privileges.
        /// </summary>
        EnvironmentError,

        /// <summary>
        /// CA host unreachable, DNS failure, transport failure.
        /// </summary>
        ConnectivityError,

        /// <summary>
        /// Access denied, enrollment permission failure.
        /// </summary>
        AuthorizationError,

        /// <summary>
        /// Request rejected, template mismatch, policy denial.
        /// </summary>
        CARequestError,

        /// <summary>
        /// Invalid path, permission failure, overwrite violation, write failure.
        /// </summary>
        ExportError,

        /// <summary>
        /// Returned certificate or key cannot be parsed or converted.
        /// </summary>
        FormatError
    }
}


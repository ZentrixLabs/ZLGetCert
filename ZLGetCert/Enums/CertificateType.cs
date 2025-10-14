namespace ZLGetCert.Enums
{
    /// <summary>
    /// Types of certificates supported by the application
    /// </summary>
    public enum CertificateType
    {
        /// <summary>
        /// Standard hostname certificate with SANs (Web Server)
        /// </summary>
        Standard,

        /// <summary>
        /// Wildcard domain certificate (*.domain.com)
        /// </summary>
        Wildcard,

        /// <summary>
        /// Client authentication certificate
        /// </summary>
        ClientAuth,

        /// <summary>
        /// Code signing certificate
        /// </summary>
        CodeSigning,

        /// <summary>
        /// Email protection certificate (S/MIME)
        /// </summary>
        Email,

        /// <summary>
        /// Custom certificate with user-defined OIDs
        /// </summary>
        Custom,

        /// <summary>
        /// Certificate generated from existing CSR file
        /// </summary>
        FromCSR
    }
}

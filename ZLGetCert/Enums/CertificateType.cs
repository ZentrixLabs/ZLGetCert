namespace ZLGetCert.Enums
{
    /// <summary>
    /// Types of certificates supported by the application
    /// </summary>
    public enum CertificateType
    {
        /// <summary>
        /// Standard hostname certificate with SANs
        /// </summary>
        Standard,

        /// <summary>
        /// Wildcard domain certificate (*.domain.com)
        /// </summary>
        Wildcard,

        /// <summary>
        /// Certificate generated from existing CSR file
        /// </summary>
        FromCSR
    }
}

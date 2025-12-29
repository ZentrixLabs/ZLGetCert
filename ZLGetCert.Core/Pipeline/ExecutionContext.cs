namespace ZentrixLabs.ZLGetCert.Core.Pipeline
{
    /// <summary>
    /// Execution context for certificate request processing.
    /// </summary>
    public sealed class ExecutionContext
    {
        /// <summary>
        /// The certificate request to process.
        /// </summary>
        public ZentrixLabs.ZLGetCert.Core.Contracts.CertificateRequest Request { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ExecutionContext class.
        /// </summary>
        /// <param name="request">The certificate request to process.</param>
        public ExecutionContext(ZentrixLabs.ZLGetCert.Core.Contracts.CertificateRequest request)
        {
            Request = request;
        }
    }
}

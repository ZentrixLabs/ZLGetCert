namespace ZentrixLabs.ZLGetCert.Core.Doctor
{
    public sealed class DoctorContext
    {
        // Inputs
        public ZentrixLabs.ZLGetCert.Core.Contracts.CertificateRequest Request { get; private set; }

        // Derived values (no IO here; just placeholders for later population)
        public string RequestPath { get; private set; }     // optional: where the request came from (for evidence)
        public string ConfigPath { get; private set; }      // optional: where config came from (for evidence)

        public DoctorContext(
            ZentrixLabs.ZLGetCert.Core.Contracts.CertificateRequest request,
            string requestPath = null,
            string configPath = null)
        {
            Request = request;
            RequestPath = requestPath;
            ConfigPath = configPath;
        }
    }
}


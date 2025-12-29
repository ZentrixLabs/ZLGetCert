namespace ZentrixLabs.ZLGetCert.Core.Doctor
{
    public interface IDoctorCheck
    {
        // Stable ID, e.g. "env.windows", "export.path.writable"
        string Id { get; }

        // Executes a single check and returns a DoctorCheckResult.
        // No throwing for expected failures; encode them as fail results with category + remediation.
        ZentrixLabs.ZLGetCert.Core.Contracts.DoctorCheckResult Run(DoctorContext context);
    }
}


using System.Collections.Generic;
using ZentrixLabs.ZLGetCert.Core.Doctor.Checks;

namespace ZentrixLabs.ZLGetCert.Core.Doctor
{
    public static class DefaultDoctorChecks
    {
        // Returns the authoritative, ordered list of doctor checks.
        // CLI, WPF, and any future host MUST use this.
        public static IReadOnlyList<IDoctorCheck> Create()
        {
            return new IDoctorCheck[]
            {
                // Configuration validity first
                new ConfigRequiredFieldsCheck(),
                new ModeLegalityCheck(),

                // Export safety before any external interaction
                new ExportDestinationReadinessCheck()

                // Environment + CA reachability checks will be added later
            };
        }
    }
}


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
                // Environment checks first
                new WindowsOsCheck(),
                new RuntimeInfoCheck(),
                new ElevationCheck(),
                new ToolingPresenceCheck(),

                // Configuration validity
                new ConfigRequiredFieldsCheck(),
                new ModeLegalityCheck(),

                // Export safety before any external interaction
                new ExportDestinationReadinessCheck()
            };
        }
    }
}


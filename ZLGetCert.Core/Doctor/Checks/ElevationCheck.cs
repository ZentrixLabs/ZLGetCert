using System;
using System.Collections.Generic;
using System.Security.Principal;
using ZentrixLabs.ZLGetCert.Core.Contracts;

namespace ZentrixLabs.ZLGetCert.Core.Doctor.Checks
{
    public sealed class ElevationCheck : IDoctorCheck
    {
        public string Id => "env.elevation";

        public DoctorCheckResult Run(DoctorContext context)
        {
            bool isElevated = false;

            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                // If we can't determine elevation, assume not elevated
                isElevated = false;
            }

            var evidence = new Dictionary<string, object>
            {
                { "isElevated", isElevated }
            };

            if (!isElevated)
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.EnvironmentError,
                    Summary = "Administrative privileges required",
                    Evidence = evidence,
                    Remediation = "Re-run the command in an elevated Administrator shell."
                };
            }

            return new DoctorCheckResult
            {
                Id = Id,
                Status = "pass",
                Summary = "Process running with administrative privileges",
                Evidence = evidence
            };
        }
    }
}


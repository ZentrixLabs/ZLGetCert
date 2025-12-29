using System;
using System.Collections.Generic;
using ZentrixLabs.ZLGetCert.Core.Contracts;

namespace ZentrixLabs.ZLGetCert.Core.Doctor.Checks
{
    public sealed class WindowsOsCheck : IDoctorCheck
    {
        public string Id => "env.windows";

        public DoctorCheckResult Run(DoctorContext context)
        {
            var platform = Environment.OSVersion.Platform;
            var osVersion = Environment.OSVersion.VersionString;

            var evidence = new Dictionary<string, object>
            {
                { "platform", platform.ToString() },
                { "osVersion", osVersion }
            };

            if (platform != PlatformID.Win32NT)
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.EnvironmentError,
                    Summary = "Unsupported operating system",
                    Evidence = evidence,
                    Remediation = "Run ZLGetCert on Windows."
                };
            }

            return new DoctorCheckResult
            {
                Id = Id,
                Status = "pass",
                Summary = "Windows operating system detected",
                Evidence = evidence
            };
        }
    }
}


using System;
using System.Collections.Generic;
using ZentrixLabs.ZLGetCert.Core.Contracts;

namespace ZentrixLabs.ZLGetCert.Core.Doctor.Checks
{
    public sealed class RuntimeInfoCheck : IDoctorCheck
    {
        public string Id => "env.runtime";

        public DoctorCheckResult Run(DoctorContext context)
        {
            var evidence = new Dictionary<string, object>
            {
                { "clrVersion", Environment.Version.ToString() },
                { "is64BitProcess", Environment.Is64BitProcess },
                { "is64BitOS", Environment.Is64BitOperatingSystem }
            };

            return new DoctorCheckResult
            {
                Id = Id,
                Status = "pass",
                Summary = "Runtime information collected",
                Evidence = evidence
            };
        }
    }
}


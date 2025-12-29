using System;
using System.Collections.Generic;
using System.IO;
using ZentrixLabs.ZLGetCert.Core.Contracts;

namespace ZentrixLabs.ZLGetCert.Core.Doctor.Checks
{
    public sealed class ToolingPresenceCheck : IDoctorCheck
    {
        public string Id => "env.tooling";

        public DoctorCheckResult Run(DoctorContext context)
        {
            var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            if (string.IsNullOrEmpty(systemRoot))
            {
                systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            }

            var system32Path = Path.Combine(systemRoot, "System32");

            var certreqPath = Path.Combine(system32Path, "certreq.exe");
            var certutilPath = Path.Combine(system32Path, "certutil.exe");

            var tools = new List<Dictionary<string, object>>();

            var certreqExists = File.Exists(certreqPath);
            tools.Add(new Dictionary<string, object>
            {
                { "name", "certreq.exe" },
                { "expectedPath", certreqPath },
                { "exists", certreqExists }
            });

            var certutilExists = File.Exists(certutilPath);
            tools.Add(new Dictionary<string, object>
            {
                { "name", "certutil.exe" },
                { "expectedPath", certutilPath },
                { "exists", certutilExists }
            });

            var evidence = new Dictionary<string, object>
            {
                { "tools", tools }
            };

            if (!certreqExists || !certutilExists)
            {
                var missingTools = new List<string>();
                if (!certreqExists) missingTools.Add("certreq.exe");
                if (!certutilExists) missingTools.Add("certutil.exe");

                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.EnvironmentError,
                    Summary = "Required Windows certificate tooling not found",
                    Detail = string.Join(", ", missingTools),
                    Evidence = evidence,
                    Remediation = "Ensure certreq.exe and certutil.exe are present (Windows Certificate Services tools)."
                };
            }

            return new DoctorCheckResult
            {
                Id = Id,
                Status = "pass",
                Summary = "Required Windows certificate tooling found",
                Evidence = evidence
            };
        }
    }
}


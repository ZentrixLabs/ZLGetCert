using System;
using System.Collections.Generic;
using System.Linq;

namespace ZentrixLabs.ZLGetCert.Core.Doctor
{
    public sealed class DoctorRunner
    {
        private readonly List<IDoctorCheck> _checks;

        public DoctorRunner(IEnumerable<IDoctorCheck> checks)
        {
            if (checks == null) throw new ArgumentNullException(nameof(checks));
            _checks = new List<IDoctorCheck>(checks);
        }

        public ZentrixLabs.ZLGetCert.Core.Contracts.DoctorResult Run(DoctorContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var results = new List<ZentrixLabs.ZLGetCert.Core.Contracts.DoctorCheckResult>();

            foreach (var check in _checks)
            {
                // Hard rule: checks should not throw for normal failures.
                // If a check throws, capture it as an EnvironmentError with evidence.
                try
                {
                    var r = check.Run(context);
                    if (r == null)
                    {
                        results.Add(new ZentrixLabs.ZLGetCert.Core.Contracts.DoctorCheckResult
                        {
                            Id = check.Id,
                            Status = "fail",
                            Category = ZentrixLabs.ZLGetCert.Core.Contracts.FailureCategory.EnvironmentError,
                            Summary = "Doctor check returned no result",
                            Detail = "A doctor check returned null, which violates the Doctor contract.",
                            Evidence = new Dictionary<string, object> { { "checkId", check.Id } },
                            Remediation = "Fix the implementation of the doctor check to always return a DoctorCheckResult."
                        });
                    }
                    else
                    {
                        // Ensure Id is always set and stable
                        if (string.IsNullOrWhiteSpace(r.Id))
                            r.Id = check.Id;

                        results.Add(r);
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new ZentrixLabs.ZLGetCert.Core.Contracts.DoctorCheckResult
                    {
                        Id = check.Id,
                        Status = "fail",
                        Category = ZentrixLabs.ZLGetCert.Core.Contracts.FailureCategory.EnvironmentError,
                        Summary = "Doctor check threw an exception",
                        Detail = ex.Message,
                        Evidence = new Dictionary<string, object>
                        {
                            { "checkId", check.Id },
                            { "exceptionType", ex.GetType().FullName }
                        },
                        Remediation = "Fix the doctor check to report failures via DoctorCheckResult rather than throwing."
                    });
                }
            }

            int passed = results.Count(r => string.Equals(r.Status, "pass", StringComparison.OrdinalIgnoreCase));
            int failed = results.Count(r => string.Equals(r.Status, "fail", StringComparison.OrdinalIgnoreCase));
            int warnings = results.Count(r => string.Equals(r.Status, "warn", StringComparison.OrdinalIgnoreCase));

            return new ZentrixLabs.ZLGetCert.Core.Contracts.DoctorResult
            {
                Status = failed > 0 ? "fail" : "pass",
                Passed = passed,
                Failed = failed,
                Warnings = warnings,
                Checks = results
            };
        }
    }
}


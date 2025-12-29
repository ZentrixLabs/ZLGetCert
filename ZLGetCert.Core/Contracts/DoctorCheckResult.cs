using System.Collections.Generic;

namespace ZentrixLabs.ZLGetCert.Core.Contracts
{
    public sealed class DoctorCheckResult
    {
        public string Id { get; set; }               // stable check identifier
        public string Status { get; set; }           // "pass", "fail", "warn"

        public FailureCategory? Category { get; set; }

        public string Summary { get; set; }
        public string Detail { get; set; }

        // Evidence should be structured but flexible
        // Use Dictionary<string, object> intentionally (serialization layer will decide later)
        public Dictionary<string, object> Evidence { get; set; }

        public string Remediation { get; set; }
    }
}


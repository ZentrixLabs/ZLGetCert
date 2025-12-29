using System.Collections.Generic;

namespace ZentrixLabs.ZLGetCert.Core.Contracts
{
    public sealed class DoctorResult
    {
        public string Status { get; set; }          // "pass" or "fail"
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Warnings { get; set; }

        public List<DoctorCheckResult> Checks { get; set; }
    }
}


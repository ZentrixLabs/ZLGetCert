using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ZentrixLabs.ZLGetCert.Core.Contracts;

namespace ZentrixLabs.ZLGetCert.Core.Doctor.Checks
{
    public sealed class CaDnsResolutionCheck : IDoctorCheck
    {
        public string Id => "ca.dns";

        public DoctorCheckResult Run(DoctorContext context)
        {
            var request = context.Request;
            if (request?.Ca?.CaConfig == null)
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.ConfigurationError,
                    Summary = "CA server not specified",
                    Remediation = "Set ca.caConfig.caServer (or configString) in the request."
                };
            }

            var caServer = request.Ca.CaConfig.CaServer;
            if (string.IsNullOrWhiteSpace(caServer))
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.ConfigurationError,
                    Summary = "CA server not specified",
                    Remediation = "Set ca.caConfig.caServer (or configString) in the request."
                };
            }

            try
            {
                var addresses = Dns.GetHostAddresses(caServer);
                if (addresses == null || addresses.Length == 0)
                {
                    return new DoctorCheckResult
                    {
                        Id = Id,
                        Status = "fail",
                        Category = FailureCategory.ConnectivityError,
                        Summary = "Unable to resolve CA host",
                        Evidence = new Dictionary<string, object>
                        {
                            { "caServer", caServer }
                        },
                        Remediation = "Fix DNS or use a resolvable CA hostname."
                    };
                }

                var addressStrings = addresses.Select(addr => addr.ToString()).ToList();
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "pass",
                    Summary = "CA host DNS resolution successful",
                    Evidence = new Dictionary<string, object>
                    {
                        { "caServer", caServer },
                        { "addresses", addressStrings }
                    }
                };
            }
            catch (Exception ex)
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.ConnectivityError,
                    Summary = "Unable to resolve CA host",
                    Evidence = new Dictionary<string, object>
                    {
                        { "caServer", caServer },
                        { "errorType", ex.GetType().Name }
                    },
                    Remediation = "Fix DNS or use a resolvable CA hostname."
                };
            }
        }
    }
}


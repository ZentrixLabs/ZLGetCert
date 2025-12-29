using System;
using System.Collections.Generic;
using ZentrixLabs.ZLGetCert.Core.Contracts;

namespace ZentrixLabs.ZLGetCert.Core.Doctor.Checks
{
    public sealed class ModeLegalityCheck : IDoctorCheck
    {
        public string Id => "config.mode.legal";

        public DoctorCheckResult Run(DoctorContext context)
        {
            var request = context.Request;
            var violatedRules = new List<string>();

            // If Mode == SignExistingCsr, CsrPath must be non-empty
            if (request.Mode == RequestMode.SignExistingCsr)
            {
                if (string.IsNullOrWhiteSpace(request.CsrPath))
                {
                    violatedRules.Add("CsrPath must be non-empty when Mode is SignExistingCsr");
                }
            }

            // If KeyPem export is enabled, ExportablePrivateKey must be true
            if (request.Exports != null && request.Exports.KeyPem != null && request.Exports.KeyPem.Enabled)
            {
                if (request.Crypto == null || !request.Crypto.ExportablePrivateKey)
                {
                    violatedRules.Add("ExportablePrivateKey must be true when KeyPem export is enabled");
                }
            }

            // If any rules are violated, return failure
            if (violatedRules.Count > 0)
            {
                var evidence = new Dictionary<string, object>
                {
                    { "violatedRules", violatedRules }
                };

                // Build remediation message based on violated rules
                string remediation;
                if (violatedRules.Count == 1 && violatedRules[0].Contains("ExportablePrivateKey"))
                {
                    remediation = "Set crypto.exportablePrivateKey to true, or disable exports.keyPem.enabled.";
                }
                else if (violatedRules.Count == 1 && violatedRules[0].Contains("CsrPath"))
                {
                    remediation = "Provide a valid CsrPath when using SignExistingCsr mode.";
                }
                else
                {
                    remediation = "Fix the configuration to satisfy all mode legality rules.";
                }

                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.ConfigurationError,
                    Summary = "Illegal request configuration",
                    Evidence = evidence,
                    Remediation = remediation
                };
            }

            // All rules satisfied - build pass result with evidence
            var passEvidence = new Dictionary<string, object>
            {
                { "mode", request.Mode.ToString() },
                { "overwrite", request.Overwrite }
            };

            if (request.Exports != null)
            {
                var exportInfo = new Dictionary<string, object>();
                if (request.Exports.KeyPem != null)
                {
                    exportInfo["keyPemEnabled"] = request.Exports.KeyPem.Enabled;
                }
                if (request.Exports.LeafPem != null)
                {
                    exportInfo["leafPemEnabled"] = request.Exports.LeafPem.Enabled;
                }
                if (request.Exports.CaBundlePem != null)
                {
                    exportInfo["caBundlePemEnabled"] = request.Exports.CaBundlePem.Enabled;
                }
                passEvidence["exports"] = exportInfo;
            }

            return new DoctorCheckResult
            {
                Id = Id,
                Status = "pass",
                Summary = "Request mode and options are legal",
                Evidence = passEvidence
            };
        }
    }
}


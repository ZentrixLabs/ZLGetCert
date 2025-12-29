using System;
using System.Collections.Generic;
using ZentrixLabs.ZLGetCert.Core.Contracts;

namespace ZentrixLabs.ZLGetCert.Core.Doctor.Checks
{
    public sealed class ConfigRequiredFieldsCheck : IDoctorCheck
    {
        public string Id => "config.required";

        public DoctorCheckResult Run(DoctorContext context)
        {
            var request = context.Request;
            var missingFields = new List<string>();

            // Check Request is not null (should be validated by DoctorRunner, but be defensive)
            if (request == null)
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.ConfigurationError,
                    Summary = "Missing required request fields",
                    Evidence = new Dictionary<string, object>
                    {
                        { "missingFields", new[] { "Request" } }
                    },
                    Remediation = "Provide the missing fields in the CertificateRequest JSON."
                };
            }

            // Check CA is not null
            if (request.Ca == null)
            {
                missingFields.Add("Ca");
            }
            else
            {
                // Check Template is not null/empty
                if (string.IsNullOrWhiteSpace(request.Ca.Template))
                {
                    missingFields.Add("Ca.Template");
                }

                // Check CaConfig is not null
                if (request.Ca.CaConfig == null)
                {
                    missingFields.Add("Ca.CaConfig");
                }
                else
                {
                    // Check at least one of ConfigString OR (CaServer and CaName) is present
                    bool hasConfigString = !string.IsNullOrWhiteSpace(request.Ca.CaConfig.ConfigString);
                    bool hasCaServer = !string.IsNullOrWhiteSpace(request.Ca.CaConfig.CaServer);
                    bool hasCaName = !string.IsNullOrWhiteSpace(request.Ca.CaConfig.CaName);

                    if (!hasConfigString && !(hasCaServer && hasCaName))
                    {
                        missingFields.Add("Ca.CaConfig.ConfigString or (Ca.CaConfig.CaServer and Ca.CaConfig.CaName)");
                    }
                }
            }

            // Check Subject is not null
            if (request.Subject == null)
            {
                missingFields.Add("Subject");
            }

            // Check Mode-specific requirements
            if (request.Mode == RequestMode.NewKeypair)
            {
                if (request.Subject != null && string.IsNullOrWhiteSpace(request.Subject.CommonName))
                {
                    missingFields.Add("Subject.CommonName (required for NewKeypair mode)");
                }
            }
            else if (request.Mode == RequestMode.SignExistingCsr)
            {
                if (string.IsNullOrWhiteSpace(request.CsrPath))
                {
                    missingFields.Add("CsrPath (required for SignExistingCsr mode)");
                }
            }

            // Check AuthMode is present (non-empty)
            if (string.IsNullOrWhiteSpace(request.AuthMode))
            {
                missingFields.Add("AuthMode");
            }

            // If any fields are missing, return failure
            if (missingFields.Count > 0)
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.ConfigurationError,
                    Summary = "Missing required request fields",
                    Evidence = new Dictionary<string, object>
                    {
                        { "missingFields", missingFields }
                    },
                    Remediation = "Provide the missing fields in the CertificateRequest JSON."
                };
            }

            // All required fields present - build pass result with evidence
            var evidence = new Dictionary<string, object>();
            evidence["template"] = request.Ca.Template;

            // Determine CA target form used
            bool usesConfigString = !string.IsNullOrWhiteSpace(request.Ca.CaConfig.ConfigString);
            if (usesConfigString)
            {
                evidence["caTargetForm"] = "configString";
                evidence["configString"] = request.Ca.CaConfig.ConfigString;
            }
            else
            {
                evidence["caTargetForm"] = "server+name";
                evidence["caServer"] = request.Ca.CaConfig.CaServer;
                evidence["caName"] = request.Ca.CaConfig.CaName;
            }

            return new DoctorCheckResult
            {
                Id = Id,
                Status = "pass",
                Summary = "Required request fields present",
                Evidence = evidence
            };
        }
    }
}


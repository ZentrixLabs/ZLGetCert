using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using ZLGetCert.Models;
using ZLGetCert.Enums;

namespace ZLGetCert.Utilities
{
    /// <summary>
    /// Helper utilities for input validation
    /// </summary>
    public static class ValidationHelper
    {
        private static readonly Regex DnsNameRegex = new Regex(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$", RegexOptions.Compiled);
        private static readonly Regex IpAddressRegex = new Regex(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", RegexOptions.Compiled);

        /// <summary>
        /// Validate certificate request
        /// </summary>
        public static ValidationResult ValidateCertificateRequest(CertificateRequest request)
        {
            var result = new ValidationResult();

            // Validate certificate type
            if (!Enum.IsDefined(typeof(CertificateType), request.Type))
            {
                result.AddError("Invalid certificate type");
            }

            // Validate based on certificate type
            switch (request.Type)
            {
                case CertificateType.Standard:
                    ValidateStandardCertificate(request, result);
                    break;
                case CertificateType.Wildcard:
                    ValidateWildcardCertificate(request, result);
                    break;
                case CertificateType.FromCSR:
                    ValidateFromCSRCertificate(request, result);
                    break;
            }

            // Validate common fields
            ValidateCommonFields(request, result);

            return result;
        }

        /// <summary>
        /// Validate standard certificate
        /// </summary>
        private static void ValidateStandardCertificate(CertificateRequest request, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(request.HostName))
            {
                result.AddError("Hostname is required for standard certificates");
            }
            else if (!IsValidDnsName(request.HostName))
            {
                result.AddError("Invalid hostname format");
            }

            if (string.IsNullOrWhiteSpace(request.FQDN))
            {
                result.AddError("FQDN is required for standard certificates");
            }
            else if (!IsValidDnsName(request.FQDN))
            {
                result.AddError("Invalid FQDN format");
            }
        }

        /// <summary>
        /// Validate wildcard certificate
        /// </summary>
        private static void ValidateWildcardCertificate(CertificateRequest request, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(request.FQDN))
            {
                result.AddError("FQDN is required for wildcard certificates");
            }
            else if (!request.FQDN.StartsWith("*."))
            {
                result.AddError("Wildcard certificates must start with '*.'");
            }
            else if (!IsValidDnsName(request.FQDN.Substring(2)))
            {
                result.AddError("Invalid wildcard domain format");
            }
        }

        /// <summary>
        /// Validate CSR-based certificate
        /// </summary>
        private static void ValidateFromCSRCertificate(CertificateRequest request, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(request.CsrFilePath))
            {
                result.AddError("CSR file path is required for CSR-based certificates");
            }
            else if (!System.IO.File.Exists(request.CsrFilePath))
            {
                result.AddError("CSR file does not exist");
            }
            else if (!request.CsrFilePath.EndsWith(".csr", StringComparison.OrdinalIgnoreCase) &&
                     !request.CsrFilePath.EndsWith(".req", StringComparison.OrdinalIgnoreCase))
            {
                result.AddError("CSR file must have .csr or .req extension");
            }
        }

        /// <summary>
        /// Validate common fields
        /// </summary>
        private static void ValidateCommonFields(CertificateRequest request, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(request.Location))
            {
                result.AddError("Location is required");
            }

            if (string.IsNullOrWhiteSpace(request.State))
            {
                result.AddError("State is required");
            }
            else if (request.State.Length != 2)
            {
                result.AddError("State must be a 2-letter abbreviation");
            }

            if (string.IsNullOrWhiteSpace(request.Company))
            {
                result.AddError("Company is required");
            }
            else if (!IsValidDnsName(request.Company))
            {
                result.AddError("Invalid company domain format");
            }

            if (string.IsNullOrWhiteSpace(request.OU))
            {
                result.AddError("Organizational Unit is required");
            }

            // Validate SANs
            ValidateSans(request.DnsSans, request.IpSans, result);
        }

        /// <summary>
        /// Validate SAN entries
        /// </summary>
        private static void ValidateSans(List<SanEntry> dnsSans, List<SanEntry> ipSans, ValidationResult result)
        {
            // Validate DNS SANs
            foreach (var dns in dnsSans)
            {
                if (string.IsNullOrWhiteSpace(dns.Value))
                {
                    result.AddError("DNS SAN entry cannot be empty");
                }
                else if (!IsValidDnsName(dns.Value))
                {
                    result.AddError($"Invalid DNS SAN format: {dns.Value}");
                }
            }

            // Validate IP SANs
            foreach (var ip in ipSans)
            {
                if (string.IsNullOrWhiteSpace(ip.Value))
                {
                    result.AddError("IP SAN entry cannot be empty");
                }
                else if (!IsValidIpAddress(ip.Value))
                {
                    result.AddError($"Invalid IP SAN format: {ip.Value}");
                }
            }

            // Check for duplicates
            var allDnsValues = dnsSans.Select(s => s.Value.ToLowerInvariant()).ToList();
            var duplicates = allDnsValues.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var duplicate in duplicates)
            {
                result.AddError($"Duplicate DNS SAN entry: {duplicate}");
            }

            var allIpValues = ipSans.Select(s => s.Value).ToList();
            var ipDuplicates = allIpValues.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var duplicate in ipDuplicates)
            {
                result.AddError($"Duplicate IP SAN entry: {duplicate}");
            }
        }

        /// <summary>
        /// Validate DNS name format
        /// </summary>
        public static bool IsValidDnsName(string dnsName)
        {
            if (string.IsNullOrWhiteSpace(dnsName))
                return false;

            if (dnsName.Length > 253)
                return false;

            if (dnsName.StartsWith(".") || dnsName.EndsWith("."))
                return false;

            return DnsNameRegex.IsMatch(dnsName);
        }

        /// <summary>
        /// Validate IP address format
        /// </summary>
        public static bool IsValidIpAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            return IPAddress.TryParse(ipAddress, out _) && IpAddressRegex.IsMatch(ipAddress);
        }

        /// <summary>
        /// Validate file path
        /// </summary>
        public static bool IsValidFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                var fullPath = System.IO.Path.GetFullPath(filePath);
                return System.IO.File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate password strength
        /// </summary>
        public static bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            return password.Length >= 8 && 
                   password.Any(char.IsUpper) && 
                   password.Any(char.IsLower) && 
                   password.Any(char.IsDigit);
        }
    }

    /// <summary>
    /// Validation result container
    /// </summary>
    public class ValidationResult
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();

        /// <summary>
        /// Whether validation passed
        /// </summary>
        public bool IsValid => _errors.Count == 0;

        /// <summary>
        /// Validation errors
        /// </summary>
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        /// <summary>
        /// Validation warnings
        /// </summary>
        public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

        /// <summary>
        /// Add validation error
        /// </summary>
        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error) && !_errors.Contains(error))
            {
                _errors.Add(error);
            }
        }

        /// <summary>
        /// Add validation warning
        /// </summary>
        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning) && !_warnings.Contains(warning))
            {
                _warnings.Add(warning);
            }
        }

        /// <summary>
        /// Get combined error and warning messages
        /// </summary>
        public string GetMessage()
        {
            var messages = new List<string>();
            messages.AddRange(_errors);
            messages.AddRange(_warnings);
            return string.Join(Environment.NewLine, messages);
        }
    }
}

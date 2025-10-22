using System;
using System.Collections.Generic;
using System.IO;
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
        /// Validate template/type match to prevent invalid certificates
        /// CRITICAL: Prevents template/type mismatches
        /// </summary>
        public static void ValidateTemplateTypeMatch(
            string templateName,
            CertificateType actualType,
            List<string> configuredOIDs,
            ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                return;

            // Skip validation for FromCSR - CSR defines the type
            if (actualType == CertificateType.FromCSR)
                return;

            // Detect expected type from template name
            var expectedType = Models.CertificateTemplate.DetectTypeFromTemplateName(templateName);

            // If expected type is Custom, we can't validate - allow anything
            if (expectedType == CertificateType.Custom)
                return;

            // Check if template and type match
            if (expectedType != actualType)
            {
                result.AddError(
                    $"Template/Type mismatch: Template '{templateName}' suggests type '{expectedType}', " +
                    $"but '{actualType}' was selected. This will create an invalid certificate. " +
                    $"The certificate type has been auto-configured to match the template.");
            }

            // Validate OIDs match the certificate type
            var expectedOIDs = Models.CertificateTemplate.GetOIDsForType(actualType);
            
            if (expectedOIDs.Any() && configuredOIDs != null)
            {
                foreach (var expectedOID in expectedOIDs)
                {
                    if (!configuredOIDs.Contains(expectedOID))
                    {
                        result.AddError(
                            $"Certificate type '{actualType}' requires OID {expectedOID}, " +
                            $"but it is not configured. This will create an invalid certificate.");
                    }
                }

                // Check for incorrect OIDs
                foreach (var configuredOID in configuredOIDs)
                {
                    if (!expectedOIDs.Contains(configuredOID) && !string.IsNullOrEmpty(configuredOID))
                    {
                        result.AddWarning(
                            $"Configured OID {configuredOID} may not be appropriate for certificate type '{actualType}'. " +
                            $"Expected OID(s): {string.Join(", ", expectedOIDs)}");
                    }
                }
            }
        }

        /// <summary>
        /// Validate common fields
        /// </summary>
        private static void ValidateCommonFields(CertificateRequest request, ValidationResult result)
        {
            // Skip common field validation for CSR-based certificates
            // CSR contains all the subject information (CN, O, OU, L, ST, C)
            if (request.Type == CertificateType.FromCSR)
            {
                // Only validate SANs for CSR-based certificates
                ValidateSans(request.DnsSans, request.IpSans, result);
                return;
            }

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

            // CRITICAL: Validate template/type match
            // This is commented out here because it requires config, will be done in CertificateService
            // ValidateTemplateTypeMatch(request.Template, request.Type, config.EnhancedKeyUsageOIDs, result);
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
        /// Validate password strength (basic check)
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

        /// <summary>
        /// Validate password meets security requirements and check against common passwords
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <param name="errors">List of validation errors</param>
        /// <returns>True if password is acceptable, false otherwise</returns>
        public static bool IsPasswordAcceptable(string password, out List<string> errors)
        {
            errors = new List<string>();
            
            if (string.IsNullOrEmpty(password))
            {
                errors.Add("Password is required");
                return false;
            }
            
            // Check minimum length
            if (password.Length < 8)
                errors.Add("Password must be at least 8 characters long");
            
            // Check for uppercase letters
            if (!password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter");
            
            // Check for lowercase letters
            if (!password.Any(char.IsLower))
                errors.Add("Password must contain at least one lowercase letter");
            
            // Check for digits
            if (!password.Any(char.IsDigit))
                errors.Add("Password must contain at least one number");
            
            // Recommend special characters for strong passwords
            if (!password.Any(c => char.IsPunctuation(c) || char.IsSymbol(c)))
            {
                // This is a warning, not an error
                if (password.Length < 12)
                    errors.Add("Password should contain special characters for better security");
            }
            
            // Check against common weak passwords
            string[] commonPasswords = { 
                "password", "Password1", "Password123", "password123",
                "123456", "12345678", "admin", "Admin123",
                "letmein", "welcome", "Welcome1", "monkey", 
                "qwerty", "abc123", "test", "Test123",
                "user", "User123", "temp", "Temp123",
                "changeme", "ChangeMe1", "default", "Default1"
            };
            
            if (commonPasswords.Contains(password, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add("Password is too common and easily guessed. Please choose a more unique password.");
            }
            
            return errors.Count == 0;
        }

        /// <summary>
        /// Get password strength level
        /// </summary>
        /// <param name="password">Password to evaluate</param>
        /// <returns>Strength description</returns>
        public static string GetPasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "Empty";

            int score = 0;
            
            // Length scoring
            if (password.Length >= 8) score++;
            if (password.Length >= 12) score++;
            if (password.Length >= 16) score++;
            
            // Complexity scoring
            if (password.Any(char.IsUpper)) score++;
            if (password.Any(char.IsLower)) score++;
            if (password.Any(char.IsDigit)) score++;
            if (password.Any(c => char.IsPunctuation(c) || char.IsSymbol(c))) score++;
            
            // Check for common patterns
            if (password.Contains("123") || password.Contains("abc") || password.Contains("qwerty"))
                score -= 2;
            
            if (score <= 2)
                return "Weak";
            else if (score <= 4)
                return "Medium";
            else if (score <= 6)
                return "Strong";
            else
                return "Very Strong";
        }
    }

    /// <summary>
    /// Process argument validation to prevent command injection attacks
    /// </summary>
    public static class ProcessArgumentValidator
    {
        /// <summary>
        /// Validate and sanitize file path for use in process arguments
        /// Prevents command injection and path traversal attacks
        /// </summary>
        /// <param name="path">File path to validate</param>
        /// <param name="parameterName">Parameter name for error messages</param>
        /// <returns>Validated absolute path</returns>
        public static string ValidateFilePath(string path, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException($"{parameterName} cannot be empty", parameterName);

            // Check for command injection characters
            char[] dangerousChars = { '&', '|', ';', '>', '<', '^', '`', '$', '(', ')', '{', '}', '\n', '\r' };
            if (path.IndexOfAny(dangerousChars) >= 0)
            {
                throw new ArgumentException(
                    $"{parameterName} contains invalid characters that could be used for command injection. " +
                    "File paths should only contain standard path characters.", 
                    parameterName);
            }

            // Normalize and validate path
            try
            {
                path = Path.GetFullPath(path);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid file path in {parameterName}: {ex.Message}", parameterName, ex);
            }

            // Additional security: Check for suspicious patterns
            if (path.Contains("..\\") || path.Contains("../"))
            {
                throw new ArgumentException(
                    $"{parameterName} contains path traversal sequences (../) which are not allowed",
                    parameterName);
            }

            return path;
        }

        /// <summary>
        /// Validate CA server name format to prevent injection
        /// </summary>
        /// <param name="serverName">Server name to validate</param>
        /// <param name="parameterName">Parameter name for error messages</param>
        /// <returns>Validated server name</returns>
        public static string ValidateCAServerName(string serverName, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(serverName))
                throw new ArgumentException($"{parameterName} cannot be empty", parameterName);

            // CA server should be a valid hostname/FQDN
            // Allow letters, numbers, dots, hyphens only (standard DNS naming)
            var regex = new Regex(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$");
            
            if (!regex.IsMatch(serverName))
            {
                throw new ArgumentException(
                    $"{parameterName} must be a valid hostname or FQDN. " +
                    "Only letters, numbers, dots, and hyphens are allowed.", 
                    parameterName);
            }

            if (serverName.Length > 253)
            {
                throw new ArgumentException(
                    $"{parameterName} exceeds maximum length of 253 characters for a DNS name", 
                    parameterName);
            }

            // Check for suspicious patterns
            if (serverName.Contains("..") || serverName.StartsWith("-") || serverName.EndsWith("-"))
            {
                throw new ArgumentException(
                    $"{parameterName} contains invalid patterns for a server name",
                    parameterName);
            }

            return serverName;
        }

        /// <summary>
        /// Validate certificate thumbprint format
        /// </summary>
        /// <param name="thumbprint">Thumbprint to validate</param>
        /// <param name="parameterName">Parameter name for error messages</param>
        /// <returns>Validated thumbprint in uppercase</returns>
        public static string ValidateThumbprint(string thumbprint, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
                throw new ArgumentException($"{parameterName} cannot be empty", parameterName);

            // Remove spaces and convert to uppercase
            thumbprint = thumbprint.Replace(" ", "").ToUpperInvariant();

            // Thumbprint should be exactly 40 hex characters (SHA-1 hash)
            var regex = new Regex(@"^[0-9A-F]{40}$");
            
            if (!regex.IsMatch(thumbprint))
            {
                throw new ArgumentException(
                    $"{parameterName} must be a 40-character hexadecimal string (SHA-1 hash). " +
                    "Current format is invalid.", 
                    parameterName);
            }

            return thumbprint;
        }

        /// <summary>
        /// Validate template name to prevent injection
        /// </summary>
        /// <param name="templateName">Template name to validate</param>
        /// <param name="parameterName">Parameter name for error messages</param>
        /// <returns>Validated template name</returns>
        public static string ValidateTemplateName(string templateName, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                throw new ArgumentException($"{parameterName} cannot be empty", parameterName);

            // Template names should only contain safe characters
            // Allow letters, numbers, spaces, underscores, hyphens
            var regex = new Regex(@"^[a-zA-Z0-9_\-\s]+$");
            
            if (!regex.IsMatch(templateName))
            {
                throw new ArgumentException(
                    $"{parameterName} contains invalid characters. " +
                    "Template names should only contain letters, numbers, spaces, underscores, and hyphens.", 
                    parameterName);
            }

            if (templateName.Length > 100)
            {
                throw new ArgumentException(
                    $"{parameterName} exceeds maximum length of 100 characters",
                    parameterName);
            }

            return templateName;
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

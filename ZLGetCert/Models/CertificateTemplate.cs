using System;
using System.Collections.Generic;
using System.ComponentModel;
using ZLGetCert.Enums;

namespace ZLGetCert.Models
{
    /// <summary>
    /// Represents a certificate template available on the CA
    /// </summary>
    public class CertificateTemplate : INotifyPropertyChanged
    {
        private string _name;
        private string _displayName;
        private string _oid;
        private int _version;
        private CertificateType? _detectedType;

        /// <summary>
        /// Template name (used in certificate requests)
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// Display name for UI
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary>
        /// Template OID (Object Identifier)
        /// </summary>
        public string OID
        {
            get => _oid;
            set
            {
                _oid = value;
                OnPropertyChanged(nameof(OID));
            }
        }

        /// <summary>
        /// Template version
        /// </summary>
        public int Version
        {
            get => _version;
            set
            {
                _version = value;
                OnPropertyChanged(nameof(Version));
            }
        }

        /// <summary>
        /// Formatted display string for ComboBox
        /// </summary>
        public string DisplayText
        {
            get
            {
                if (!string.IsNullOrEmpty(DisplayName) && DisplayName != Name)
                {
                    return $"{DisplayName} ({Name})";
                }
                return Name;
            }
        }

        /// <summary>
        /// Detected certificate type based on template name
        /// </summary>
        public CertificateType DetectedType
        {
            get
            {
                if (!_detectedType.HasValue)
                {
                    _detectedType = DetectTypeFromTemplateName(Name);
                }
                return _detectedType.Value;
            }
        }

        /// <summary>
        /// Get suggested Enhanced Key Usage OIDs based on detected type
        /// </summary>
        public List<string> GetSuggestedOIDs()
        {
            return GetOIDsForType(DetectedType);
        }

        /// <summary>
        /// Get suggested Key Usage based on detected type
        /// </summary>
        public string GetSuggestedKeyUsage()
        {
            return GetKeyUsageForType(DetectedType);
        }

        /// <summary>
        /// Auto-detect certificate type from template name
        /// </summary>
        public static CertificateType DetectTypeFromTemplateName(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
                return CertificateType.Custom;

            templateName = templateName.ToLowerInvariant();

            // Server/Web certificates
            if (templateName.Contains("web") || templateName.Contains("server") ||
                templateName.Contains("ssl") || templateName.Contains("tls") ||
                templateName.Equals("webserver", StringComparison.OrdinalIgnoreCase))
            {
                return CertificateType.Standard;
            }

            // Code signing
            if (templateName.Contains("codesign") || templateName.Contains("code") ||
                templateName.Equals("codesigning", StringComparison.OrdinalIgnoreCase))
            {
                return CertificateType.CodeSigning;
            }

            // Client authentication / User
            if (templateName.Contains("user") || templateName.Contains("client") ||
                templateName.Contains("workstation") || templateName.Contains("computer") ||
                templateName.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                return CertificateType.ClientAuth;
            }

            // Email / S/MIME
            if (templateName.Contains("email") || templateName.Contains("smime") ||
                templateName.Contains("mail") || 
                templateName.Equals("emailprotection", StringComparison.OrdinalIgnoreCase))
            {
                return CertificateType.Email;
            }

            // Default to custom if can't detect
            return CertificateType.Custom;
        }

        /// <summary>
        /// Get Enhanced Key Usage OIDs for a certificate type
        /// </summary>
        public static List<string> GetOIDsForType(CertificateType type)
        {
            switch (type)
            {
                case CertificateType.Standard:
                case CertificateType.Wildcard:
                    // Server Authentication
                    return new List<string> { "1.3.6.1.5.5.7.3.1" };

                case CertificateType.ClientAuth:
                    // Client Authentication
                    return new List<string> { "1.3.6.1.5.5.7.3.2" };

                case CertificateType.CodeSigning:
                    // Code Signing
                    return new List<string> { "1.3.6.1.5.5.7.3.3" };

                case CertificateType.Email:
                    // Email Protection
                    return new List<string> { "1.3.6.1.5.5.7.3.4" };

                case CertificateType.Custom:
                case CertificateType.FromCSR:
                default:
                    // Return empty list for custom/CSR - user will specify
                    return new List<string>();
            }
        }

        /// <summary>
        /// Get Key Usage value for a certificate type
        /// </summary>
        public static string GetKeyUsageForType(CertificateType type)
        {
            switch (type)
            {
                case CertificateType.Standard:
                case CertificateType.Wildcard:
                case CertificateType.Email:
                    // Digital Signature + Key Encipherment
                    return "0xa0";

                case CertificateType.ClientAuth:
                case CertificateType.CodeSigning:
                    // Digital Signature only
                    return "0x80";

                case CertificateType.Custom:
                case CertificateType.FromCSR:
                default:
                    return "0xa0"; // Default
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}


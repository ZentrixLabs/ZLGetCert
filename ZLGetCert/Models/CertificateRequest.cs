using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using ZLGetCert.Enums;

namespace ZLGetCert.Models
{
    /// <summary>
    /// Represents a certificate request with all necessary information
    /// </summary>
    public class CertificateRequest : INotifyPropertyChanged
    {
        private string _hostName;
        private string _fqdn;
        private string _location;
        private string _state;
        private string _company;
        private string _ou;
        private CertificateType _type;
        private string _csrFilePath;
        private bool _extractPemKey;
        private bool _confirmPassword;
        private List<SanEntry> _dnsSans;
        private List<SanEntry> _ipSans;

        public CertificateRequest()
        {
            _dnsSans = new List<SanEntry>();
            _ipSans = new List<SanEntry>();
            _company = "root.mpmaterials.com";
            _ou = "IT";
            _type = CertificateType.Standard;
            _extractPemKey = false;
            _confirmPassword = false;
        }

        /// <summary>
        /// Hostname for the certificate
        /// </summary>
        public string HostName
        {
            get => _hostName;
            set
            {
                _hostName = value;
                OnPropertyChanged(nameof(HostName));
                UpdateFQDN();
            }
        }

        /// <summary>
        /// Fully Qualified Domain Name
        /// </summary>
        public string FQDN
        {
            get => _fqdn;
            set
            {
                _fqdn = value;
                OnPropertyChanged(nameof(FQDN));
            }
        }

        /// <summary>
        /// Location (City) for the certificate
        /// </summary>
        public string Location
        {
            get => _location;
            set
            {
                _location = value;
                OnPropertyChanged(nameof(Location));
            }
        }

        /// <summary>
        /// State abbreviation for the certificate
        /// </summary>
        public string State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        /// <summary>
        /// Company name for the certificate
        /// </summary>
        public string Company
        {
            get => _company;
            set
            {
                _company = value;
                OnPropertyChanged(nameof(Company));
            }
        }

        /// <summary>
        /// Organizational Unit for the certificate
        /// </summary>
        public string OU
        {
            get => _ou;
            set
            {
                _ou = value;
                OnPropertyChanged(nameof(OU));
            }
        }

        /// <summary>
        /// Type of certificate request
        /// </summary>
        public CertificateType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
                OnPropertyChanged(nameof(IsWildcard));
                OnPropertyChanged(nameof(IsFromCSR));
            }
        }

        /// <summary>
        /// Path to existing CSR file (for FromCSR type)
        /// </summary>
        public string CsrFilePath
        {
            get => _csrFilePath;
            set
            {
                _csrFilePath = value;
                OnPropertyChanged(nameof(CsrFilePath));
            }
        }

        /// <summary>
        /// Whether to extract PEM/KEY files
        /// </summary>
        public bool ExtractPemKey
        {
            get => _extractPemKey;
            set
            {
                _extractPemKey = value;
                OnPropertyChanged(nameof(ExtractPemKey));
            }
        }

        /// <summary>
        /// Whether password confirmation is required
        /// </summary>
        public bool ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged(nameof(ConfirmPassword));
            }
        }

        /// <summary>
        /// List of DNS SAN entries
        /// </summary>
        public List<SanEntry> DnsSans
        {
            get => _dnsSans;
            set
            {
                _dnsSans = value;
                OnPropertyChanged(nameof(DnsSans));
            }
        }

        /// <summary>
        /// List of IP SAN entries
        /// </summary>
        public List<SanEntry> IpSans
        {
            get => _ipSans;
            set
            {
                _ipSans = value;
                OnPropertyChanged(nameof(IpSans));
            }
        }

        /// <summary>
        /// PFX password (handled as SecureString)
        /// </summary>
        public SecureString PfxPassword { get; set; }

        /// <summary>
        /// Whether this is a wildcard certificate request
        /// </summary>
        public bool IsWildcard => Type == CertificateType.Wildcard;

        /// <summary>
        /// Whether this is a CSR-based certificate request
        /// </summary>
        public bool IsFromCSR => Type == CertificateType.FromCSR;

        /// <summary>
        /// Certificate name for file naming (handles wildcard special case)
        /// </summary>
        public string CertificateName
        {
            get
            {
                if (Type == CertificateType.Wildcard)
                {
                    return FQDN?.Replace("*", "_") ?? "wildcard";
                }
                return FQDN ?? HostName ?? "certificate";
            }
        }

        /// <summary>
        /// Subject string for the certificate
        /// </summary>
        public string Subject
        {
            get
            {
                if (string.IsNullOrEmpty(FQDN) || string.IsNullOrEmpty(Location) || string.IsNullOrEmpty(State))
                    return string.Empty;

                return $"CN={FQDN}, OU={OU}, O={Company}, L={Location}, S={State}, C=US";
            }
        }

        private void UpdateFQDN()
        {
            if (Type == CertificateType.Wildcard)
            {
                FQDN = $"*.{Company}";
            }
            else if (!string.IsNullOrEmpty(HostName) && !string.IsNullOrEmpty(Company))
            {
                FQDN = $"{HostName}.{Company}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

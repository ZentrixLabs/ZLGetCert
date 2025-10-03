using System;
using System.Collections.ObjectModel;
using System.Security;
using System.Windows.Input;
using ZLGetCert.Models;
using ZLGetCert.Enums;
using ZLGetCert.Utilities;

namespace ZLGetCert.ViewModels
{
    /// <summary>
    /// ViewModel for certificate request form
    /// </summary>
    public class CertificateRequestViewModel : BaseViewModel
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
        private string _pfxPassword;
        private string _confirmPassword;
        private bool _showPassword;
        private bool _showConfirmPassword;

        public CertificateRequestViewModel()
        {
            // Initialize collections
            DnsSans = new ObservableCollection<SanEntry>();
            IpSans = new ObservableCollection<SanEntry>();

            // Initialize properties
            _type = CertificateType.Standard;
            _company = "root.mpmaterials.com";
            _ou = "IT";
            _extractPemKey = false;
            _showPassword = false;
            _showConfirmPassword = false;

            // Initialize commands
            BrowseCsrCommand = new RelayCommand(BrowseCsr);
            TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);
            ToggleConfirmPasswordVisibilityCommand = new RelayCommand(ToggleConfirmPasswordVisibility);

            // Add default SANs
            AddDnsSan();
            AddIpSan();
        }

        /// <summary>
        /// Hostname for the certificate
        /// </summary>
        public string HostName
        {
            get => _hostName;
            set
            {
                SetProperty(ref _hostName, value);
                UpdateFQDN();
                OnPropertyChanged(nameof(CanGenerate));
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
                SetProperty(ref _fqdn, value);
                OnPropertyChanged(nameof(CanGenerate));
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
                SetProperty(ref _location, value);
                OnPropertyChanged(nameof(CanGenerate));
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
                SetProperty(ref _state, value);
                OnPropertyChanged(nameof(CanGenerate));
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
                SetProperty(ref _company, value);
                UpdateFQDN();
                OnPropertyChanged(nameof(CanGenerate));
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
                SetProperty(ref _ou, value);
                OnPropertyChanged(nameof(CanGenerate));
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
                if (SetProperty(ref _type, value))
                {
                    // Clear all form fields when certificate type changes
                    ClearFormFields();
                    
                    OnPropertyChanged(nameof(IsWildcard));
                    OnPropertyChanged(nameof(IsFromCSR));
                    OnPropertyChanged(nameof(CanGenerate));
                }
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
                SetProperty(ref _csrFilePath, value);
                OnPropertyChanged(nameof(CanGenerate));
            }
        }

        /// <summary>
        /// Whether to extract PEM/KEY files
        /// </summary>
        public bool ExtractPemKey
        {
            get => _extractPemKey;
            set => SetProperty(ref _extractPemKey, value);
        }

        /// <summary>
        /// PFX password
        /// </summary>
        public string PfxPassword
        {
            get => _pfxPassword;
            set
            {
                SetProperty(ref _pfxPassword, value);
                OnPropertyChanged(nameof(PasswordStrength));
                OnPropertyChanged(nameof(CanGenerate));
            }
        }

        /// <summary>
        /// Confirm password
        /// </summary>
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                SetProperty(ref _confirmPassword, value);
                OnPropertyChanged(nameof(CanGenerate));
            }
        }

        /// <summary>
        /// Whether to show password
        /// </summary>
        public bool ShowPassword
        {
            get => _showPassword;
            set => SetProperty(ref _showPassword, value);
        }

        /// <summary>
        /// Whether to show confirm password
        /// </summary>
        public bool ShowConfirmPassword
        {
            get => _showConfirmPassword;
            set => SetProperty(ref _showConfirmPassword, value);
        }

        /// <summary>
        /// DNS SAN entries
        /// </summary>
        public ObservableCollection<SanEntry> DnsSans { get; }

        /// <summary>
        /// IP SAN entries
        /// </summary>
        public ObservableCollection<SanEntry> IpSans { get; }

        /// <summary>
        /// Whether this is a wildcard certificate request
        /// </summary>
        public bool IsWildcard => Type == CertificateType.Wildcard;

        /// <summary>
        /// Whether this is a CSR-based certificate request
        /// </summary>
        public bool IsFromCSR => Type == CertificateType.FromCSR;

        /// <summary>
        /// Certificate name for display
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
        /// Password strength indicator
        /// </summary>
        public string PasswordStrength
        {
            get
            {
                if (string.IsNullOrEmpty(PfxPassword))
                    return "No password set";

                var strength = SecureStringHelper.ValidatePasswordStrength(SecureStringHelper.StringToSecureString(PfxPassword));
                switch (strength)
                {
                    case ZLGetCert.Utilities.PasswordStrength.Weak:
                        return "Weak";
                    case ZLGetCert.Utilities.PasswordStrength.Medium:
                        return "Medium";
                    case ZLGetCert.Utilities.PasswordStrength.Strong:
                        return "Strong";
                    default:
                        return "Unknown";
                }
            }
        }

        /// <summary>
        /// Whether the form can generate a certificate
        /// </summary>
        public bool CanGenerate
        {
            get
            {
                if (Type == CertificateType.FromCSR)
                {
                    return !string.IsNullOrWhiteSpace(CsrFilePath) && 
                           !string.IsNullOrWhiteSpace(PfxPassword);
                }

                return !string.IsNullOrWhiteSpace(HostName) &&
                       !string.IsNullOrWhiteSpace(Location) &&
                       !string.IsNullOrWhiteSpace(State) &&
                       !string.IsNullOrWhiteSpace(PfxPassword) &&
                       (string.IsNullOrEmpty(ConfirmPassword) || PfxPassword == ConfirmPassword);
            }
        }

        /// <summary>
        /// Browse for CSR file command
        /// </summary>
        public ICommand BrowseCsrCommand { get; }

        /// <summary>
        /// Toggle password visibility command
        /// </summary>
        public ICommand TogglePasswordVisibilityCommand { get; }

        /// <summary>
        /// Toggle confirm password visibility command
        /// </summary>
        public ICommand ToggleConfirmPasswordVisibilityCommand { get; }

        /// <summary>
        /// Add DNS SAN entry
        /// </summary>
        public void AddDnsSan()
        {
            DnsSans.Add(new SanEntry { Type = SanType.DNS, Value = "" });
        }

        /// <summary>
        /// Add IP SAN entry
        /// </summary>
        public void AddIpSan()
        {
            IpSans.Add(new SanEntry { Type = SanType.IP, Value = "" });
        }

        /// <summary>
        /// Remove DNS SAN entry
        /// </summary>
        public void RemoveDnsSan(SanEntry entry)
        {
            DnsSans.Remove(entry);
        }

        /// <summary>
        /// Remove IP SAN entry
        /// </summary>
        public void RemoveIpSan(SanEntry entry)
        {
            IpSans.Remove(entry);
        }

        /// <summary>
        /// Clear the form
        /// </summary>
        public void Clear()
        {
            HostName = "";
            FQDN = "";
            Location = "";
            State = "";
            Company = "root.mpmaterials.com";
            OU = "IT";
            Type = CertificateType.Standard;
            CsrFilePath = "";
            ExtractPemKey = false;
            PfxPassword = "";
            ConfirmPassword = "";
            ShowPassword = false;
            ShowConfirmPassword = false;

            DnsSans.Clear();
            IpSans.Clear();

            // Add default SANs
            AddDnsSan();
            AddIpSan();
        }

        /// <summary>
        /// Clear form fields when certificate type changes (preserves type and defaults)
        /// </summary>
        private void ClearFormFields()
        {
            HostName = "";
            FQDN = "";
            Location = "";
            State = "";
            CsrFilePath = "";
            PfxPassword = "";
            ConfirmPassword = "";
            ShowPassword = false;
            ShowConfirmPassword = false;

            DnsSans.Clear();
            IpSans.Clear();

            // Add default SANs
            AddDnsSan();
            AddIpSan();
        }

        /// <summary>
        /// Convert to CertificateRequest model
        /// </summary>
        public CertificateRequest ToCertificateRequest()
        {
            var request = new CertificateRequest
            {
                HostName = HostName,
                FQDN = FQDN,
                Location = Location,
                State = State,
                Company = Company,
                OU = OU,
                Type = Type,
                CsrFilePath = CsrFilePath,
                ExtractPemKey = ExtractPemKey,
                PfxPassword = SecureStringHelper.StringToSecureString(PfxPassword),
                ConfirmPassword = !string.IsNullOrEmpty(ConfirmPassword)
            };

            // Copy SANs
            foreach (var dns in DnsSans)
            {
                if (!string.IsNullOrWhiteSpace(dns.Value))
                {
                    request.DnsSans.Add(dns);
                }
            }

            foreach (var ip in IpSans)
            {
                if (!string.IsNullOrWhiteSpace(ip.Value))
                {
                    request.IpSans.Add(ip);
                }
            }

            return request;
        }

        /// <summary>
        /// Update FQDN based on hostname and company
        /// </summary>
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

        /// <summary>
        /// Browse for CSR file
        /// </summary>
        private void BrowseCsr()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Certificate Request Files (*.csr;*.req)|*.csr;*.req|All Files (*.*)|*.*",
                Title = "Select CSR File"
            };

            if (dialog.ShowDialog() == true)
            {
                CsrFilePath = dialog.FileName;
            }
        }

        /// <summary>
        /// Toggle password visibility
        /// </summary>
        private void TogglePasswordVisibility()
        {
            ShowPassword = !ShowPassword;
        }

        /// <summary>
        /// Toggle confirm password visibility
        /// </summary>
        private void ToggleConfirmPasswordVisibility()
        {
            ShowConfirmPassword = !ShowConfirmPassword;
        }
    }
}

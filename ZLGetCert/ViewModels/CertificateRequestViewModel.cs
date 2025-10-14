using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Windows.Input;
using ZLGetCert.Models;
using ZLGetCert.Enums;
using ZLGetCert.Utilities;
using ZLGetCert.Services;

namespace ZLGetCert.ViewModels
{
    /// <summary>
    /// ViewModel for certificate request form
    /// </summary>
    public class CertificateRequestViewModel : BaseViewModel, IDisposable
    {
        private string _hostName;
        private string _fqdn;
        private string _location;
        private string _state;
        private string _company;
        private string _ou;
        private string _caServer;
        private string _template;
        private string _csrFilePath;
        private bool _extractPemKey;
        private bool _extractCaBundle;
        private bool _isWildcard;
        private SecureString _pfxPassword;
        private SecureString _confirmPassword;
        private bool _showPassword;
        private bool _showConfirmPassword;
        private readonly CertificateService _certificateService;
        private bool _disposed = false;

        public CertificateRequestViewModel()
        {
            // Initialize services
            _certificateService = CertificateService.Instance;
            
            // Initialize collections
            DnsSans = new ObservableCollection<SanEntry>();
            IpSans = new ObservableCollection<SanEntry>();

            // Initialize properties
            _company = "example.com";
            _ou = "IT";
            _caServer = "";
            _template = "";
            _extractPemKey = true; // Default to true since it's always available
            _extractCaBundle = true; // Default to true - extract CA chain
            _isWildcard = false;
            _showPassword = false;
            _showConfirmPassword = false;

            // Initialize commands
            BrowseCsrCommand = new RelayCommand(BrowseCsr);
            ImportFromCSRCommand = new RelayCommand(ImportFromCSR);
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
        /// Certificate Authority server
        /// </summary>
        public string CAServer
        {
            get => _caServer;
            set
            {
                if (SetProperty(ref _caServer, value))
                {
                    OnPropertyChanged(nameof(CanGenerate));
                    // Refresh templates when CA server changes
                    RefreshTemplates();
                }
            }
        }

        /// <summary>
        /// Certificate template
        /// </summary>
        public string Template
        {
            get => _template;
            set
            {
                if (SetProperty(ref _template, value))
                {
                    // CRITICAL FIX: Auto-configure certificate type based on selected template
                    AutoConfigureFromTemplate(value);
                    OnPropertyChanged(nameof(CanGenerate));
                }
            }
        }

        /// <summary>
        /// Whether this is a wildcard certificate (*.domain.com)
        /// Only applicable for web server templates
        /// </summary>
        public bool IsWildcard
        {
            get => _isWildcard;
            set
            {
                if (SetProperty(ref _isWildcard, value))
                {
                    // Update FQDN when wildcard changes
                    UpdateFQDN();
                    OnPropertyChanged(nameof(CanGenerate));
                    OnPropertyChanged(nameof(CertificateName));
                }
            }
        }

        /// <summary>
        /// Internal certificate type - derived from template selection
        /// NOT exposed to UI - template selection determines this automatically
        /// </summary>
        internal CertificateType Type
        {
            get
            {
                // Derive type from template
                if (string.IsNullOrEmpty(Template))
                    return CertificateType.Custom;

                var detectedType = CertificateTemplate.DetectTypeFromTemplateName(Template);
                
                // If it's a web server template and wildcard is checked, return Wildcard
                if ((detectedType == CertificateType.Standard) && IsWildcard)
                    return CertificateType.Wildcard;
                
                return detectedType;
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
            set
            {
                if (SetProperty(ref _extractPemKey, value))
                {
                    OnPropertyChanged(nameof(ShowKeySecurityWarning));
                }
            }
        }

        /// <summary>
        /// Whether to show security warning about unencrypted keys
        /// </summary>
        public bool ShowKeySecurityWarning => ExtractPemKey;

        /// <summary>
        /// Whether to extract CA bundle (intermediate + root certificates)
        /// </summary>
        public bool ExtractCaBundle
        {
            get => _extractCaBundle;
            set => SetProperty(ref _extractCaBundle, value);
        }

        /// <summary>
        /// PFX password (SecureString for security)
        /// </summary>
        public SecureString PfxPassword
        {
            get => _pfxPassword;
            set
            {
                // Dispose old SecureString
                _pfxPassword?.Dispose();
                
                if (SetProperty(ref _pfxPassword, value))
                {
                    OnPropertyChanged(nameof(PasswordStrength));
                    OnPropertyChanged(nameof(CanGenerate));
                }
            }
        }

        /// <summary>
        /// Confirm password (SecureString for security)
        /// </summary>
        public SecureString ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                // Dispose old SecureString
                _confirmPassword?.Dispose();
                
                if (SetProperty(ref _confirmPassword, value))
                {
                    OnPropertyChanged(nameof(CanGenerate));
                }
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
        /// Available certificate templates from CA
        /// </summary>
        public List<CertificateTemplate> AvailableTemplates { get; set; }

        /// <summary>
        /// Available Certificate Authority servers from Active Directory
        /// </summary>
        public List<string> AvailableCAs { get; set; }

        /// <summary>
        /// Whether wildcard checkbox should be visible (only for web server templates)
        /// </summary>
        public bool ShowWildcardOption
        {
            get
            {
                var type = Type;
                return type == CertificateType.Standard || type == CertificateType.Wildcard;
            }
        }

        /// <summary>
        /// Whether hostname/FQDN fields should be visible (not for client auth, code signing, email)
        /// </summary>
        public bool ShowHostnameFields
        {
            get
            {
                var type = Type;
                return type == CertificateType.Standard || type == CertificateType.Wildcard;
            }
        }

        /// <summary>
        /// Certificate name for display
        /// </summary>
        public string CertificateName
        {
            get
            {
                if (IsWildcard)
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
                if (PfxPassword == null || PfxPassword.Length == 0)
                    return "No password set";

                var strength = SecureStringHelper.ValidatePasswordStrength(PfxPassword);
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
                // CA Server and Template are required for all certificate types
                if (string.IsNullOrWhiteSpace(CAServer) || string.IsNullOrWhiteSpace(Template))
                    return false;

                // Password is always required
                if (PfxPassword == null || PfxPassword.Length == 0)
                    return false;

                // For FromCSR workflow (when CSR file path is set), less validation needed
                if (!string.IsNullOrWhiteSpace(CsrFilePath))
                {
                    return true; // CSR file path + password is sufficient
                }

                // Check if passwords match
                bool passwordsMatch = true;
                if (ConfirmPassword != null && ConfirmPassword.Length > 0)
                {
                    passwordsMatch = SecureStringHelper.SecureStringEquals(PfxPassword, ConfirmPassword);
                }

                // For web server templates (Standard/Wildcard), require hostname
                var currentType = Type;
                if (currentType == CertificateType.Standard || currentType == CertificateType.Wildcard)
                {
                    return !string.IsNullOrWhiteSpace(HostName) &&
                           !string.IsNullOrWhiteSpace(Location) &&
                           !string.IsNullOrWhiteSpace(State) &&
                           passwordsMatch;
                }
                
                // For other templates (ClientAuth, CodeSigning, Email), hostname not required
                return !string.IsNullOrWhiteSpace(Location) &&
                       !string.IsNullOrWhiteSpace(State) &&
                       passwordsMatch;
            }
        }

        /// <summary>
        /// Browse for CSR file command (for file path selection)
        /// </summary>
        public ICommand BrowseCsrCommand { get; }

        /// <summary>
        /// Import certificate from existing CSR file command (separate workflow)
        /// </summary>
        public ICommand ImportFromCSRCommand { get; }

        /// <summary>
        /// Toggle password visibility command
        /// </summary>
        public ICommand TogglePasswordVisibilityCommand { get; }

        /// <summary>
        /// Toggle confirm password visibility command
        /// </summary>
        public ICommand ToggleConfirmPasswordVisibilityCommand { get; }

        /// <summary>
        /// Whether the CSR import workflow is active
        /// </summary>
        public bool IsCSRWorkflow => !string.IsNullOrWhiteSpace(CsrFilePath);

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
            Company = "example.com";
            OU = "IT";
            CsrFilePath = "";
            ExtractPemKey = false;
            IsWildcard = false;
            
            // Dispose and clear SecureStrings
            PfxPassword?.Dispose();
            PfxPassword = null;
            ConfirmPassword?.Dispose();
            ConfirmPassword = null;
            
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
            
            // Dispose and clear SecureStrings
            PfxPassword?.Dispose();
            PfxPassword = null;
            ConfirmPassword?.Dispose();
            ConfirmPassword = null;
            
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
            // Determine certificate type from template and wildcard setting
            var certType = Type; // Uses internal Type property that derives from template
            
            // Override to FromCSR if we have a CSR file path
            if (!string.IsNullOrWhiteSpace(CsrFilePath))
            {
                certType = CertificateType.FromCSR;
            }

            var request = new CertificateRequest
            {
                HostName = HostName,
                FQDN = FQDN,
                Location = Location,
                State = State,
                Company = Company,
                OU = OU,
                CAServer = CAServer,
                Template = Template,
                Type = certType, // Type is now derived, not user-selected
                CsrFilePath = CsrFilePath,
                ExtractPemKey = ExtractPemKey,
                ExtractCaBundle = ExtractCaBundle,
                PfxPassword = PfxPassword?.Copy(), // Create a copy of SecureString
                ConfirmPassword = ConfirmPassword != null && ConfirmPassword.Length > 0
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
            if (IsWildcard)
            {
                FQDN = $"*.{Company}";
            }
            else if (!string.IsNullOrEmpty(HostName) && !string.IsNullOrEmpty(Company))
            {
                FQDN = $"{HostName}.{Company}";
            }
        }

        /// <summary>
        /// Browse for CSR file (opens file dialog)
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
                OnPropertyChanged(nameof(IsCSRWorkflow));
            }
        }

        /// <summary>
        /// Import certificate from existing CSR file
        /// This is a separate workflow from generating new certificates
        /// </summary>
        private void ImportFromCSR()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Certificate Request Files (*.csr;*.req)|*.csr;*.req|All Files (*.*)|*.*",
                Title = "Select CSR File to Import and Submit to CA"
            };

            if (dialog.ShowDialog() == true)
            {
                CsrFilePath = dialog.FileName;
                OnPropertyChanged(nameof(IsCSRWorkflow));
                OnPropertyChanged(nameof(CanGenerate));
                
                System.Windows.MessageBox.Show(
                    "CSR file loaded successfully!\n\n" +
                    "The CSR already contains:\n" +
                    "✓ Hostname and domain information\n" +
                    "✓ Organization details (location, state, company, OU)\n" +
                    "✓ Subject Alternative Names (SANs)\n\n" +
                    "You only need to:\n" +
                    "1. Select a CA Server\n" +
                    "2. Select a Template (for submission)\n" +
                    "3. Enter a PFX password\n" +
                    "4. Click Generate Certificate",
                    "CSR Import",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
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

        /// <summary>
        /// Refresh available templates from the selected CA server
        /// </summary>
        private void RefreshTemplates()
        {
            if (string.IsNullOrWhiteSpace(CAServer))
            {
                AvailableTemplates = new List<CertificateTemplate>();
                OnPropertyChanged(nameof(AvailableTemplates));
                return;
            }

            try
            {
                var templates = _certificateService.GetAvailableTemplates(CAServer);
                AvailableTemplates = templates;
                OnPropertyChanged(nameof(AvailableTemplates));
            }
            catch (Exception ex)
            {
                // Log error but don't crash the UI
                AvailableTemplates = new List<CertificateTemplate>();
                OnPropertyChanged(nameof(AvailableTemplates));
            }
        }

        /// <summary>
        /// Auto-configure UI based on selected template
        /// CRITICAL: Determines what fields are shown and certificate configuration
        /// </summary>
        private void AutoConfigureFromTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                OnPropertyChanged(nameof(ShowWildcardOption));
                OnPropertyChanged(nameof(ShowHostnameFields));
                return;
            }

            try
            {
                // Detect type to determine what UI elements to show
                var detectedType = CertificateTemplate.DetectTypeFromTemplateName(templateName);
                
                System.Diagnostics.Debug.WriteLine(
                    $"Template '{templateName}' detected as type '{detectedType}'");
                
                // Update UI visibility
                OnPropertyChanged(nameof(ShowWildcardOption));
                OnPropertyChanged(nameof(ShowHostnameFields));
                
                // Reset wildcard if not a web server template
                if (detectedType != CertificateType.Standard && detectedType != CertificateType.Wildcard)
                {
                    IsWildcard = false;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error auto-configuring from template: {ex.Message}");
            }
        }

        /// <summary>
        /// Dispose of SecureStrings to clear passwords from memory
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose SecureStrings to clear passwords from memory
                    _pfxPassword?.Dispose();
                    _pfxPassword = null;
                    
                    _confirmPassword?.Dispose();
                    _confirmPassword = null;
                }
                
                _disposed = true;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
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
        private bool _securityWarningExpanded;
        private bool _isFqdnManuallyEdited;
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
            _securityWarningExpanded = false;

            // Initialize commands
            BrowseCsrCommand = new RelayCommand(BrowseCsr);
            ImportFromCSRCommand = new RelayCommand(ImportFromCSR);
            TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);
            ToggleConfirmPasswordVisibilityCommand = new RelayCommand(ToggleConfirmPasswordVisibility);
            GeneratePasswordCommand = new RelayCommand(GenerateStrongPassword);
            CopyPasswordCommand = new RelayCommand(CopyPasswordToClipboard, CanCopyPassword);
            ToggleSecurityWarningCommand = new RelayCommand(ToggleSecurityWarning);
            ToggleFqdnEditModeCommand = new RelayCommand(ToggleFqdnEditMode);

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
                OnPropertyChanged(nameof(HostNameError));
                OnPropertyChanged(nameof(ValidationSummary));
                OnPropertyChanged(nameof(HasValidationErrors));
                OnPropertyChanged(nameof(CertificateSubjectPreview));
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
                OnPropertyChanged(nameof(FqdnDisplayText));
                OnPropertyChanged(nameof(CertificateSubjectPreview));
            }
        }

        /// <summary>
        /// Whether FQDN is manually edited (not auto-generated)
        /// </summary>
        public bool IsFqdnManuallyEdited
        {
            get => _isFqdnManuallyEdited;
            set
            {
                if (SetProperty(ref _isFqdnManuallyEdited, value))
                {
                    OnPropertyChanged(nameof(IsFqdnReadOnly));
                    OnPropertyChanged(nameof(FqdnDisplayText));
                    OnPropertyChanged(nameof(FqdnEditButtonText));
                }
            }
        }

        /// <summary>
        /// Whether FQDN field is read-only (not in edit mode)
        /// </summary>
        public bool IsFqdnReadOnly => !IsFqdnManuallyEdited;

        /// <summary>
        /// Display text showing if FQDN is auto-generated or manually edited
        /// </summary>
        public string FqdnDisplayText
        {
            get
            {
                if (IsFqdnManuallyEdited)
                    return "✏️ (manually edited)";
                return "⚡ (auto-generated)";
            }
        }

        /// <summary>
        /// Button text for FQDN edit toggle
        /// </summary>
        public string FqdnEditButtonText => IsFqdnManuallyEdited ? "Auto" : "Edit";

        /// <summary>
        /// Tooltip for FQDN field
        /// </summary>
        public string FqdnTooltip
        {
            get
            {
                if (IsFqdnManuallyEdited)
                {
                    return "FQDN is manually edited. Click 'Auto' to restore automatic generation from hostname + organization.";
                }
                
                if (IsWildcard)
                {
                    return $"Automatically generated wildcard FQDN:\n  *.{Company}\n\nClick 'Edit' to manually override if needed.";
                }
                
                return $"Automatically generated from:\n  Hostname ({HostName}) + Organization ({Company})\n  = {FQDN}\n\nClick 'Edit' to manually override if needed.";
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
                OnPropertyChanged(nameof(LocationError));
                OnPropertyChanged(nameof(ValidationSummary));
                OnPropertyChanged(nameof(HasValidationErrors));
                OnPropertyChanged(nameof(CertificateSubjectPreview));
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
                OnPropertyChanged(nameof(StateError));
                OnPropertyChanged(nameof(ValidationSummary));
                OnPropertyChanged(nameof(HasValidationErrors));
                OnPropertyChanged(nameof(CertificateSubjectPreview));
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
                OnPropertyChanged(nameof(CertificateSubjectPreview));
                OnPropertyChanged(nameof(FqdnTooltip));
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
                OnPropertyChanged(nameof(CertificateSubjectPreview));
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
                    OnPropertyChanged(nameof(CAServerError));
                    OnPropertyChanged(nameof(ValidationSummary));
                    OnPropertyChanged(nameof(HasValidationErrors));
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
                    OnPropertyChanged(nameof(TemplateError));
                    OnPropertyChanged(nameof(HostNameError)); // Template affects hostname requirement
                    OnPropertyChanged(nameof(ValidationSummary));
                    OnPropertyChanged(nameof(HasValidationErrors));
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
                    OnPropertyChanged(nameof(HostNameError)); // Wildcard affects hostname requirement
                    OnPropertyChanged(nameof(ValidationSummary));
                    OnPropertyChanged(nameof(HasValidationErrors));
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
        /// Whether the security warning details are expanded
        /// </summary>
        public bool SecurityWarningExpanded
        {
            get => _securityWarningExpanded;
            set => SetProperty(ref _securityWarningExpanded, value);
        }

        /// <summary>
        /// Expand/collapse indicator for security warning
        /// </summary>
        public string SecurityWarningIndicator => SecurityWarningExpanded ? "▲" : "▼";

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
                    OnPropertyChanged(nameof(PasswordStrengthValue));
                    OnPropertyChanged(nameof(PasswordValidation));
                    OnPropertyChanged(nameof(CanGenerate));
                    OnPropertyChanged(nameof(ConfirmPasswordError));
                    OnPropertyChanged(nameof(ValidationSummary));
                    OnPropertyChanged(nameof(HasValidationErrors));
                    ((RelayCommand)CopyPasswordCommand).RaiseCanExecuteChanged();
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
                    OnPropertyChanged(nameof(ConfirmPasswordError));
                    OnPropertyChanged(nameof(ValidationSummary));
                    OnPropertyChanged(nameof(HasValidationErrors));
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
        /// Password strength enum for binding to visual elements
        /// </summary>
        public PasswordStrength PasswordStrengthValue
        {
            get
            {
                if (PfxPassword == null || PfxPassword.Length == 0)
                    return ZLGetCert.Utilities.PasswordStrength.Empty;

                return SecureStringHelper.ValidatePasswordStrength(PfxPassword);
            }
        }

        /// <summary>
        /// Password requirements text
        /// </summary>
        public string PasswordRequirements => "Requirements: 8+ characters with uppercase, lowercase, and numbers";

        /// <summary>
        /// Password validation status
        /// </summary>
        public string PasswordValidation
        {
            get
            {
                if (PfxPassword == null || PfxPassword.Length == 0)
                    return "";

                var plainPassword = SecureStringHelper.SecureStringToString(PfxPassword);
                var issues = new List<string>();

                if (plainPassword.Length < 8)
                    issues.Add("At least 8 characters");
                if (!plainPassword.Any(char.IsUpper))
                    issues.Add("One uppercase letter");
                if (!plainPassword.Any(char.IsLower))
                    issues.Add("One lowercase letter");
                if (!plainPassword.Any(char.IsDigit))
                    issues.Add("One number");

                if (issues.Any())
                    return "Missing: " + string.Join(", ", issues);

                return "✓ All requirements met";
            }
        }

        /// <summary>
        /// CA Server validation error message
        /// </summary>
        public string CAServerError
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CAServer))
                    return "CA Server is required";
                return string.Empty;
            }
        }

        /// <summary>
        /// Template validation error message
        /// </summary>
        public string TemplateError
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Template))
                    return "Certificate Template is required";
                return string.Empty;
            }
        }

        /// <summary>
        /// Hostname validation error message
        /// </summary>
        public string HostNameError
        {
            get
            {
                var currentType = Type;
                if (currentType != CertificateType.Standard && currentType != CertificateType.Wildcard)
                    return string.Empty; // Not required for non-web server certs

                if (IsWildcard)
                    return string.Empty; // Not required for wildcard

                if (string.IsNullOrWhiteSpace(HostName))
                    return "Hostname is required for standard certificates";
                
                if (!ValidationHelper.IsValidDnsName(HostName))
                    return "Invalid hostname format (use only letters, numbers, hyphens)";
                
                return string.Empty;
            }
        }

        /// <summary>
        /// Location validation error message
        /// </summary>
        public string LocationError
        {
            get
            {
                if (IsCSRWorkflow)
                    return string.Empty; // Not validated for CSR workflow

                if (string.IsNullOrWhiteSpace(Location))
                    return "Location (City) is required";
                return string.Empty;
            }
        }

        /// <summary>
        /// State validation error message
        /// </summary>
        public string StateError
        {
            get
            {
                if (IsCSRWorkflow)
                    return string.Empty; // Not validated for CSR workflow

                if (string.IsNullOrWhiteSpace(State))
                    return "State is required";
                
                if (State != null && State.Length != 2)
                    return "State must be exactly 2 letters (e.g., WA, CA, NY)";
                
                return string.Empty;
            }
        }

        /// <summary>
        /// Password confirmation error message
        /// </summary>
        public string ConfirmPasswordError
        {
            get
            {
                if (PfxPassword == null || PfxPassword.Length == 0)
                    return string.Empty; // Don't show error if no password yet

                if (ConfirmPassword == null || ConfirmPassword.Length == 0)
                    return "Please confirm your password";
                
                if (!SecureStringHelper.SecureStringEquals(PfxPassword, ConfirmPassword))
                    return "Passwords do not match";
                
                return string.Empty;
            }
        }

        /// <summary>
        /// Get list of missing required fields for validation summary
        /// </summary>
        public List<string> MissingRequiredFields
        {
            get
            {
                var missing = new List<string>();

                if (!string.IsNullOrEmpty(CAServerError))
                    missing.Add("CA Server");

                if (!string.IsNullOrEmpty(TemplateError))
                    missing.Add("Certificate Template");

                if (!string.IsNullOrEmpty(HostNameError))
                    missing.Add("Hostname");

                if (!string.IsNullOrEmpty(LocationError))
                    missing.Add("Location (City)");

                if (!string.IsNullOrEmpty(StateError))
                    missing.Add("State");

                if (PfxPassword == null || PfxPassword.Length == 0)
                    missing.Add("PFX Password");

                if (!string.IsNullOrEmpty(ConfirmPasswordError))
                    missing.Add("Password Confirmation");

                return missing;
            }
        }

        /// <summary>
        /// Validation summary message
        /// </summary>
        public string ValidationSummary
        {
            get
            {
                var missing = MissingRequiredFields;
                if (missing.Count == 0)
                    return "✓ All required fields completed";
                
                return $"⚠ Missing {missing.Count} required field{(missing.Count == 1 ? "" : "s")}: {string.Join(", ", missing)}";
            }
        }

        /// <summary>
        /// Whether validation summary should show error state
        /// </summary>
        public bool HasValidationErrors => MissingRequiredFields.Count > 0;

        /// <summary>
        /// Preview of the certificate subject Distinguished Name (DN)
        /// Shows how the organization fields map to X.500 attributes
        /// </summary>
        public string CertificateSubjectPreview
        {
            get
            {
                var parts = new List<string>();

                // CN (Common Name) - from FQDN or hostname
                if (!string.IsNullOrWhiteSpace(FQDN))
                    parts.Add($"CN={FQDN}");
                else if (!string.IsNullOrWhiteSpace(HostName))
                    parts.Add($"CN={HostName}");

                // OU (Organizational Unit)
                if (!string.IsNullOrWhiteSpace(OU))
                    parts.Add($"OU={OU}");

                // O (Organization)
                if (!string.IsNullOrWhiteSpace(Company))
                    parts.Add($"O={Company}");

                // L (Locality/City)
                if (!string.IsNullOrWhiteSpace(Location))
                    parts.Add($"L={Location}");

                // S (State/Province)
                if (!string.IsNullOrWhiteSpace(State))
                    parts.Add($"S={State}");

                // C (Country) - Always US for this app
                parts.Add("C=US");

                if (parts.Count == 0)
                    return "(Enter organization information to see certificate subject preview)";

                return string.Join(", ", parts);
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
        /// Generate strong password command
        /// </summary>
        public ICommand GeneratePasswordCommand { get; }

        /// <summary>
        /// Copy password to clipboard command
        /// </summary>
        public ICommand CopyPasswordCommand { get; }

        /// <summary>
        /// Toggle security warning expand/collapse command
        /// </summary>
        public ICommand ToggleSecurityWarningCommand { get; }

        /// <summary>
        /// Toggle FQDN edit mode command
        /// </summary>
        public ICommand ToggleFqdnEditModeCommand { get; }

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
        /// Only updates if not in manual edit mode
        /// </summary>
        private void UpdateFQDN()
        {
            // Don't auto-update if user has manually edited the FQDN
            if (IsFqdnManuallyEdited)
                return;

            if (IsWildcard)
            {
                FQDN = $"*.{Company}";
            }
            else if (!string.IsNullOrEmpty(HostName) && !string.IsNullOrEmpty(Company))
            {
                FQDN = $"{HostName}.{Company}";
            }

            // Update tooltip when FQDN changes
            OnPropertyChanged(nameof(FqdnTooltip));
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
        /// Generate a strong password
        /// </summary>
        private void GenerateStrongPassword()
        {
            try
            {
                const int passwordLength = 16;
                const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
                const string digits = "0123456789";
                const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";
                const string allChars = upperCase + lowerCase + digits + special;

                using (var rng = RandomNumberGenerator.Create())
                {
                    var password = new StringBuilder();
                    
                    // Ensure at least one character from each category
                    password.Append(GetRandomChar(upperCase, rng));
                    password.Append(GetRandomChar(lowerCase, rng));
                    password.Append(GetRandomChar(digits, rng));
                    password.Append(GetRandomChar(special, rng));

                    // Fill the rest with random characters
                    for (int i = 4; i < passwordLength; i++)
                    {
                        password.Append(GetRandomChar(allChars, rng));
                    }

                    // Shuffle the password
                    var passwordChars = password.ToString().ToCharArray();
                    for (int i = passwordChars.Length - 1; i > 0; i--)
                    {
                        int j = GetRandomInt(rng, i + 1);
                        var temp = passwordChars[i];
                        passwordChars[i] = passwordChars[j];
                        passwordChars[j] = temp;
                    }

                    var generatedPassword = new string(passwordChars);

                    // Set the password
                    PfxPassword = SecureStringHelper.StringToSecureString(generatedPassword);
                    ConfirmPassword = SecureStringHelper.StringToSecureString(generatedPassword);

                    // Show password briefly to allow user to see it
                    ShowPassword = true;
                    ShowConfirmPassword = true;

                    OnPropertyChanged(nameof(PasswordStrength));
                    OnPropertyChanged(nameof(PasswordStrengthValue));
                    OnPropertyChanged(nameof(PasswordValidation));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating password: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Get random character from string
        /// </summary>
        private char GetRandomChar(string chars, RandomNumberGenerator rng)
        {
            int index = GetRandomInt(rng, chars.Length);
            return chars[index];
        }

        /// <summary>
        /// Get random integer using cryptographic RNG
        /// </summary>
        private int GetRandomInt(RandomNumberGenerator rng, int max)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            uint value = BitConverter.ToUInt32(bytes, 0);
            return (int)(value % (uint)max);
        }

        /// <summary>
        /// Copy password to clipboard
        /// </summary>
        private void CopyPasswordToClipboard()
        {
            try
            {
                if (PfxPassword != null && PfxPassword.Length > 0)
                {
                    var plainPassword = SecureStringHelper.SecureStringToString(PfxPassword);
                    Clipboard.SetText(plainPassword);
                    
                    MessageBox.Show(
                        "Password copied to clipboard!\n\n" +
                        "⚠️ Security Notice: The password is now in your clipboard.\n" +
                        "Please paste it into your password manager immediately\n" +
                        "and clear your clipboard afterwards.",
                        "Password Copied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying password: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Check if password can be copied
        /// </summary>
        private bool CanCopyPassword()
        {
            return PfxPassword != null && PfxPassword.Length > 0;
        }

        /// <summary>
        /// Toggle security warning expansion
        /// </summary>
        private void ToggleSecurityWarning()
        {
            SecurityWarningExpanded = !SecurityWarningExpanded;
            OnPropertyChanged(nameof(SecurityWarningIndicator));
        }

        /// <summary>
        /// Toggle FQDN edit mode (manual vs auto-generated)
        /// </summary>
        private void ToggleFqdnEditMode()
        {
            if (IsFqdnManuallyEdited)
            {
                // Switching back to auto mode - regenerate FQDN
                IsFqdnManuallyEdited = false;
                UpdateFQDN();
            }
            else
            {
                // Switching to manual mode - keep current value but allow editing
                IsFqdnManuallyEdited = true;
            }
            
            OnPropertyChanged(nameof(FqdnTooltip));
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

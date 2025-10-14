using System;
using System.Collections.ObjectModel;
using System.Security;
using System.Windows.Input;
using ZLGetCert.Models;
using ZLGetCert.Services;
using ZLGetCert.Utilities;
using ZLGetCert.Enums;
using ZLGetCert.Views;

namespace ZLGetCert.ViewModels
{
    /// <summary>
    /// Main ViewModel for the application
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly CertificateService _certificateService;
        private readonly LoggingService _logger;
        private readonly ConfigurationService _configService;
        private readonly PemExportService _pemExportService;
        private readonly FileManagementService _fileService;

        private CertificateRequestViewModel _certificateRequest;
        private SettingsViewModel _settings;
        private CertificateInfo _currentCertificate;
        private bool _isGenerating;
        private string _statusMessage;
        private bool _showSettings;

        public MainViewModel()
        {
            _certificateService = CertificateService.Instance;
            _logger = LoggingService.Instance;
            _configService = ConfigurationService.Instance;
            _pemExportService = PemExportService.Instance;
            _fileService = FileManagementService.Instance;

            // Initialize ViewModels
            _certificateRequest = new CertificateRequestViewModel();
            _settings = new SettingsViewModel();

            // Initialize commands
            GenerateCertificateCommand = new RelayCommand(GenerateCertificate, CanGenerateCertificate);
            ShowSettingsCommand = new RelayCommand(ShowSettingsPanel);
            HideSettingsCommand = new RelayCommand(HideSettings);
            ClearFormCommand = new RelayCommand(ClearForm);
            AddDnsSanCommand = new RelayCommand(AddDnsSan);
            AddIpSanCommand = new RelayCommand(AddIpSan);
            RemoveDnsSanCommand = new RelayCommand<SanEntry>(RemoveDnsSan);
            RemoveIpSanCommand = new RelayCommand<SanEntry>(RemoveIpSan);
            OpenConfigurationEditorCommand = new RelayCommand(OpenConfigurationEditor);
            OpenUsersGuideCommand = new RelayCommand(OpenUsersGuide);

            // Initialize properties
            _statusMessage = "Ready to generate certificate";
            _isGenerating = false;
            _showSettings = false;

            // Load configuration
            LoadConfiguration();

            _logger.LogInfo("MainViewModel initialized");
        }

        /// <summary>
        /// Certificate request ViewModel
        /// </summary>
        public CertificateRequestViewModel CertificateRequest
        {
            get => _certificateRequest;
            set => SetProperty(ref _certificateRequest, value);
        }

        /// <summary>
        /// Settings ViewModel
        /// </summary>
        public SettingsViewModel Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        /// <summary>
        /// Current certificate information
        /// </summary>
        public CertificateInfo CurrentCertificate
        {
            get => _currentCertificate;
            set => SetProperty(ref _currentCertificate, value);
        }

        /// <summary>
        /// Whether certificate is currently being generated
        /// </summary>
        public bool IsGenerating
        {
            get => _isGenerating;
            set
            {
                SetProperty(ref _isGenerating, value);
                ((RelayCommand)GenerateCertificateCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Status message for the user
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Whether to show settings panel
        /// </summary>
        public bool ShowSettings
        {
            get => _showSettings;
            set => SetProperty(ref _showSettings, value);
        }

        /// <summary>
        /// PEM/KEY export availability status (now always available using pure .NET)
        /// </summary>
        public string OpenSSLStatus => "PEM/KEY extraction available (built-in .NET)";

        /// <summary>
        /// Generate certificate command
        /// </summary>
        public ICommand GenerateCertificateCommand { get; }

        /// <summary>
        /// Show settings command
        /// </summary>
        public ICommand ShowSettingsCommand { get; }

        /// <summary>
        /// Hide settings command
        /// </summary>
        public ICommand HideSettingsCommand { get; }

        /// <summary>
        /// Clear form command
        /// </summary>
        public ICommand ClearFormCommand { get; }

        /// <summary>
        /// Add DNS SAN command
        /// </summary>
        public ICommand AddDnsSanCommand { get; }

        /// <summary>
        /// Add IP SAN command
        /// </summary>
        public ICommand AddIpSanCommand { get; }

        /// <summary>
        /// Remove DNS SAN command
        /// </summary>
        public ICommand RemoveDnsSanCommand { get; }

        /// <summary>
        /// Remove IP SAN command
        /// </summary>
        public ICommand RemoveIpSanCommand { get; }

        /// <summary>
        /// Open configuration editor command
        /// </summary>
        public ICommand OpenConfigurationEditorCommand { get; }

        /// <summary>
        /// Open users guide command
        /// </summary>
        public ICommand OpenUsersGuideCommand { get; }

        /// <summary>
        /// Load configuration and update UI
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                var config = _configService.GetConfiguration();
                
                // Update certificate request with default values from configuration
                CertificateRequest.Company = config.CertificateAuthority.DefaultCompany;
                CertificateRequest.OU = config.CertificateAuthority.DefaultOU;
                CertificateRequest.CAServer = config.CertificateAuthority.Server;
                CertificateRequest.Template = config.CertificateAuthority.Template;
                CertificateRequest.ExtractPemKey = true; // Always available with pure .NET implementation

                // Load available CAs from Active Directory
                var availableCAs = _certificateService.GetAvailableCAs();
                CertificateRequest.AvailableCAs = availableCAs;

                // Load available templates from CA if server is configured
                if (!string.IsNullOrEmpty(config.CertificateAuthority.Server))
                {
                    var templates = _certificateService.GetAvailableTemplates(config.CertificateAuthority.Server);
                    CertificateRequest.AvailableTemplates = templates;
                }
                
                OnPropertyChanged(nameof(CertificateRequest));

                // Update settings
                Settings.LoadConfiguration(config);

                OnPropertyChanged(nameof(OpenSSLStatus));
                _logger.LogInfo("Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration");
                StatusMessage = "Error loading configuration";
            }
        }

        /// <summary>
        /// Generate certificate
        /// </summary>
        private async void GenerateCertificate()
        {
            try
            {
                IsGenerating = true;
                StatusMessage = "Validating certificate request...";

                // Validate the request
                var validationResult = ValidationHelper.ValidateCertificateRequest(CertificateRequest.ToCertificateRequest());
                if (!validationResult.IsValid)
                {
                    StatusMessage = $"Validation failed: {validationResult.GetMessage()}";
                    return;
                }

                StatusMessage = "Generating certificate...";
                _logger.LogInfo("Starting certificate generation for {0}", CertificateRequest.CertificateName);

                // Generate certificate
                var certificate = await System.Threading.Tasks.Task.Run(() => 
                    _certificateService.GenerateCertificate(CertificateRequest.ToCertificateRequest()));

                if (certificate.IsValid)
                {
                    CurrentCertificate = certificate;
                    StatusMessage = $"Certificate generated successfully: {certificate.Subject}";
                    _logger.LogInfo("Certificate generated successfully: {0}", certificate.Thumbprint);
                }
                else
                {
                    StatusMessage = $"Certificate generation failed: {certificate.ErrorMessage}";
                    _logger.LogError("Certificate generation failed: {0}", certificate.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate");
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
            }
        }

        /// <summary>
        /// Check if certificate can be generated
        /// </summary>
        private bool CanGenerateCertificate()
        {
            return !IsGenerating && CertificateRequest != null;
        }

        /// <summary>
        /// Toggle settings panel
        /// </summary>
        private void ShowSettingsPanel()
        {
            ShowSettings = !ShowSettings;
        }

        /// <summary>
        /// Hide settings panel
        /// </summary>
        private void HideSettings()
        {
            ShowSettings = false;
        }

        /// <summary>
        /// Clear the form
        /// </summary>
        private void ClearForm()
        {
            CertificateRequest.Clear();
            CurrentCertificate = null;
            StatusMessage = "Form cleared";
        }

        /// <summary>
        /// Open configuration editor window
        /// </summary>
        private void OpenConfigurationEditor()
        {
            try
            {
                var editorWindow = new Views.ConfigurationEditorView();
                editorWindow.DataContext = new ConfigurationEditorViewModel();
                editorWindow.Owner = System.Windows.Application.Current.MainWindow;
                editorWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening configuration editor");
                System.Windows.MessageBox.Show($"Error opening configuration editor: {ex.Message}", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Open users guide window
        /// </summary>
        private void OpenUsersGuide()
        {
            try
            {
                var guideWindow = new Views.UsersGuideView();
                guideWindow.DataContext = new UsersGuideViewModel();
                guideWindow.Owner = System.Windows.Application.Current.MainWindow;
                guideWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening users guide");
                System.Windows.MessageBox.Show($"Error opening users guide: {ex.Message}", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Add DNS SAN entry
        /// </summary>
        private void AddDnsSan()
        {
            CertificateRequest.AddDnsSan();
        }

        /// <summary>
        /// Add IP SAN entry
        /// </summary>
        private void AddIpSan()
        {
            CertificateRequest.AddIpSan();
        }

        /// <summary>
        /// Remove DNS SAN entry
        /// </summary>
        private void RemoveDnsSan(SanEntry entry)
        {
            CertificateRequest.RemoveDnsSan(entry);
        }

        /// <summary>
        /// Remove IP SAN entry
        /// </summary>
        private void RemoveIpSan(SanEntry entry)
        {
            CertificateRequest.RemoveIpSan(entry);
        }
    }

    /// <summary>
    /// Simple relay command implementation
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Generic relay command implementation
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke((T)parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Security;
using System.Windows;
using System.Windows.Controls;
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
        private StatusMessageType _statusMessageType;

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
            ClearFormCommand = new RelayCommand(ClearForm);
            AddDnsSanCommand = new RelayCommand(AddDnsSan);
            AddIpSanCommand = new RelayCommand(AddIpSan);
            RemoveDnsSanCommand = new RelayCommand<SanEntry>(RemoveDnsSan);
            RemoveIpSanCommand = new RelayCommand<SanEntry>(RemoveIpSan);
            BulkAddDnsSansCommand = new RelayCommand(BulkAddDnsSans);
            BulkAddIpSansCommand = new RelayCommand(BulkAddIpSans);
            OpenConfigurationEditorCommand = new RelayCommand(OpenConfigurationEditor);
            OpenUsersGuideCommand = new RelayCommand(OpenUsersGuide);
            SaveAsDefaultsCommand = new RelayCommand(SaveAsDefaults);
            ShowTemplateHelpCommand = new RelayCommand(ShowTemplateHelp);

            // Initialize properties
            _statusMessage = "Ready to generate certificate";
            _statusMessageType = StatusMessageType.Info;
            _isGenerating = false;

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
        /// Type of status message (for color coding)
        /// </summary>
        public StatusMessageType StatusMessageType
        {
            get => _statusMessageType;
            set
            {
                if (SetProperty(ref _statusMessageType, value))
                {
                    OnPropertyChanged(nameof(StatusBarBackground));
                    OnPropertyChanged(nameof(StatusBarBorderBrush));
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(StatusTextColor));
                }
            }
        }

        /// <summary>
        /// Status bar background color based on message type
        /// </summary>
        public string StatusBarBackground
        {
            get
            {
                switch (StatusMessageType)
                {
                    case StatusMessageType.Success:
                        return "#D4EDDA"; // Light green
                    case StatusMessageType.Error:
                        return "#F8D7DA"; // Light red
                    case StatusMessageType.Warning:
                        return "#FFF3CD"; // Light yellow
                    case StatusMessageType.Info:
                    default:
                        return "#F8F9FA"; // Light gray (default)
                }
            }
        }

        /// <summary>
        /// Status bar border color based on message type
        /// </summary>
        public string StatusBarBorderBrush
        {
            get
            {
                switch (StatusMessageType)
                {
                    case StatusMessageType.Success:
                        return "#28A745"; // Green
                    case StatusMessageType.Error:
                        return "#DC3545"; // Red
                    case StatusMessageType.Warning:
                        return "#FFC107"; // Yellow/Orange
                    case StatusMessageType.Info:
                    default:
                        return "#E9ECEF"; // Light gray
                }
            }
        }

        /// <summary>
        /// Status icon based on message type
        /// </summary>
        public string StatusIcon
        {
            get
            {
                switch (StatusMessageType)
                {
                    case StatusMessageType.Success:
                        return "✓";
                    case StatusMessageType.Error:
                        return "⚠";
                    case StatusMessageType.Warning:
                        return "⚠";
                    case StatusMessageType.Info:
                    default:
                        return "📋";
                }
            }
        }

        /// <summary>
        /// Status text color based on message type
        /// </summary>
        public string StatusTextColor
        {
            get
            {
                switch (StatusMessageType)
                {
                    case StatusMessageType.Success:
                        return "#155724"; // Dark green
                    case StatusMessageType.Error:
                        return "#721C24"; // Dark red
                    case StatusMessageType.Warning:
                        return "#856404"; // Dark yellow
                    case StatusMessageType.Info:
                    default:
                        return "#383D41"; // Dark gray
                }
            }
        }


        /// <summary>
        /// PEM/KEY export availability status (now always available using pure .NET)
        /// </summary>
        public string OpenSSLStatus => "PEM/KEY extraction available (built-in .NET)";

        /// <summary>
        /// Set status message with type for color coding
        /// </summary>
        private void SetStatus(string message, StatusMessageType type)
        {
            StatusMessage = message;
            StatusMessageType = type;
        }

        /// <summary>
        /// Generate certificate command
        /// </summary>
        public ICommand GenerateCertificateCommand { get; }

        /// <summary>
        /// Show settings command
        /// </summary>
        public ICommand ShowSettingsCommand { get; }


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
        /// Bulk add DNS SANs command
        /// </summary>
        public ICommand BulkAddDnsSansCommand { get; }

        /// <summary>
        /// Bulk add IP SANs command
        /// </summary>
        public ICommand BulkAddIpSansCommand { get; }

        /// <summary>
        /// Open configuration editor command
        /// </summary>
        public ICommand OpenConfigurationEditorCommand { get; }

        /// <summary>
        /// Open users guide command
        /// </summary>
        public ICommand OpenUsersGuideCommand { get; }

        /// <summary>
        /// Save current settings as defaults command
        /// </summary>
        public ICommand SaveAsDefaultsCommand { get; }

        /// <summary>
        /// Show template selection help command
        /// </summary>
        public ICommand ShowTemplateHelpCommand { get; }

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
                CertificateRequest.Location = config.CertificateAuthority.DefaultLocation;
                CertificateRequest.State = config.CertificateAuthority.DefaultState;
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
                SetStatus("Error loading configuration", StatusMessageType.Error);
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
                SetStatus("Validating certificate request...", StatusMessageType.Info);

                // Validate the request
                var validationResult = ValidationHelper.ValidateCertificateRequest(CertificateRequest.ToCertificateRequest());
                if (!validationResult.IsValid)
                {
                    SetStatus($"Validation failed: {validationResult.GetMessage()}", StatusMessageType.Error);
                    return;
                }

                SetStatus("Generating certificate...", StatusMessageType.Info);
                _logger.LogInfo("Starting certificate generation for {0}", CertificateRequest.CertificateName);

                // Log the request details for debugging
                var request = CertificateRequest.ToCertificateRequest();
                _logger.LogInfo("Certificate request details - ExtractPemKey: {0}, ExtractCaBundle: {1}", 
                    request.ExtractPemKey, request.ExtractCaBundle);

                // Generate certificate
                var certificate = await System.Threading.Tasks.Task.Run(() => 
                    _certificateService.GenerateCertificate(request));

                if (certificate.IsValid)
                {
                    CurrentCertificate = certificate;
                    SetStatus($"✓ Certificate generated successfully! Saved to: {certificate.PfxPath}", StatusMessageType.Success);
                    _logger.LogInfo("Certificate generated successfully: {0}", certificate.Thumbprint);
                }
                else
                {
                    SetStatus($"Certificate generation failed: {certificate.ErrorMessage}", StatusMessageType.Error);
                    _logger.LogError("Certificate generation failed: {0}", certificate.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate");
                SetStatus($"Error: {ex.Message}", StatusMessageType.Error);
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
        /// Open settings window
        /// </summary>
        private void ShowSettingsPanel()
        {
            try
            {
                var settingsWindow = new Views.SettingsWindow();
                settingsWindow.DataContext = Settings;
                settingsWindow.Owner = System.Windows.Application.Current.MainWindow;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening settings window");
                System.Windows.MessageBox.Show($"Error opening settings: {ex.Message}", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Clear the form
        /// </summary>
        private void ClearForm()
        {
            CertificateRequest.Clear();
            CurrentCertificate = null;
            SetStatus("Form cleared - ready for new certificate request", StatusMessageType.Info);
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

        /// <summary>
        /// Bulk add DNS SANs from multiline input
        /// </summary>
        private void BulkAddDnsSans()
        {
            try
            {
                // Create input dialog
                var inputWindow = new Window
                {
                    Title = "Bulk Add DNS SANs",
                    Width = 500,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = System.Windows.Application.Current.MainWindow
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                var label = new TextBlock
                {
                    Text = "Enter DNS names (one per line):",
                    Margin = new Thickness(10)
                };
                Grid.SetRow(label, 0);
                grid.Children.Add(label);

                var textBox = new TextBox
                {
                    Margin = new Thickness(10, 0, 10, 10),
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                Grid.SetRow(textBox, 1);
                grid.Children.Add(textBox);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(10)
                };

                var addButton = new Button
                {
                    Content = "Add All",
                    Width = 80,
                    Margin = new Thickness(0, 0, 10, 0),
                    IsDefault = true
                };
                addButton.Click += (s, e) => { inputWindow.DialogResult = true; inputWindow.Close(); };

                var cancelButton = new Button
                {
                    Content = "Cancel",
                    Width = 80,
                    IsCancel = true
                };

                buttonPanel.Children.Add(addButton);
                buttonPanel.Children.Add(cancelButton);
                Grid.SetRow(buttonPanel, 2);
                grid.Children.Add(buttonPanel);

                inputWindow.Content = grid;

                if (inputWindow.ShowDialog() == true)
                {
                    var count = CertificateRequest.BulkAddDnsSans(textBox.Text);
                    if (count > 0)
                        SetStatus($"✓ Successfully added {count} DNS SAN(s)", StatusMessageType.Success);
                    else
                        SetStatus("No valid DNS names found in the input", StatusMessageType.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk adding DNS SANs");
                System.Windows.MessageBox.Show($"Error adding DNS SANs: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Bulk add IP SANs from multiline input
        /// </summary>
        private void BulkAddIpSans()
        {
            try
            {
                // Create input dialog
                var inputWindow = new Window
                {
                    Title = "Bulk Add IP SANs",
                    Width = 500,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = System.Windows.Application.Current.MainWindow
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                var label = new TextBlock
                {
                    Text = "Enter IP addresses (one per line):",
                    Margin = new Thickness(10)
                };
                Grid.SetRow(label, 0);
                grid.Children.Add(label);

                var textBox = new TextBox
                {
                    Margin = new Thickness(10, 0, 10, 10),
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                Grid.SetRow(textBox, 1);
                grid.Children.Add(textBox);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(10)
                };

                var addButton = new Button
                {
                    Content = "Add All",
                    Width = 80,
                    Margin = new Thickness(0, 0, 10, 0),
                    IsDefault = true
                };
                addButton.Click += (s, e) => { inputWindow.DialogResult = true; inputWindow.Close(); };

                var cancelButton = new Button
                {
                    Content = "Cancel",
                    Width = 80,
                    IsCancel = true
                };

                buttonPanel.Children.Add(addButton);
                buttonPanel.Children.Add(cancelButton);
                Grid.SetRow(buttonPanel, 2);
                grid.Children.Add(buttonPanel);

                inputWindow.Content = grid;

                if (inputWindow.ShowDialog() == true)
                {
                    var count = CertificateRequest.BulkAddIpSans(textBox.Text);
                    if (count > 0)
                        SetStatus($"✓ Successfully added {count} IP SAN(s)", StatusMessageType.Success);
                    else
                        SetStatus("No valid IP addresses found in the input", StatusMessageType.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk adding IP SANs");
                System.Windows.MessageBox.Show($"Error adding IP SANs: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Save current certificate settings as defaults
        /// </summary>
        private void SaveAsDefaults()
        {
            try
            {
                var config = _configService.GetConfiguration();
                
                // Update the configuration with current certificate request values
                config.CertificateAuthority.Server = CertificateRequest.CAServer;
                config.CertificateAuthority.Template = CertificateRequest.Template;
                config.CertificateAuthority.DefaultCompany = CertificateRequest.Company;
                config.CertificateAuthority.DefaultOU = CertificateRequest.OU;
                config.CertificateAuthority.DefaultLocation = CertificateRequest.Location;
                config.CertificateAuthority.DefaultState = CertificateRequest.State;
                
                // Save the updated configuration
                _configService.SaveConfiguration(config);
                
                SetStatus("✓ Default settings saved successfully", StatusMessageType.Success);
                _logger.LogInfo("Default certificate settings saved: CA={0}, Template={1}, Company={2}, OU={3}, Location={4}, State={5}", 
                    CertificateRequest.CAServer, CertificateRequest.Template, CertificateRequest.Company, CertificateRequest.OU, 
                    CertificateRequest.Location, CertificateRequest.State);
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to save defaults: {ex.Message}", StatusMessageType.Error);
                _logger.LogError(ex, "Failed to save default certificate settings");
            }
        }

        /// <summary>
        /// Show template selection help dialog
        /// </summary>
        private void ShowTemplateHelp()
        {
            var helpMessage = @"Don't know which certificate template to use?

Choose based on what you need the certificate for:

🔐 Web Servers (SSL/TLS)
   → Use: WebServer, SSL, TLS, WebServerV2
   → For: Apache, IIS, NGINX, HAProxy
   → Secures: HTTPS websites

✍️ Code Signing
   → Use: CodeSigning, CodeSign
   → For: Applications, scripts, executables
   → Purpose: Verify software authenticity

👤 User/Computer Authentication
   → Use: User, Workstation, Computer
   → For: VPN access, Wi-Fi, domain computers
   → Purpose: Verify user/device identity

📧 Email Encryption (S/MIME)
   → Use: EmailProtection, SMIME
   → For: Email signing and encryption
   → Purpose: Secure email communications

💡 Tip: If unsure, start with WebServer for most web applications.";

            System.Windows.MessageBox.Show(helpMessage, 
                "Certificate Template Help", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);
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

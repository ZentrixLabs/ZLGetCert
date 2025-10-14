using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ZLGetCert.Models;
using ZLGetCert.Services;
using ZLGetCert.Enums;

namespace ZLGetCert.ViewModels
{
    /// <summary>
    /// ViewModel for settings panel
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ConfigurationService _configService;
        private readonly LoggingService _logger;
        private readonly OpenSSLService _openSSLService;
        private readonly CertificateService _certService;

        private AppConfiguration _configuration;
        private bool _hasChanges;
        private bool _isLoadingTemplates;
        private string _templateLoadStatus;

        public SettingsViewModel()
        {
            _configService = ConfigurationService.Instance;
            _logger = LoggingService.Instance;
            _openSSLService = OpenSSLService.Instance;
            _certService = CertificateService.Instance;

            // Initialize commands
            SaveCommand = new RelayCommand(SaveSettings, CanSaveSettings);
            CancelCommand = new RelayCommand(CancelSettings);
            ResetCommand = new RelayCommand(ResetSettings);
            TestOpenSSLCommand = new RelayCommand(TestOpenSSL);
            RefreshTemplatesCommand = new RelayCommand(RefreshTemplates, CanRefreshTemplates);

            // Initialize properties
            _hasChanges = false;
            _isLoadingTemplates = false;
            _templateLoadStatus = "";
        }

        /// <summary>
        /// Application configuration
        /// </summary>
        public AppConfiguration Configuration
        {
            get => _configuration;
            set
            {
                SetProperty(ref _configuration, value);
                OnPropertyChanged(nameof(OpenSSLStatus));
            }
        }

        /// <summary>
        /// Whether there are unsaved changes
        /// </summary>
        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                SetProperty(ref _hasChanges, value);
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// OpenSSL status message
        /// </summary>
        public string OpenSSLStatus => _openSSLService.IsAvailable ? 
            $"OpenSSL Available - {_openSSLService.GetConfig().DisplayStatus}" : 
            "OpenSSL Not Available";

        /// <summary>
        /// Save settings command
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// Cancel settings command
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Reset settings command
        /// </summary>
        public ICommand ResetCommand { get; }

        /// <summary>
        /// Test OpenSSL command
        /// </summary>
        public ICommand TestOpenSSLCommand { get; }

        /// <summary>
        /// Refresh templates command
        /// </summary>
        public ICommand RefreshTemplatesCommand { get; }

        /// <summary>
        /// Whether templates are currently being loaded
        /// </summary>
        public bool IsLoadingTemplates
        {
            get => _isLoadingTemplates;
            set
            {
                SetProperty(ref _isLoadingTemplates, value);
                ((RelayCommand)RefreshTemplatesCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Template load status message
        /// </summary>
        public string TemplateLoadStatus
        {
            get => _templateLoadStatus;
            set => SetProperty(ref _templateLoadStatus, value);
        }


        /// <summary>
        /// Load configuration
        /// </summary>
        public void LoadConfiguration(AppConfiguration config)
        {
            Configuration = config;
            HasChanges = false;
            
            // Load templates if CA server is configured
            if (!string.IsNullOrEmpty(config?.CertificateAuthority?.Server))
            {
                RefreshTemplates();
            }
        }

        /// <summary>
        /// Save settings
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                _configService.SaveConfiguration(Configuration);
                HasChanges = false;
                _logger.LogInfo("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
            }
        }

        /// <summary>
        /// Check if settings can be saved
        /// </summary>
        private bool CanSaveSettings()
        {
            return HasChanges;
        }

        /// <summary>
        /// Cancel settings changes
        /// </summary>
        private void CancelSettings()
        {
            LoadConfiguration(_configService.GetConfiguration());
        }

        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        private void ResetSettings()
        {
            Configuration = GetDefaultConfiguration();
            HasChanges = true;
        }

        /// <summary>
        /// Test OpenSSL connection
        /// </summary>
        private void TestOpenSSL()
        {
            try
            {
                _openSSLService.Reinitialize();
                OnPropertyChanged(nameof(OpenSSLStatus));
                _logger.LogInfo("OpenSSL test completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing OpenSSL");
            }
        }

        /// <summary>
        /// Refresh available templates from CA
        /// </summary>
        private void RefreshTemplates()
        {
            if (Configuration?.CertificateAuthority == null)
                return;

            IsLoadingTemplates = true;
            TemplateLoadStatus = "Loading templates...";

            try
            {
                var templates = _certService.GetAvailableTemplates(Configuration.CertificateAuthority.Server);
                
                if (templates.Count > 0)
                {
                    Configuration.CertificateAuthority.AvailableTemplates = templates;
                    TemplateLoadStatus = $"Loaded {templates.Count} template(s)";
                    _logger.LogInfo("Loaded {0} certificate templates from CA", templates.Count);
                }
                else
                {
                    TemplateLoadStatus = "No templates found or unable to connect to CA";
                    _logger.LogWarning("No certificate templates found from CA");
                }
            }
            catch (Exception ex)
            {
                TemplateLoadStatus = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error loading certificate templates");
            }
            finally
            {
                IsLoadingTemplates = false;
            }
        }

        /// <summary>
        /// Check if templates can be refreshed
        /// </summary>
        private bool CanRefreshTemplates()
        {
            return !IsLoadingTemplates && 
                   Configuration?.CertificateAuthority != null &&
                   !string.IsNullOrEmpty(Configuration.CertificateAuthority.Server);
        }

        /// <summary>
        /// Get default configuration
        /// </summary>
        private AppConfiguration GetDefaultConfiguration()
        {
            return new AppConfiguration
            {
                CertificateAuthority = new CertificateAuthorityConfig
                {
                    Server = "your-ca-server.example.com",
                    Template = "WebServer",
                    DefaultCompany = "example.com",
                    DefaultOU = "IT"
                },
                FilePaths = new FilePathsConfig
                {
                    CertificateFolder = "C:\\ssl",
                    LogPath = "C:\\ProgramData\\ZentrixLabs\\ZLGetCert"
                },
                OpenSSL = new OpenSSLConfig
                {
                    ExecutablePath = "",
                    AutoDetect = true
                },
                DefaultSettings = new DefaultSettingsConfig
                {
                    KeyLength = 2048,
                    HashAlgorithm = "sha256",
                    DefaultPassword = "password",
                    RequirePasswordConfirmation = true,
                    AutoCleanup = true,
                    RememberPassword = false,
                    AvailableHashAlgorithms = new List<string> { "sha256", "sha384", "sha512" }
                },
                Logging = new LoggingConfig
                {
                    LogLevel = Enums.LogLevel.Information,
                    LogToFile = true,
                    LogToConsole = false,
                    MaxLogFileSize = "10MB",
                    MaxLogFiles = 5,
                    AvailableLogLevels = new List<Enums.LogLevel> 
                    { 
                        Enums.LogLevel.Trace, 
                        Enums.LogLevel.Debug, 
                        Enums.LogLevel.Information, 
                        Enums.LogLevel.Warning, 
                        Enums.LogLevel.Error, 
                        Enums.LogLevel.Fatal 
                    }
                }
            };
        }
    }
}

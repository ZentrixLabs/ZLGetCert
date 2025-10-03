using System;
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

        private AppConfiguration _configuration;
        private bool _hasChanges;

        public SettingsViewModel()
        {
            _configService = ConfigurationService.Instance;
            _logger = LoggingService.Instance;
            _openSSLService = OpenSSLService.Instance;

            // Initialize commands
            SaveCommand = new RelayCommand(SaveSettings, CanSaveSettings);
            CancelCommand = new RelayCommand(CancelSettings);
            ResetCommand = new RelayCommand(ResetSettings);
            TestOpenSSLCommand = new RelayCommand(TestOpenSSL);

            // Initialize properties
            _hasChanges = false;
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
        /// Load configuration
        /// </summary>
        public void LoadConfiguration(AppConfiguration config)
        {
            Configuration = config;
            HasChanges = false;
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
        /// Get default configuration
        /// </summary>
        private AppConfiguration GetDefaultConfiguration()
        {
            return new AppConfiguration
            {
                CertificateAuthority = new CertificateAuthorityConfig
                {
                    Server = "mpazica01.root.mpmaterials.com",
                    Template = "WebServerV2",
                    DefaultCompany = "root.mpmaterials.com",
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
                    RememberPassword = false
                },
                Logging = new LoggingConfig
                {
                    LogLevel = Enums.LogLevel.Information,
                    LogToFile = true,
                    LogToConsole = false,
                    MaxLogFileSize = "10MB",
                    MaxLogFiles = 5
                }
            };
        }
    }
}

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
        private readonly PemExportService _pemExportService;

        private AppConfiguration _configuration;
        private bool _hasChanges;

        public SettingsViewModel()
        {
            _configService = ConfigurationService.Instance;
            _logger = LoggingService.Instance;
            _pemExportService = PemExportService.Instance;

            // Initialize commands
            SaveCommand = new RelayCommand(SaveSettings, CanSaveSettings);
            CancelCommand = new RelayCommand(CancelSettings);
            ResetCommand = new RelayCommand(ResetSettings);

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
        /// PEM/KEY export status (now always available using pure .NET)
        /// </summary>
        public string OpenSSLStatus => "PEM/KEY extraction available (built-in .NET)";

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
        /// Get default configuration
        /// </summary>
        private AppConfiguration GetDefaultConfiguration()
        {
            return new AppConfiguration
            {
                CertificateAuthority = new CertificateAuthorityConfig
                {
                    DefaultCompany = "example.com",
                    DefaultOU = "IT"
                },
                FilePaths = new FilePathsConfig
                {
                    CertificateFolder = "%USERPROFILE%\\Documents\\Certificates",
                    LogPath = "%APPDATA%\\ZLGetCert\\Logs",
                    TempPath = "%TEMP%\\ZLGetCert"
                },
                DefaultSettings = new DefaultSettingsConfig
                {
                    KeyLength = 2048,
                    HashAlgorithm = "sha256",
                    DefaultPassword = "", // No default password for security
                    RequirePasswordConfirmation = true,
                    AutoCleanup = true,
                    RememberPassword = false,
                    AvailableHashAlgorithms = new List<string> { "sha256", "sha384", "sha512" }
                },
                CertificateParameters = new CertificateParametersConfig(),
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

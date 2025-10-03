using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using ZLGetCert.Models;

namespace ZLGetCert.Services
{
    /// <summary>
    /// Service for managing application configuration
    /// </summary>
    public class ConfigurationService
    {
        private static readonly Lazy<ConfigurationService> _instance = new Lazy<ConfigurationService>(() => new ConfigurationService());
        public static ConfigurationService Instance => _instance.Value;

        private AppConfiguration _configuration;
        private readonly string _configPath;
        private readonly string _devConfigPath;

        private ConfigurationService()
        {
            var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _configPath = Path.Combine(appDirectory, "appsettings.json");
            _devConfigPath = Path.Combine(appDirectory, "appsettings.Development.json");
        }

        /// <summary>
        /// Get the current application configuration
        /// </summary>
        public AppConfiguration GetConfiguration()
        {
            if (_configuration == null)
            {
                LoadConfiguration();
            }
            return _configuration;
        }

        /// <summary>
        /// Load configuration from files
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // Load base configuration
                _configuration = LoadConfigurationFromFile(_configPath);

                // Override with development configuration if it exists
                if (File.Exists(_devConfigPath))
                {
                    var devConfig = LoadConfigurationFromFile(_devConfigPath);
                    if (devConfig != null)
                    {
                        MergeConfigurations(_configuration, devConfig);
                    }
                }

                // Set default values if configuration is null
                if (_configuration == null)
                {
                    _configuration = GetDefaultConfiguration();
                }
            }
            catch (Exception ex)
            {
                // Log error and use default configuration
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
                _configuration = GetDefaultConfiguration();
            }
        }

        /// <summary>
        /// Load configuration from a specific file
        /// </summary>
        private AppConfiguration LoadConfigurationFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<AppConfiguration>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration from {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Merge development configuration into base configuration
        /// </summary>
        private void MergeConfigurations(AppConfiguration baseConfig, AppConfiguration devConfig)
        {
            if (devConfig.CertificateAuthority != null)
            {
                if (!string.IsNullOrEmpty(devConfig.CertificateAuthority.Server))
                    baseConfig.CertificateAuthority.Server = devConfig.CertificateAuthority.Server;
                if (!string.IsNullOrEmpty(devConfig.CertificateAuthority.Template))
                    baseConfig.CertificateAuthority.Template = devConfig.CertificateAuthority.Template;
                if (!string.IsNullOrEmpty(devConfig.CertificateAuthority.DefaultCompany))
                    baseConfig.CertificateAuthority.DefaultCompany = devConfig.CertificateAuthority.DefaultCompany;
                if (!string.IsNullOrEmpty(devConfig.CertificateAuthority.DefaultOU))
                    baseConfig.CertificateAuthority.DefaultOU = devConfig.CertificateAuthority.DefaultOU;
            }

            if (devConfig.FilePaths != null)
            {
                if (!string.IsNullOrEmpty(devConfig.FilePaths.CertificateFolder))
                    baseConfig.FilePaths.CertificateFolder = devConfig.FilePaths.CertificateFolder;
                if (!string.IsNullOrEmpty(devConfig.FilePaths.LogPath))
                    baseConfig.FilePaths.LogPath = devConfig.FilePaths.LogPath;
            }

            if (devConfig.OpenSSL != null)
            {
                if (!string.IsNullOrEmpty(devConfig.OpenSSL.ExecutablePath))
                    baseConfig.OpenSSL.ExecutablePath = devConfig.OpenSSL.ExecutablePath;
                baseConfig.OpenSSL.AutoDetect = devConfig.OpenSSL.AutoDetect;
            }

            if (devConfig.DefaultSettings != null)
            {
                if (devConfig.DefaultSettings.KeyLength > 0)
                    baseConfig.DefaultSettings.KeyLength = devConfig.DefaultSettings.KeyLength;
                if (!string.IsNullOrEmpty(devConfig.DefaultSettings.HashAlgorithm))
                    baseConfig.DefaultSettings.HashAlgorithm = devConfig.DefaultSettings.HashAlgorithm;
                if (!string.IsNullOrEmpty(devConfig.DefaultSettings.DefaultPassword))
                    baseConfig.DefaultSettings.DefaultPassword = devConfig.DefaultSettings.DefaultPassword;
                baseConfig.DefaultSettings.RequirePasswordConfirmation = devConfig.DefaultSettings.RequirePasswordConfirmation;
                baseConfig.DefaultSettings.AutoCleanup = devConfig.DefaultSettings.AutoCleanup;
                baseConfig.DefaultSettings.RememberPassword = devConfig.DefaultSettings.RememberPassword;
            }

            if (devConfig.Logging != null)
            {
                baseConfig.Logging.LogLevel = devConfig.Logging.LogLevel;
                baseConfig.Logging.LogToFile = devConfig.Logging.LogToFile;
                baseConfig.Logging.LogToConsole = devConfig.Logging.LogToConsole;
                if (!string.IsNullOrEmpty(devConfig.Logging.MaxLogFileSize))
                    baseConfig.Logging.MaxLogFileSize = devConfig.Logging.MaxLogFileSize;
                if (devConfig.Logging.MaxLogFiles > 0)
                    baseConfig.Logging.MaxLogFiles = devConfig.Logging.MaxLogFiles;
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

        /// <summary>
        /// Save configuration to file
        /// </summary>
        public void SaveConfiguration(AppConfiguration config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(_configPath, json);
                _configuration = config;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Reload configuration from files
        /// </summary>
        public void ReloadConfiguration()
        {
            _configuration = null;
            LoadConfiguration();
        }
    }
}

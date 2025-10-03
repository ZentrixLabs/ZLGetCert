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

        private ConfigurationService()
        {
            var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _configPath = Path.Combine(appDirectory, "appsettings.json");
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
                // Load configuration
                _configuration = LoadConfigurationFromFile(_configPath);

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

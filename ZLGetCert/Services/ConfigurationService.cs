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
        private readonly string _defaultConfigPath;

        private ConfigurationService()
        {
            // Default config in Program Files (read-only template)
            var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _defaultConfigPath = Path.Combine(appDirectory, "appsettings.json");
            
            // User config in AppData (writable, no admin required)
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var userConfigDirectory = Path.Combine(appDataPath, "ZentrixLabs", "ZLGetCert");
            _configPath = Path.Combine(userConfigDirectory, "appsettings.json");
            
            // Ensure user config directory exists
            if (!Directory.Exists(userConfigDirectory))
            {
                Directory.CreateDirectory(userConfigDirectory);
            }
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
        /// Priority: 1) User config in AppData, 2) Default config in Program Files, 3) Hardcoded defaults
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // Try to load user-specific configuration from AppData first
                _configuration = LoadConfigurationFromFile(_configPath);

                // If no user config, try default config from Program Files
                if (_configuration == null)
                {
                    _configuration = LoadConfigurationFromFile(_defaultConfigPath);
                    
                    // If we loaded from default, save it to user location for future edits
                    if (_configuration != null)
                    {
                        try
                        {
                            SaveConfiguration(_configuration);
                        }
                        catch
                        {
                            // Silently fail if we can't save - just use the loaded config
                        }
                    }
                }

                // If still null, use hardcoded defaults
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
                var config = JsonConvert.DeserializeObject<AppConfiguration>(json);
                
                // Expand environment variables in file paths
                if (config?.FilePaths != null)
                {
                    config.FilePaths.CertificateFolder = ExpandEnvironmentVariables(config.FilePaths.CertificateFolder);
                    config.FilePaths.LogPath = ExpandEnvironmentVariables(config.FilePaths.LogPath);
                    config.FilePaths.TempPath = ExpandEnvironmentVariables(config.FilePaths.TempPath);
                }
                
                return config;
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
                    DefaultPassword = "", // No default password for security
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
        /// Save configuration to user-specific file in AppData (no admin required)
        /// </summary>
        public void SaveConfiguration(AppConfiguration config)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(_configPath, json);
                _configuration = config;
                
                System.Diagnostics.Debug.WriteLine($"Configuration saved to: {_configPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving configuration to {_configPath}: {ex.Message}");
                throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get the path where user configuration is saved
        /// </summary>
        public string GetUserConfigPath()
        {
            return _configPath;
        }
        
        /// <summary>
        /// Get the path of the default configuration template
        /// </summary>
        public string GetDefaultConfigPath()
        {
            return _defaultConfigPath;
        }

        /// <summary>
        /// Reload configuration from files
        /// </summary>
        public void ReloadConfiguration()
        {
            _configuration = null;
            LoadConfiguration();
        }

        /// <summary>
        /// Expand environment variables in a path string
        /// </summary>
        private string ExpandEnvironmentVariables(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            try
            {
                return Environment.ExpandEnvironmentVariables(path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error expanding environment variables in path '{path}': {ex.Message}");
                return path; // Return original path if expansion fails
            }
        }
    }
}

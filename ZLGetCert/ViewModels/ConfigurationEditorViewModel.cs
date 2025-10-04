using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using ZLGetCert.Models;
using ZLGetCert.Services;
using ZLGetCert.Views;

namespace ZLGetCert.ViewModels
{
    /// <summary>
    /// ViewModel for configuration editor window
    /// </summary>
    public class ConfigurationEditorViewModel : BaseViewModel
    {
        private readonly ConfigurationService _configService;
        private readonly LoggingService _logger;
        private string _configurationJson;
        private string _originalConfigurationJson;
        private bool _hasChanges;
        private string _validationIcon;
        private string _validationMessage;
        private string _validationDetails;
        private string _validationBackground;
        private string _validationBorderBrush;
        private string _validationForeground;
        private bool _hasValidationDetails;
        private bool _isJsonValid;

        public ConfigurationEditorViewModel()
        {
            _configService = ConfigurationService.Instance;
            _logger = LoggingService.Instance;

            // Initialize commands
            LoadFromFileCommand = new RelayCommand(LoadFromFile);
            SaveToFileCommand = new RelayCommand(SaveToFile, CanSave);
            ResetToDefaultCommand = new RelayCommand(ResetToDefault);
            ApplyAndCloseCommand = new RelayCommand(ApplyAndClose, CanApply);
            CancelCommand = new RelayCommand(Cancel);

            // Load current configuration
            LoadCurrentConfiguration();
        }

        /// <summary>
        /// Current configuration as JSON string
        /// </summary>
        public string ConfigurationJson
        {
            get => _configurationJson;
            set
            {
                if (SetProperty(ref _configurationJson, value))
                {
                    HasChanges = _configurationJson != _originalConfigurationJson;
                    ((RelayCommand)SaveToFileCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ApplyAndCloseCommand).RaiseCanExecuteChanged();
                    
                    // Validate JSON in real-time
                    ValidateJson();
                }
            }
        }

        /// <summary>
        /// Whether there are unsaved changes
        /// </summary>
        public bool HasChanges
        {
            get => _hasChanges;
            set => SetProperty(ref _hasChanges, value);
        }

        /// <summary>
        /// Validation icon
        /// </summary>
        public string ValidationIcon
        {
            get => _validationIcon;
            set => SetProperty(ref _validationIcon, value);
        }

        /// <summary>
        /// Validation message
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// Validation details
        /// </summary>
        public string ValidationDetails
        {
            get => _validationDetails;
            set => SetProperty(ref _validationDetails, value);
        }

        /// <summary>
        /// Validation background color
        /// </summary>
        public string ValidationBackground
        {
            get => _validationBackground;
            set => SetProperty(ref _validationBackground, value);
        }

        /// <summary>
        /// Validation border brush
        /// </summary>
        public string ValidationBorderBrush
        {
            get => _validationBorderBrush;
            set => SetProperty(ref _validationBorderBrush, value);
        }

        /// <summary>
        /// Validation foreground color
        /// </summary>
        public string ValidationForeground
        {
            get => _validationForeground;
            set => SetProperty(ref _validationForeground, value);
        }

        /// <summary>
        /// Whether validation has details
        /// </summary>
        public bool HasValidationDetails
        {
            get => _hasValidationDetails;
            set => SetProperty(ref _hasValidationDetails, value);
        }

        /// <summary>
        /// Whether JSON is valid
        /// </summary>
        public bool IsJsonValid
        {
            get => _isJsonValid;
            set => SetProperty(ref _isJsonValid, value);
        }

        /// <summary>
        /// Load from file command
        /// </summary>
        public ICommand LoadFromFileCommand { get; }

        /// <summary>
        /// Save to file command
        /// </summary>
        public ICommand SaveToFileCommand { get; }

        /// <summary>
        /// Reset to default command
        /// </summary>
        public ICommand ResetToDefaultCommand { get; }

        /// <summary>
        /// Apply and close command
        /// </summary>
        public ICommand ApplyAndCloseCommand { get; }

        /// <summary>
        /// Cancel command
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Load current configuration from service
        /// </summary>
        private void LoadCurrentConfiguration()
        {
            try
            {
                var config = _configService.GetConfiguration();
                ConfigurationJson = JsonConvert.SerializeObject(config, Formatting.Indented);
                _originalConfigurationJson = ConfigurationJson;
                HasChanges = false;
                
                // Validate the loaded configuration
                ValidateJson();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading current configuration");
                MessageBox.Show($"Error loading current configuration: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load configuration from file
        /// </summary>
        private void LoadFromFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Configuration File",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(openFileDialog.FileName);
                    
                    // Validate JSON
                    JsonConvert.DeserializeObject<AppConfiguration>(json);
                    
                    ConfigurationJson = json;
                    _originalConfigurationJson = json;
                    HasChanges = false;
                    
                    _logger.LogInfo($"Configuration loaded from file: {openFileDialog.FileName}");
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Invalid JSON file: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration from file");
                MessageBox.Show($"Error loading file: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        private void SaveToFile()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Configuration File",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = "appsettings.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Validate JSON before saving
                    JsonConvert.DeserializeObject<AppConfiguration>(ConfigurationJson);
                    
                    File.WriteAllText(saveFileDialog.FileName, ConfigurationJson);
                    _originalConfigurationJson = ConfigurationJson;
                    HasChanges = false;
                    
                    _logger.LogInfo($"Configuration saved to file: {saveFileDialog.FileName}");
                    MessageBox.Show("Configuration saved successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Invalid JSON: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving configuration to file");
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Validate JSON and update validation status
        /// </summary>
        private void ValidateJson()
        {
            if (string.IsNullOrWhiteSpace(ConfigurationJson))
            {
                SetValidationStatus("⚠️", "Empty JSON", "Please enter JSON configuration", 
                    "#FEF3C7", "#F59E0B", "#92400E", false);
                IsJsonValid = false;
                return;
            }

            try
            {
                var config = JsonConvert.DeserializeObject<AppConfiguration>(ConfigurationJson);
                
                // Additional validation for required properties
                var validationErrors = ValidateConfiguration(config);
                
                if (validationErrors.Count == 0)
                {
                    SetValidationStatus("✅", "Valid JSON", "Configuration is valid and ready to save", 
                        "#D1FAE5", "#10B981", "#065F46", false);
                    IsJsonValid = true;
                }
                else
                {
                    var errorDetails = string.Join("\n", validationErrors);
                    SetValidationStatus("⚠️", "Configuration Issues", "JSON is valid but has configuration issues", 
                        "#FEF3C7", "#F59E0B", "#92400E", true);
                    ValidationDetails = errorDetails;
                    IsJsonValid = false;
                }
            }
            catch (JsonException ex)
            {
                SetValidationStatus("❌", "Invalid JSON", "JSON syntax error detected", 
                    "#FEE2E2", "#EF4444", "#991B1B", true);
                ValidationDetails = $"Error: {ex.Message}";
                IsJsonValid = false;
            }
            catch (Exception ex)
            {
                SetValidationStatus("❌", "Validation Error", "Unexpected error during validation", 
                    "#FEE2E2", "#EF4444", "#991B1B", true);
                ValidationDetails = ex.Message;
                IsJsonValid = false;
            }
        }

        /// <summary>
        /// Set validation status properties
        /// </summary>
        private void SetValidationStatus(string icon, string message, string details, 
            string background, string borderBrush, string foreground, bool hasDetails)
        {
            ValidationIcon = icon;
            ValidationMessage = message;
            ValidationDetails = details;
            ValidationBackground = background;
            ValidationBorderBrush = borderBrush;
            ValidationForeground = foreground;
            HasValidationDetails = hasDetails;
        }

        /// <summary>
        /// Validate configuration properties
        /// </summary>
        private List<string> ValidateConfiguration(AppConfiguration config)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add("Configuration is null");
                return errors;
            }

            // Validate CertificateAuthority
            if (config.CertificateAuthority == null)
            {
                errors.Add("CertificateAuthority section is missing");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(config.CertificateAuthority.Server))
                    errors.Add("CertificateAuthority.Server is required");
                if (string.IsNullOrWhiteSpace(config.CertificateAuthority.Template))
                    errors.Add("CertificateAuthority.Template is required");
            }

            // Validate FilePaths
            if (config.FilePaths == null)
            {
                errors.Add("FilePaths section is missing");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(config.FilePaths.CertificateFolder))
                    errors.Add("FilePaths.CertificateFolder is required");
                if (string.IsNullOrWhiteSpace(config.FilePaths.LogPath))
                    errors.Add("FilePaths.LogPath is required");
            }

            // Validate DefaultSettings
            if (config.DefaultSettings == null)
            {
                errors.Add("DefaultSettings section is missing");
            }
            else
            {
                if (config.DefaultSettings.KeyLength <= 0)
                    errors.Add("DefaultSettings.KeyLength must be greater than 0");
                if (string.IsNullOrWhiteSpace(config.DefaultSettings.HashAlgorithm))
                    errors.Add("DefaultSettings.HashAlgorithm is required");
            }

            // Validate Logging
            if (config.Logging == null)
            {
                errors.Add("Logging section is missing");
            }

            return errors;
        }

        /// <summary>
        /// Check if save is possible
        /// </summary>
        private bool CanSave()
        {
            return IsJsonValid && HasChanges;
        }

        /// <summary>
        /// Reset to default configuration
        /// </summary>
        private void ResetToDefault()
        {
            try
            {
                var result = MessageBox.Show(
                    "This will reset the configuration to default values. Are you sure?",
                    "Reset Configuration",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var defaultConfig = GetDefaultConfiguration();
                    ConfigurationJson = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                    _originalConfigurationJson = ConfigurationJson;
                    HasChanges = false;
                    
                    _logger.LogInfo("Configuration reset to default values");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting configuration to default");
                MessageBox.Show($"Error resetting configuration: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Apply changes and close
        /// </summary>
        private void ApplyAndClose()
        {
            try
            {
                // Validate JSON
                var config = JsonConvert.DeserializeObject<AppConfiguration>(ConfigurationJson);
                
                // Save to appsettings.json
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                File.WriteAllText(configPath, ConfigurationJson);
                
                _logger.LogInfo("Configuration updated successfully");
                
                // Show restart notification
                var result = MessageBox.Show(
                    "Configuration has been updated successfully!\n\n" +
                    "The application needs to be restarted for changes to take effect.\n\n" +
                    "Would you like to restart the application now?",
                    "Configuration Updated",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    // Close the editor window
                    Application.Current.MainWindow?.Close();
                }
                else
                {
                    // Just close the editor
                    var window = Application.Current.Windows.OfType<ConfigurationEditorView>().FirstOrDefault();
                    window?.Close();
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Invalid JSON: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying configuration changes");
                MessageBox.Show($"Error applying changes: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Check if apply is possible
        /// </summary>
        private bool CanApply()
        {
            return IsJsonValid && HasChanges;
        }

        /// <summary>
        /// Cancel changes
        /// </summary>
        private void Cancel()
        {
            if (HasChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to cancel?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            var window = Application.Current.Windows.OfType<ConfigurationEditorView>().FirstOrDefault();
            window?.Close();
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
                    AutoDetect = true,
                    CommonPaths = new List<string>
                    {
                        "C:\\Program Files\\OpenSSL-Win64\\bin\\openssl.exe",
                        "C:\\Program Files (x86)\\OpenSSL-Win32\\bin\\openssl.exe",
                        "C:\\OpenSSL-Win64\\bin\\openssl.exe",
                        "C:\\OpenSSL-Win32\\bin\\openssl.exe"
                    }
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

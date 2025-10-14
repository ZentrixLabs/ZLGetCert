using System;
using System.Collections.Generic;
using System.ComponentModel;
using ZLGetCert.Enums;

namespace ZLGetCert.Models
{
    /// <summary>
    /// Application configuration model
    /// </summary>
    public class AppConfiguration : INotifyPropertyChanged
    {
        private CertificateAuthorityConfig _certificateAuthority;
        private FilePathsConfig _filePaths;
        private OpenSSLConfig _openSSL;
        private DefaultSettingsConfig _defaultSettings;
        private CertificateParametersConfig _certificateParameters;
        private LoggingConfig _logging;

        public AppConfiguration()
        {
            _certificateAuthority = new CertificateAuthorityConfig();
            _filePaths = new FilePathsConfig();
            _openSSL = new OpenSSLConfig();
            _defaultSettings = new DefaultSettingsConfig();
            _certificateParameters = new CertificateParametersConfig();
            _logging = new LoggingConfig();
        }

        /// <summary>
        /// Certificate Authority configuration
        /// </summary>
        public CertificateAuthorityConfig CertificateAuthority
        {
            get => _certificateAuthority;
            set
            {
                _certificateAuthority = value;
                OnPropertyChanged(nameof(CertificateAuthority));
            }
        }

        /// <summary>
        /// File paths configuration
        /// </summary>
        public FilePathsConfig FilePaths
        {
            get => _filePaths;
            set
            {
                _filePaths = value;
                OnPropertyChanged(nameof(FilePaths));
            }
        }

        /// <summary>
        /// OpenSSL configuration
        /// </summary>
        public OpenSSLConfig OpenSSL
        {
            get => _openSSL;
            set
            {
                _openSSL = value;
                OnPropertyChanged(nameof(OpenSSL));
            }
        }

        /// <summary>
        /// Default settings configuration
        /// </summary>
        public DefaultSettingsConfig DefaultSettings
        {
            get => _defaultSettings;
            set
            {
                _defaultSettings = value;
                OnPropertyChanged(nameof(DefaultSettings));
            }
        }

        /// <summary>
        /// Certificate parameters configuration
        /// </summary>
        public CertificateParametersConfig CertificateParameters
        {
            get => _certificateParameters;
            set
            {
                _certificateParameters = value;
                OnPropertyChanged(nameof(CertificateParameters));
            }
        }

        /// <summary>
        /// Logging configuration
        /// </summary>
        public LoggingConfig Logging
        {
            get => _logging;
            set
            {
                _logging = value;
                OnPropertyChanged(nameof(Logging));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Certificate Authority configuration
    /// </summary>
    public class CertificateAuthorityConfig : INotifyPropertyChanged
    {
        private string _server;
        private string _template;
        private string _defaultCompany;
        private string _defaultOU;
        private string _defaultLocation;
        private string _defaultState;
        private List<CertificateTemplate> _availableTemplates;

        public CertificateAuthorityConfig()
        {
            _availableTemplates = new List<CertificateTemplate>();
        }

        /// <summary>
        /// CA server name
        /// </summary>
        public string Server
        {
            get => _server;
            set
            {
                _server = value;
                OnPropertyChanged(nameof(Server));
            }
        }

        /// <summary>
        /// Certificate template name
        /// </summary>
        public string Template
        {
            get => _template;
            set
            {
                _template = value;
                OnPropertyChanged(nameof(Template));
            }
        }

        /// <summary>
        /// Default company name
        /// </summary>
        public string DefaultCompany
        {
            get => _defaultCompany;
            set
            {
                _defaultCompany = value;
                OnPropertyChanged(nameof(DefaultCompany));
            }
        }

        /// <summary>
        /// Default organizational unit
        /// </summary>
        public string DefaultOU
        {
            get => _defaultOU;
            set
            {
                _defaultOU = value;
                OnPropertyChanged(nameof(DefaultOU));
            }
        }

        /// <summary>
        /// Default location (city)
        /// </summary>
        public string DefaultLocation
        {
            get => _defaultLocation;
            set
            {
                _defaultLocation = value;
                OnPropertyChanged(nameof(DefaultLocation));
            }
        }

        /// <summary>
        /// Default state (2-letter code)
        /// </summary>
        public string DefaultState
        {
            get => _defaultState;
            set
            {
                _defaultState = value;
                OnPropertyChanged(nameof(DefaultState));
            }
        }

        /// <summary>
        /// Available certificate templates from CA (not persisted to config file)
        /// </summary>
        public List<CertificateTemplate> AvailableTemplates
        {
            get => _availableTemplates;
            set
            {
                _availableTemplates = value;
                OnPropertyChanged(nameof(AvailableTemplates));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// File paths configuration
    /// </summary>
    public class FilePathsConfig : INotifyPropertyChanged
    {
        private string _certificateFolder;
        private string _logPath;
        private string _tempPath;

        /// <summary>
        /// Certificate output folder
        /// </summary>
        public string CertificateFolder
        {
            get => _certificateFolder;
            set
            {
                _certificateFolder = value;
                OnPropertyChanged(nameof(CertificateFolder));
            }
        }

        /// <summary>
        /// Log file path
        /// </summary>
        public string LogPath
        {
            get => _logPath;
            set
            {
                _logPath = value;
                OnPropertyChanged(nameof(LogPath));
            }
        }

        /// <summary>
        /// Temporary files path
        /// </summary>
        public string TempPath
        {
            get => _tempPath;
            set
            {
                _tempPath = value;
                OnPropertyChanged(nameof(TempPath));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Default settings configuration
    /// </summary>
    public class DefaultSettingsConfig : INotifyPropertyChanged
    {
        private int _keyLength;
        private string _hashAlgorithm;
        private string _defaultPassword;
        private bool _requirePasswordConfirmation;
        private bool _autoCleanup;
        private bool _rememberPassword;
        private List<string> _availableHashAlgorithms;

        /// <summary>
        /// Default key length
        /// </summary>
        public int KeyLength
        {
            get => _keyLength;
            set
            {
                _keyLength = value;
                OnPropertyChanged(nameof(KeyLength));
            }
        }

        /// <summary>
        /// Default hash algorithm
        /// </summary>
        public string HashAlgorithm
        {
            get => _hashAlgorithm;
            set
            {
                _hashAlgorithm = value;
                OnPropertyChanged(nameof(HashAlgorithm));
            }
        }

        /// <summary>
        /// Default password - DEPRECATED for security reasons
        /// Passwords must be entered at runtime and are no longer stored in configuration
        /// </summary>
        [Obsolete("DefaultPassword is deprecated for security reasons. Passwords must be entered at runtime.")]
        public string DefaultPassword
        {
            get => string.Empty; // Always return empty for security
            set
            {
                // Log warning if someone tries to set a password
                if (!string.IsNullOrEmpty(value))
                {
                    System.Diagnostics.Debug.WriteLine(
                        "WARNING: DefaultPassword in configuration is deprecated and ignored for security reasons. " +
                        "Passwords must be entered at runtime.");
                }
                // Don't store the value - always keep it empty
                _defaultPassword = string.Empty;
                OnPropertyChanged(nameof(DefaultPassword));
            }
        }

        /// <summary>
        /// Whether to require password confirmation
        /// </summary>
        public bool RequirePasswordConfirmation
        {
            get => _requirePasswordConfirmation;
            set
            {
                _requirePasswordConfirmation = value;
                OnPropertyChanged(nameof(RequirePasswordConfirmation));
            }
        }

        /// <summary>
        /// Whether to auto-cleanup temporary files
        /// </summary>
        public bool AutoCleanup
        {
            get => _autoCleanup;
            set
            {
                _autoCleanup = value;
                OnPropertyChanged(nameof(AutoCleanup));
            }
        }

        /// <summary>
        /// Whether to remember password in session
        /// </summary>
        public bool RememberPassword
        {
            get => _rememberPassword;
            set
            {
                _rememberPassword = value;
                OnPropertyChanged(nameof(RememberPassword));
            }
        }

        /// <summary>
        /// Available hash algorithms
        /// </summary>
        public List<string> AvailableHashAlgorithms
        {
            get => _availableHashAlgorithms;
            set
            {
                _availableHashAlgorithms = value;
                OnPropertyChanged(nameof(AvailableHashAlgorithms));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Logging configuration
    /// </summary>
    public class LoggingConfig : INotifyPropertyChanged
    {
        private Enums.LogLevel _logLevel;
        private bool _logToFile;
        private bool _logToConsole;
        private string _maxLogFileSize;
        private int _maxLogFiles;
        private List<Enums.LogLevel> _availableLogLevels;

        /// <summary>
        /// Minimum log level
        /// </summary>
        public Enums.LogLevel LogLevel
        {
            get => _logLevel;
            set
            {
                _logLevel = value;
                OnPropertyChanged(nameof(LogLevel));
            }
        }

        /// <summary>
        /// Whether to log to file
        /// </summary>
        public bool LogToFile
        {
            get => _logToFile;
            set
            {
                _logToFile = value;
                OnPropertyChanged(nameof(LogToFile));
            }
        }

        /// <summary>
        /// Whether to log to console
        /// </summary>
        public bool LogToConsole
        {
            get => _logToConsole;
            set
            {
                _logToConsole = value;
                OnPropertyChanged(nameof(LogToConsole));
            }
        }

        /// <summary>
        /// Maximum log file size
        /// </summary>
        public string MaxLogFileSize
        {
            get => _maxLogFileSize;
            set
            {
                _maxLogFileSize = value;
                OnPropertyChanged(nameof(MaxLogFileSize));
            }
        }

        /// <summary>
        /// Maximum number of log files to keep
        /// </summary>
        public int MaxLogFiles
        {
            get => _maxLogFiles;
            set
            {
                _maxLogFiles = value;
                OnPropertyChanged(nameof(MaxLogFiles));
            }
        }

        /// <summary>
        /// Available log levels
        /// </summary>
        public List<Enums.LogLevel> AvailableLogLevels
        {
            get => _availableLogLevels;
            set
            {
                _availableLogLevels = value;
                OnPropertyChanged(nameof(AvailableLogLevels));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Certificate parameters configuration
    /// </summary>
    public class CertificateParametersConfig : INotifyPropertyChanged
    {
        private int _keySpec;
        private string _providerName;
        private int _providerType;
        private string _keyUsage;
        private List<string> _enhancedKeyUsageOIDs;
        private bool _exportable;
        private bool _machineKeySet;
        private bool _smime;
        private bool _privateKeyArchive;
        private bool _userProtected;
        private bool _useExistingKeySet;

        public CertificateParametersConfig()
        {
            // Default values for web server certificates
            _keySpec = 1; // RSA
            _providerName = "Microsoft RSA SChannel Cryptographic Provider";
            _providerType = 12; // RSA
            _keyUsage = "0xa0"; // Digital Signature + Key Encipherment
            _enhancedKeyUsageOIDs = new List<string> { "1.3.6.1.5.5.7.3.1" }; // Server Authentication
            _exportable = true;
            _machineKeySet = true;
            _smime = false;
            _privateKeyArchive = false;
            _userProtected = false;
            _useExistingKeySet = false;
        }

        /// <summary>
        /// Key specification (1=RSA, 2=DH, 3=DSS)
        /// </summary>
        public int KeySpec
        {
            get => _keySpec;
            set
            {
                _keySpec = value;
                OnPropertyChanged(nameof(KeySpec));
            }
        }

        /// <summary>
        /// Cryptographic provider name
        /// </summary>
        public string ProviderName
        {
            get => _providerName;
            set
            {
                _providerName = value;
                OnPropertyChanged(nameof(ProviderName));
            }
        }

        /// <summary>
        /// Cryptographic provider type
        /// </summary>
        public int ProviderType
        {
            get => _providerType;
            set
            {
                _providerType = value;
                OnPropertyChanged(nameof(ProviderType));
            }
        }

        /// <summary>
        /// Key usage flags (hex string)
        /// </summary>
        public string KeyUsage
        {
            get => _keyUsage;
            set
            {
                _keyUsage = value;
                OnPropertyChanged(nameof(KeyUsage));
            }
        }

        /// <summary>
        /// Enhanced key usage OIDs
        /// </summary>
        public List<string> EnhancedKeyUsageOIDs
        {
            get => _enhancedKeyUsageOIDs;
            set
            {
                _enhancedKeyUsageOIDs = value;
                OnPropertyChanged(nameof(EnhancedKeyUsageOIDs));
            }
        }

        /// <summary>
        /// Whether private key is exportable
        /// </summary>
        public bool Exportable
        {
            get => _exportable;
            set
            {
                _exportable = value;
                OnPropertyChanged(nameof(Exportable));
            }
        }

        /// <summary>
        /// Whether to use machine key set
        /// </summary>
        public bool MachineKeySet
        {
            get => _machineKeySet;
            set
            {
                _machineKeySet = value;
                OnPropertyChanged(nameof(MachineKeySet));
            }
        }

        /// <summary>
        /// Whether this is an S/MIME certificate
        /// </summary>
        public bool SMIME
        {
            get => _smime;
            set
            {
                _smime = value;
                OnPropertyChanged(nameof(SMIME));
            }
        }

        /// <summary>
        /// Whether to archive private key
        /// </summary>
        public bool PrivateKeyArchive
        {
            get => _privateKeyArchive;
            set
            {
                _privateKeyArchive = value;
                OnPropertyChanged(nameof(PrivateKeyArchive));
            }
        }

        /// <summary>
        /// Whether private key is user protected
        /// </summary>
        public bool UserProtected
        {
            get => _userProtected;
            set
            {
                _userProtected = value;
                OnPropertyChanged(nameof(UserProtected));
            }
        }

        /// <summary>
        /// Whether to use existing key set
        /// </summary>
        public bool UseExistingKeySet
        {
            get => _useExistingKeySet;
            set
            {
                _useExistingKeySet = value;
                OnPropertyChanged(nameof(UseExistingKeySet));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

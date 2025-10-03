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
        private LoggingConfig _logging;

        public AppConfiguration()
        {
            _certificateAuthority = new CertificateAuthorityConfig();
            _filePaths = new FilePathsConfig();
            _openSSL = new OpenSSLConfig();
            _defaultSettings = new DefaultSettingsConfig();
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
        /// Default password (not secure, use for development only)
        /// </summary>
        public string DefaultPassword
        {
            get => _defaultPassword;
            set
            {
                _defaultPassword = value;
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
}

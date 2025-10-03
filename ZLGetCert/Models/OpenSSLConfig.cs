using System.ComponentModel;

namespace ZLGetCert.Models
{
    /// <summary>
    /// Configuration for OpenSSL integration
    /// </summary>
    public class OpenSSLConfig : INotifyPropertyChanged
    {
        private string _executablePath;
        private bool _autoDetect;
        private bool _isAvailable;
        private string _version;

        /// <summary>
        /// Path to OpenSSL executable
        /// </summary>
        public string ExecutablePath
        {
            get => _executablePath;
            set
            {
                _executablePath = value;
                OnPropertyChanged(nameof(ExecutablePath));
            }
        }

        /// <summary>
        /// Whether to auto-detect OpenSSL installation
        /// </summary>
        public bool AutoDetect
        {
            get => _autoDetect;
            set
            {
                _autoDetect = value;
                OnPropertyChanged(nameof(AutoDetect));
            }
        }

        /// <summary>
        /// Whether OpenSSL is available and working
        /// </summary>
        public bool IsAvailable
        {
            get => _isAvailable;
            set
            {
                _isAvailable = value;
                OnPropertyChanged(nameof(IsAvailable));
            }
        }

        /// <summary>
        /// OpenSSL version string
        /// </summary>
        public string Version
        {
            get => _version;
            set
            {
                _version = value;
                OnPropertyChanged(nameof(Version));
            }
        }

        /// <summary>
        /// Display name for OpenSSL status
        /// </summary>
        public string DisplayStatus
        {
            get
            {
                if (IsAvailable)
                    return $"OpenSSL {Version} - {ExecutablePath}";
                return "OpenSSL not available";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

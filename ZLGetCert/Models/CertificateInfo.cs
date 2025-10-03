using System;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;

namespace ZLGetCert.Models
{
    /// <summary>
    /// Represents information about a generated certificate
    /// </summary>
    public class CertificateInfo : INotifyPropertyChanged
    {
        private string _thumbprint;
        private string _subject;
        private string _issuer;
        private DateTime _notBefore;
        private DateTime _notAfter;
        private string _serialNumber;
        private string _friendlyName;
        private string _pfxPath;
        private string _cerPath;
        private string _pemPath;
        private string _keyPath;
        private bool _isValid;
        private string _errorMessage;

        /// <summary>
        /// Certificate thumbprint
        /// </summary>
        public string Thumbprint
        {
            get => _thumbprint;
            set
            {
                _thumbprint = value;
                OnPropertyChanged(nameof(Thumbprint));
            }
        }

        /// <summary>
        /// Certificate subject
        /// </summary>
        public string Subject
        {
            get => _subject;
            set
            {
                _subject = value;
                OnPropertyChanged(nameof(Subject));
            }
        }

        /// <summary>
        /// Certificate issuer
        /// </summary>
        public string Issuer
        {
            get => _issuer;
            set
            {
                _issuer = value;
                OnPropertyChanged(nameof(Issuer));
            }
        }

        /// <summary>
        /// Certificate valid from date
        /// </summary>
        public DateTime NotBefore
        {
            get => _notBefore;
            set
            {
                _notBefore = value;
                OnPropertyChanged(nameof(NotBefore));
            }
        }

        /// <summary>
        /// Certificate valid until date
        /// </summary>
        public DateTime NotAfter
        {
            get => _notAfter;
            set
            {
                _notAfter = value;
                OnPropertyChanged(nameof(NotAfter));
            }
        }

        /// <summary>
        /// Certificate serial number
        /// </summary>
        public string SerialNumber
        {
            get => _serialNumber;
            set
            {
                _serialNumber = value;
                OnPropertyChanged(nameof(SerialNumber));
            }
        }

        /// <summary>
        /// Certificate friendly name
        /// </summary>
        public string FriendlyName
        {
            get => _friendlyName;
            set
            {
                _friendlyName = value;
                OnPropertyChanged(nameof(FriendlyName));
            }
        }

        /// <summary>
        /// Path to PFX file
        /// </summary>
        public string PfxPath
        {
            get => _pfxPath;
            set
            {
                _pfxPath = value;
                OnPropertyChanged(nameof(PfxPath));
            }
        }

        /// <summary>
        /// Path to CER file
        /// </summary>
        public string CerPath
        {
            get => _cerPath;
            set
            {
                _cerPath = value;
                OnPropertyChanged(nameof(CerPath));
            }
        }

        /// <summary>
        /// Path to PEM file (if extracted)
        /// </summary>
        public string PemPath
        {
            get => _pemPath;
            set
            {
                _pemPath = value;
                OnPropertyChanged(nameof(PemPath));
            }
        }

        /// <summary>
        /// Path to KEY file (if extracted)
        /// </summary>
        public string KeyPath
        {
            get => _keyPath;
            set
            {
                _keyPath = value;
                OnPropertyChanged(nameof(KeyPath));
            }
        }

        /// <summary>
        /// Whether the certificate is valid
        /// </summary>
        public bool IsValid
        {
            get => _isValid;
            set
            {
                _isValid = value;
                OnPropertyChanged(nameof(IsValid));
            }
        }

        /// <summary>
        /// Error message if certificate is invalid
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        /// <summary>
        /// Days until certificate expires
        /// </summary>
        public int DaysUntilExpiry
        {
            get
            {
                if (NotAfter == DateTime.MinValue)
                    return -1;
                
                return (int)(NotAfter - DateTime.Now).TotalDays;
            }
        }

        /// <summary>
        /// Whether certificate is expired
        /// </summary>
        public bool IsExpired => NotAfter < DateTime.Now;

        /// <summary>
        /// Whether certificate expires within 30 days
        /// </summary>
        public bool ExpiresSoon => DaysUntilExpiry <= 30 && DaysUntilExpiry > 0;

        /// <summary>
        /// Create CertificateInfo from X509Certificate2
        /// </summary>
        public static CertificateInfo FromX509Certificate(X509Certificate2 cert)
        {
            if (cert == null)
                return null;

            return new CertificateInfo
            {
                Thumbprint = cert.Thumbprint,
                Subject = cert.Subject,
                Issuer = cert.Issuer,
                NotBefore = cert.NotBefore,
                NotAfter = cert.NotAfter,
                SerialNumber = cert.SerialNumber,
                FriendlyName = cert.FriendlyName,
                IsValid = true
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

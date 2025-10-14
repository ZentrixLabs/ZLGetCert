using System;
using System.ComponentModel;

namespace ZLGetCert.Models
{
    /// <summary>
    /// Represents a certificate template available on the CA
    /// </summary>
    public class CertificateTemplate : INotifyPropertyChanged
    {
        private string _name;
        private string _displayName;
        private string _oid;
        private int _version;

        /// <summary>
        /// Template name (used in certificate requests)
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// Display name for UI
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary>
        /// Template OID (Object Identifier)
        /// </summary>
        public string OID
        {
            get => _oid;
            set
            {
                _oid = value;
                OnPropertyChanged(nameof(OID));
            }
        }

        /// <summary>
        /// Template version
        /// </summary>
        public int Version
        {
            get => _version;
            set
            {
                _version = value;
                OnPropertyChanged(nameof(Version));
            }
        }

        /// <summary>
        /// Formatted display string for ComboBox
        /// </summary>
        public string DisplayText
        {
            get
            {
                if (!string.IsNullOrEmpty(DisplayName) && DisplayName != Name)
                {
                    return $"{DisplayName} ({Name})";
                }
                return Name;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}


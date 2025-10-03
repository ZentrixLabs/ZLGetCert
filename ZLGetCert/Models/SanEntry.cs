using System.ComponentModel;

namespace ZLGetCert.Models
{
    /// <summary>
    /// Represents a Subject Alternative Name (SAN) entry
    /// </summary>
    public class SanEntry : INotifyPropertyChanged
    {
        private string _value;
        private SanType _type;

        /// <summary>
        /// The value of the SAN entry (DNS name or IP address)
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        /// <summary>
        /// The type of SAN entry (DNS or IP)
        /// </summary>
        public SanType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        /// <summary>
        /// Display name for the SAN entry
        /// </summary>
        public string DisplayName => $"{Type}: {Value}";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    /// <summary>
    /// Types of SAN entries
    /// </summary>
    public enum SanType
    {
        /// <summary>
        /// DNS name SAN
        /// </summary>
        DNS,

        /// <summary>
        /// IP address SAN
        /// </summary>
        IP
    }
}

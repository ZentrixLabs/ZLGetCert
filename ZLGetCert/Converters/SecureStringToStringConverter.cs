using System;
using System.Globalization;
using System.Security;
using System.Windows.Data;
using ZLGetCert.Utilities;

namespace ZLGetCert.Converters
{
    /// <summary>
    /// Converts SecureString to plain string for display purposes
    /// WARNING: This reduces security and should only be used when password visibility is toggled
    /// </summary>
    public class SecureStringToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SecureString secureString)
            {
                return SecureStringHelper.SecureStringToString(secureString);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string plainString)
            {
                return SecureStringHelper.StringToSecureString(plainString);
            }
            return null;
        }
    }
}


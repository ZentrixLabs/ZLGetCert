using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ZLGetCert.Utilities;

namespace ZLGetCert.Converters
{
    /// <summary>
    /// Converts password strength to visual representation
    /// </summary>
    public class PasswordStrengthToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PasswordStrength strength)
            {
                switch (strength)
                {
                    case PasswordStrength.Empty:
                        return 0;
                    case PasswordStrength.Weak:
                        return 33;
                    case PasswordStrength.Medium:
                        return 66;
                    case PasswordStrength.Strong:
                        return 100;
                }
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts password strength to color
    /// </summary>
    public class PasswordStrengthToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PasswordStrength strength)
            {
                switch (strength)
                {
                    case PasswordStrength.Empty:
                        return new SolidColorBrush(Colors.LightGray);
                    case PasswordStrength.Weak:
                        return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
                    case PasswordStrength.Medium:
                        return new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Orange/Yellow
                    case PasswordStrength.Strong:
                        return new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green
                }
            }
            return new SolidColorBrush(Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts password strength to text
    /// </summary>
    public class PasswordStrengthToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PasswordStrength strength)
            {
                switch (strength)
                {
                    case PasswordStrength.Empty:
                        return "No password";
                    case PasswordStrength.Weak:
                        return "Weak";
                    case PasswordStrength.Medium:
                        return "Medium";
                    case PasswordStrength.Strong:
                        return "Strong";
                }
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


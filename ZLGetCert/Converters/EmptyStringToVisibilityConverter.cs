using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ZLGetCert.Converters
{
    /// <summary>
    /// Converts an empty string to Collapsed, non-empty string to Visible
    /// Used to show validation error messages only when they exist
    /// </summary>
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                // Show if string is NOT empty
                return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverse version - shows when string is empty, hides when string has content
    /// Used to show helper text when there's no error
    /// </summary>
    public class InverseEmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                // Show if string IS empty
                return string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an empty string to true, non-empty string to false
    /// Used for DataTriggers to detect errors
    /// </summary>
    public class EmptyStringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                // True if empty (no error), false if has content (has error)
                return string.IsNullOrEmpty(str);
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


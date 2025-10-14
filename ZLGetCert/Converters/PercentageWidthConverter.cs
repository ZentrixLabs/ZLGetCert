using System;
using System.Globalization;
using System.Windows.Data;

namespace ZLGetCert.Converters
{
    /// <summary>
    /// Converts a percentage value to an actual width based on container width
    /// Used for the password strength meter
    /// </summary>
    public class PercentageWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return 0.0;

            // values[0] = container width
            // values[1] = percentage (0-100)
            if (values[0] is double containerWidth && values[1] is double percentage)
            {
                return containerWidth * (percentage / 100.0);
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


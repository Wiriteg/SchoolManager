using System;
using System.Globalization;
using System.Windows.Data;

namespace SchoolManager.Converters
{
    public class TimeRangeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || values[0] is not string startTime || values[1] is not string endTime)
                return string.Empty;

            return $"{startTime}-{endTime}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
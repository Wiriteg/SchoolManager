using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace SchoolManager.Converters
{
    public class ClassNameToUserControlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string className)
            {
                var userControl = parameter as UserControl;
                return new Tuple<string, UserControl>(className, userControl);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
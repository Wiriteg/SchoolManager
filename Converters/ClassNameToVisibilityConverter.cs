using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SchoolManager.Converters
{
    public class ClassNameToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || values[0] is not string className || values[1] is not Dictionary<string, bool> status)
            {
                System.Diagnostics.Debug.WriteLine("ClassNameToVisibilityConverter: Invalid input values");
                return Visibility.Collapsed;
            }

            bool isSaved = status.TryGetValue(className, out bool saved) && saved;
            System.Diagnostics.Debug.WriteLine($"ClassNameToVisibilityConverter: Class={className}, IsSaved={isSaved}");
            return isSaved ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SchoolManager
{
    public class RoleToAdminVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string role = value as string;
            return role == "Администратор" ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
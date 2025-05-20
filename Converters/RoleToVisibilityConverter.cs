using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SchoolManager.Converters
{
    public class RoleToVisibilityConverter : IValueConverter
    {
        private static readonly string[] AdminRoles = { "Администратор", "Директор", "Заместитель директора по УВР", "Заместитель директора по ВР" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string role = value as string;
            System.Diagnostics.Debug.WriteLine($"RoleToVisibilityConverter: Role = {role ?? "null"}");
            if (string.IsNullOrEmpty(role) || Array.IndexOf(AdminRoles, role) < 0)
            {
                System.Diagnostics.Debug.WriteLine("Returning Visibility.Collapsed");
                return Visibility.Collapsed;
            }
            System.Diagnostics.Debug.WriteLine("Returning Visibility.Visible");
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
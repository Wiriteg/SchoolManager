using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SchoolManager.Models;

namespace SchoolManager.Converters
{
    public class ClassNameToVisibilityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 ||
                values[0] is not Dictionary<string, ObservableCollection<ScheduleItem>> schedules ||
                values[1] is not string className)
            {
                return Visibility.Collapsed;
            }

            if (schedules.TryGetValue(className, out var classSchedules) && classSchedules != null)
            {
                bool isAddButton = parameter?.ToString() == "Add";
                if (isAddButton)
                {
                    return classSchedules.Count < 8 ? Visibility.Visible : Visibility.Collapsed;
                }
                return classSchedules.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using SchoolManager.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace SchoolManager.Converters
{
    public class ClassNameToScheduleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return new ObservableCollection<ScheduleItem>();

            if (values[0] is string className &&
                values[1] is Dictionary<string, ObservableCollection<ScheduleItem>> classSchedules)
            {
                return classSchedules.TryGetValue(className, out var schedule) ? schedule : new ObservableCollection<ScheduleItem>();
            }
            return new ObservableCollection<ScheduleItem>();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
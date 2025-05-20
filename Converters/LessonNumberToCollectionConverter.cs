using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace SchoolManager.Converters
{
    public class LessonNumberToCollectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || values[0] is not Dictionary<int, ObservableCollection<string>> collections || values[1] is not int lessonNumber)
            {
                return new ObservableCollection<string>();
            }

            if (collections.TryGetValue(lessonNumber, out var collection))
            {
                return collection;
            }

            return new ObservableCollection<string>();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
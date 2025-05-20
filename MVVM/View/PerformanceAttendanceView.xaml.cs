using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using System.Windows.Input;
using SchoolManager.MVVM.ViewModel;
using SchoolManager.Model;
using System.Linq;
using System.Windows.Media;
using System.Collections.Generic;

namespace SchoolManager.MVVM.View
{
    public partial class PerformanceAttendanceView : UserControl
    {
        private readonly PerformanceAttendanceViewModel _viewModel;
        private Student _currentStudent;
        private DateTime _currentDate;
        private Attendance _currentAttendance;

        public PerformanceAttendanceView(MainViewModel mainViewModel)
        {
            InitializeComponent();
            _viewModel = new PerformanceAttendanceViewModel(mainViewModel);
            DataContext = _viewModel;

            _viewModel.PropertyChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"PropertyChanged: {e.PropertyName}");
                if (e.PropertyName == nameof(_viewModel.SelectedQuarter) ||
                    e.PropertyName == nameof(_viewModel.SelectedClass) ||
                    e.PropertyName == nameof(_viewModel.SelectedAcademicYear) ||
                    e.PropertyName == nameof(_viewModel.SelectedSubject) ||
                    e.PropertyName == nameof(_viewModel.Students))
                {
                    UpdateColumns();
                }
            };

            PerformanceAttendanceDataGrid.MouseDoubleClick += PerformanceAttendanceDataGrid_MouseDoubleClick;
        }

        private void UpdateColumns()
        {
            PerformanceAttendanceDataGrid.Columns.Clear();
            if (_viewModel.Students == null)
            {
                System.Diagnostics.Debug.WriteLine("Students collection is null.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Students count in UpdateColumns: {_viewModel.Students.Count}");
            PerformanceAttendanceDataGrid.ItemsSource = _viewModel.Students;

            var studentColumn = new DataGridTextColumn
            {
                Header = "Ученик",
                Width = new DataGridLength(120),
                Binding = new Binding("FullName"),
                IsReadOnly = true
            };
            studentColumn.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap),
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center),
                    new Setter(TextBlock.FontSizeProperty, 14.0)
                }
            };
            PerformanceAttendanceDataGrid.Columns.Add(studentColumn);

            var currentDate = _viewModel.StartDate;
            while (currentDate <= _viewModel.EndDate)
            {
                if (currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    var date = currentDate;
                    var column = new DataGridTextColumn
                    {
                        Header = date.ToString("dd.MM"),
                        Width = new DataGridLength(60),
                        Binding = new Binding($"Attendances[{_viewModel.SelectedSubject?.Id}][{date:yyyy-MM-dd}]")
                        {
                            Converter = new AttendanceConverter()
                        }
                    };
                    column.ElementStyle = new Style(typeof(TextBlock))
                    {
                        Setters =
                        {
                            new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                            new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center),
                            new Setter(TextBlock.FontSizeProperty, 14.0)
                        }
                    };
                    PerformanceAttendanceDataGrid.Columns.Add(column);
                }
                currentDate = currentDate.AddDays(1);
            }

            var totalAbsencesColumn = new DataGridTextColumn
            {
                Header = "Пропуски",
                Width = new DataGridLength(90),
                Binding = new Binding(".")
                {
                    Converter = new TotalAbsencesCountConverter(),
                    ConverterParameter = _viewModel
                },
                IsReadOnly = true
            };
            totalAbsencesColumn.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center),
                    new Setter(TextBlock.FontSizeProperty, 14.0)
                }
            };
            PerformanceAttendanceDataGrid.Columns.Add(totalAbsencesColumn);

            var unexcusedAbsencesColumn = new DataGridTextColumn
            {
                Header = "Н",
                Width = new DataGridLength(50),
                Binding = new Binding(".")
                {
                    Converter = new AbsencesCountConverter(),
                    ConverterParameter = _viewModel
                },
                IsReadOnly = true
            };
            unexcusedAbsencesColumn.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center),
                    new Setter(TextBlock.FontSizeProperty, 14.0)
                }
            };
            PerformanceAttendanceDataGrid.Columns.Add(unexcusedAbsencesColumn);

            var excusedAbsencesColumn = new DataGridTextColumn
            {
                Header = "У",
                Width = new DataGridLength(50),
                Binding = new Binding(".")
                {
                    Converter = new RespectfulAbsencesCountConverter(),
                    ConverterParameter = _viewModel
                },
                IsReadOnly = true
            };
            excusedAbsencesColumn.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center),
                    new Setter(TextBlock.FontSizeProperty, 14.0)
                }
            };
            PerformanceAttendanceDataGrid.Columns.Add(excusedAbsencesColumn);

            var averageGradeColumn = new DataGridTextColumn
            {
                Header = "Средняя оценка",
                Width = new DataGridLength(120),
                Binding = new Binding(".")
                {
                    Converter = new AverageGradeConverter(),
                    ConverterParameter = _viewModel
                },
                IsReadOnly = true
            };
            averageGradeColumn.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                    new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center),
                    new Setter(TextBlock.FontSizeProperty, 14.0)
                }
            };
            PerformanceAttendanceDataGrid.Columns.Add(averageGradeColumn);
        }

        private void PerformanceAttendanceDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MouseDoubleClick triggered.");

            if (PerformanceAttendanceDataGrid.SelectedCells.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No selected cells.");
                return;
            }

            var selectedCell = PerformanceAttendanceDataGrid.SelectedCells[0];
            if (!(selectedCell.Column is DataGridTextColumn column))
            {
                System.Diagnostics.Debug.WriteLine("Selected column is not a DataGridTextColumn.");
                return;
            }
            if (!(selectedCell.Item is Student student))
            {
                System.Diagnostics.Debug.WriteLine("Selected item is not a Student.");
                return;
            }

            var header = column.Header?.ToString();
            if (string.IsNullOrEmpty(header) || !DateTime.TryParseExact(header, "dd.MM", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                System.Diagnostics.Debug.WriteLine($"Invalid header or date: {header}");
                return;
            }

            if (column.DisplayIndex == 0)
            {
                System.Diagnostics.Debug.WriteLine("Double-click on 'Ученик' column, ignoring.");
                return;
            }

            _currentStudent = student;
            _currentDate = new DateTime(_viewModel.StartDate.Year, date.Month, date.Day);

            System.Diagnostics.Debug.WriteLine($"Selected student: {student.FullName}, Date: {_currentDate:yyyy-MM-dd}");

            PresenceComboBox.SelectedItem = null;
            ReasonComboBox.SelectedItem = null;
            GradeComboBox.SelectedItem = null;

            if (_viewModel.SelectedSubject == null || student.Attendances == null ||
                !student.Attendances.ContainsKey(_viewModel.SelectedSubject.Id) ||
                !student.Attendances[_viewModel.SelectedSubject.Id].TryGetValue(_currentDate.ToString("yyyy-MM-dd"), out var attendance))
            {
                System.Diagnostics.Debug.WriteLine($"Attendance not found for student ID={student.Id}, Subject ID={_viewModel.SelectedSubject?.Id}, Date={_currentDate:yyyy-MM-dd}.");
                EditPopup.IsOpen = true;
                PresenceComboBox_SelectionChanged(null, null);
                return;
            }
            _currentAttendance = attendance;

            System.Diagnostics.Debug.WriteLine($"Found attendance: Presence={_currentAttendance.Presence}, Reason={_currentAttendance.Reason}");

            PresenceComboBox.SelectedItem = PresenceComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == _currentAttendance.Presence);

            ReasonComboBox.SelectedItem = ReasonComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == _currentAttendance.Reason);

            var grade = "";
            if (_viewModel.SelectedSubject != null &&
                student.Performances.TryGetValue(_viewModel.SelectedSubject.Id, out var performances) &&
                performances.ContainsKey(_currentDate.ToString("yyyy-MM-dd")))
            {
                grade = performances[_currentDate.ToString("yyyy-MM-dd")];
            }
            GradeComboBox.SelectedItem = GradeComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == grade);

            EditPopup.IsOpen = true;

            PresenceComboBox_SelectionChanged(null, null);
        }


        private void PresenceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PresenceComboBox == null || ReasonComboBox == null || GradeComboBox == null)
                return;

            if (PresenceComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var presence = selectedItem.Content?.ToString();

                if (string.IsNullOrEmpty(presence))
                {
                    ReasonComboBox.IsEnabled = false;
                    GradeComboBox.IsEnabled = false;
                }
                else if (presence == "Присутствовал")
                {
                    ReasonComboBox.IsEnabled = false;
                    GradeComboBox.IsEnabled = true;
                }
                else if (presence == "Отсутствовал")
                {
                    ReasonComboBox.IsEnabled = true;
                    GradeComboBox.IsEnabled = false;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var presence = (PresenceComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            var reason = (ReasonComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            var grade = (GradeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (_currentAttendance != null)
            {
                _currentAttendance.Presence = presence;
                _currentAttendance.Reason = reason;
                _viewModel.UpdateAttendance(_currentAttendance);
            }

            if (presence == "Присутствовал" && !string.IsNullOrEmpty(grade) && _viewModel.SelectedSubject != null)
            {
                if (int.TryParse(grade, out int gradeValue))
                {
                    _viewModel.UpdatePerformance(_currentStudent.Id, _viewModel.SelectedSubject.Id, _currentDate, gradeValue);
                }

                if (!_currentStudent.Performances.ContainsKey(_viewModel.SelectedSubject.Id))
                {
                    _currentStudent.Performances[_viewModel.SelectedSubject.Id] = new Dictionary<string, string>();
                }
                _currentStudent.Performances[_viewModel.SelectedSubject.Id][_currentDate.ToString("yyyy-MM-dd")] = grade;
            }

            EditPopup.IsOpen = false;
            UpdateColumns();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            EditPopup.IsOpen = false;
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid == null) return;

            var scrollViewer = FindVisualChild<ScrollViewer>(dataGrid);
            if (scrollViewer == null) return;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (e.Delta > 0)
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - 50);
                else
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + 50);
            }
            else
            {
                if (e.Delta > 0)
                    scrollViewer.LineUp();
                else
                    scrollViewer.LineDown();
            }

            e.Handled = true;
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childResult = FindVisualChild<T>(child);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }
    }

    public static class VisualTreeExtensions
    {
        public static T FindVisualParent<T>(this DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }

    public class AttendanceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Attendance attendance)
            {
                if (attendance.Presence == "Отсутствовал")
                    return attendance.Reason == "Неуважительная" ? "Н" : "У";

                if (attendance.ViewModel?.SelectedSubject == null) return "";

                string grade = "";
                if (attendance.Student?.Performances.TryGetValue(attendance.ViewModel.SelectedSubject.Id, out var performances) == true && performances.ContainsKey(attendance.Date.ToString("yyyy-MM-dd")))
                {
                    grade = performances[attendance.Date.ToString("yyyy-MM-dd")];
                }
                return grade;
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AbsenceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string cellValue && cellValue == "Н")
                return true;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RespectfulAbsenceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string cellValue && cellValue == "У")
                return true;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AbsencesCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Student student && parameter is PerformanceAttendanceViewModel viewModel)
            {
                return viewModel.GetUnexcusedAbsencesCount(student).ToString();
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RespectfulAbsencesCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Student student && parameter is PerformanceAttendanceViewModel viewModel)
            {
                return viewModel.GetExcusedAbsencesCount(student).ToString();
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TotalAbsencesCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Student student && parameter is PerformanceAttendanceViewModel viewModel)
            {
                return viewModel.GetTotalAbsencesCount(student).ToString();
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AverageGradeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Student student && parameter is PerformanceAttendanceViewModel viewModel)
            {
                var average = viewModel.GetAverageGrade(student);
                return average == 0 ? "" : average.ToString("F2");
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using Npgsql;
using SchoolManager.Core;
using SchoolManager.MVVM.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SchoolManager.Models;

namespace SchoolManager.MVVM.ViewModel
{
    public static class VisualTreeHelperExtensions
    {
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t)
                {
                    yield return t;
                }

                foreach (var childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

        public static T FindVisualParent<T>(this DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
                return null;

            T parent = parentObject as T;
            if (parent != null)
                return parent;

            return FindVisualParent<T>(parentObject);
        }
    }

    public class EditScheduleViewModel : ObservableObject
    {
        private ObservableCollection<string> _classes;
        private ObservableCollection<string> _subjects;
        private ObservableCollection<string> _classrooms;
        private Dictionary<int, ObservableCollection<string>> _accessibleTeachersByLesson;
        private Dictionary<int, ObservableCollection<string>> _accessibleClassroomsByLesson;
        private DateTime? _selectedDate;
        private Dictionary<string, ObservableCollection<ScheduleItem>> _classSchedules;
        private Dictionary<string, bool> _classScheduleStatus;
        private bool _isInitialized;

        public ObservableCollection<string> Classes
        {
            get => _classes;
            set
            {
                _classes = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Subjects
        {
            get => _subjects;
            set
            {
                _subjects = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Classrooms
        {
            get => _classrooms;
            set
            {
                _classrooms = value;
                OnPropertyChanged();
            }
        }

        public Dictionary<int, ObservableCollection<string>> AvailableTeachersByLesson
        {
            get => _accessibleTeachersByLesson;
            set
            {
                _accessibleTeachersByLesson = value;
                OnPropertyChanged();
            }
        }

        public Dictionary<int, ObservableCollection<string>> AvailableClassroomsByLesson
        {
            get => _accessibleClassroomsByLesson;
            set
            {
                _accessibleClassroomsByLesson = value;
                OnPropertyChanged();
            }
        }

        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
                if (_isInitialized) LoadSchedules();
            }
        }

        public Dictionary<string, ObservableCollection<ScheduleItem>> ClassSchedules
        {
            get => _classSchedules;
            set
            {
                _classSchedules = value;
                OnPropertyChanged();
            }
        }

        public Dictionary<string, bool> ClassScheduleStatus
        {
            get => _classScheduleStatus;
            set
            {
                _classScheduleStatus = value;
                OnPropertyChanged();
                (PublishScheduleCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand SaveScheduleCommand { get; set; }
        public ICommand PublishScheduleCommand { get; set; }
        public ICommand AddLessonCommand { get; set; }
        public ICommand RemoveLessonCommand { get; set; }

        public EditScheduleViewModel()
        {
            Classes = new ObservableCollection<string>();
            Subjects = new ObservableCollection<string>();
            Classrooms = new ObservableCollection<string>();
            AvailableTeachersByLesson = new Dictionary<int, ObservableCollection<string>>();
            AvailableClassroomsByLesson = new Dictionary<int, ObservableCollection<string>>();
            ClassSchedules = new Dictionary<string, ObservableCollection<ScheduleItem>>();
            ClassScheduleStatus = new Dictionary<string, bool>();

            for (int lesson = 1; lesson <= 8; lesson++)
            {
                AvailableTeachersByLesson[lesson] = new ObservableCollection<string>();
                AvailableClassroomsByLesson[lesson] = new ObservableCollection<string>();
            }

            SaveScheduleCommand = new RelayCommand(SaveSchedule);
            PublishScheduleCommand = new RelayCommand(PublishSchedule, CanPublishSchedule);
            AddLessonCommand = new RelayCommand(AddLesson, CanAddLesson);
            RemoveLessonCommand = new RelayCommand(RemoveLesson);

            _isInitialized = false;
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                LoadClasses();
                LoadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в EditScheduleViewModel: {ex.Message}");
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            _isInitialized = true;
        }

        private void LoadClasses()
        {
            Classes.Clear();
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT Название FROM Классы ORDER BY Название", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var classList = new List<string>();
                        while (reader.Read())
                        {
                            string className = reader.GetString(0);
                            classList.Add(className);
                        }

                        var numericClasses = classList.Where(name => char.IsDigit(name[0])).ToList();
                        var textClasses = classList.Where(name => !char.IsDigit(name[0])).ToList();

                        numericClasses.Sort((a, b) =>
                        {
                            int aNumber = ExtractNumber(a);
                            int bNumber = ExtractNumber(b);
                            return aNumber.CompareTo(bNumber);
                        });

                        textClasses.Sort();

                        classList = numericClasses.Concat(textClasses).ToList();

                        foreach (var className in classList)
                        {
                            Classes.Add(className);
                            if (!ClassSchedules.ContainsKey(className))
                            {
                                ClassSchedules[className] = new ObservableCollection<ScheduleItem>();
                            }
                            if (!ClassScheduleStatus.ContainsKey(className))
                            {
                                ClassScheduleStatus[className] = false;
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Загружено классов: {Classes.Count}");
                if (Classes.Count == 0)
                {
                    MessageBox.Show("Список классов пуст. Проверьте данные в таблице Классы.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                OnPropertyChanged(nameof(Classes));
                OnPropertyChanged(nameof(ClassSchedules));
                OnPropertyChanged(nameof(ClassScheduleStatus));
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки классов: {ex.Message}");
                MessageBox.Show($"Не удалось загрузить список классов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int ExtractNumber(string name)
        {
            string numberPart = new string(name.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(numberPart, out int number) ? number : 0;
        }

        private void LoadData()
        {
            Subjects.Clear();
            Classrooms.Clear();
            for (int lesson = 1; lesson <= 8; lesson++)
            {
                AvailableTeachersByLesson[lesson].Clear();
                AvailableClassroomsByLesson[lesson].Clear();
            }

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = new NpgsqlCommand("SELECT Название FROM Предметы ORDER BY Название", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Subjects.Add(reader.GetString(0));
                        }
                    }

                    var allTeachers = new List<string>();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT CONCAT(Фамилия, ' ', Имя, ' ', Отчество) AS ФИО " +
                        "FROM Преподаватели " +
                        "ORDER BY ФИО", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allTeachers.Add(reader.GetString(0));
                        }
                    }

                    var classroomList = new List<string>();
                    using (var cmd = new NpgsqlCommand("SELECT Номер_кабинета FROM Кабинеты ORDER BY Номер_кабинета", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            classroomList.Add(reader.GetString(0));
                        }
                    }

                    classroomList.Sort((a, b) =>
                    {
                        bool aIsNumeric = int.TryParse(new string(a.TakeWhile(char.IsDigit).ToArray()), out int aNumber);
                        bool bIsNumeric = int.TryParse(new string(b.TakeWhile(char.IsDigit).ToArray()), out int bNumber);

                        if (aIsNumeric && bIsNumeric)
                            return aNumber.CompareTo(bNumber);
                        if (aIsNumeric) return -1;
                        if (bIsNumeric) return 1;
                        return a.CompareTo(b);
                    });

                    foreach (var classroom in classroomList)
                    {
                        Classrooms.Add(classroom);
                    }

                    for (int lesson = 1; lesson <= 8; lesson++)
                    {
                        AvailableTeachersByLesson[lesson].Clear();
                        AvailableClassroomsByLesson[lesson].Clear();
                        foreach (var teacher in allTeachers)
                        {
                            AvailableTeachersByLesson[lesson].Add(teacher);
                        }
                        foreach (var classroom in Classrooms)
                        {
                            AvailableClassroomsByLesson[lesson].Add(classroom);
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Загружено предметов: {Subjects.Count}");
                System.Diagnostics.Debug.WriteLine($"Загружено кабинетов: {Classrooms.Count}");
                System.Diagnostics.Debug.WriteLine($"Загружено преподавателей: {AvailableTeachersByLesson[1].Count}");

                OnPropertyChanged(nameof(Subjects));
                OnPropertyChanged(nameof(Classrooms));
                OnPropertyChanged(nameof(AvailableTeachersByLesson));
                OnPropertyChanged(nameof(AvailableClassroomsByLesson));
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки данных: {ex.Message}");
                MessageBox.Show($"Не удалось загрузить данные: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateAvailability(string teacherName, string classroomNumber, int lessonNumber, bool isAvailable)
        {
            if (lessonNumber < 1 || lessonNumber > 8)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: lessonNumber ({lessonNumber}) вне допустимого диапазона (1–8).");
                return;
            }
            OnPropertyChanged(nameof(AvailableTeachersByLesson));
            OnPropertyChanged(nameof(AvailableClassroomsByLesson));
        }

        private void LoadSchedules()
        {
            if (!SelectedDate.HasValue)
                return;

            foreach (var className in Classes)
            {
                for (int lesson = 1; lesson <= 8; lesson++)
                {
                    AvailableTeachersByLesson[lesson].Clear();
                    AvailableClassroomsByLesson[lesson].Clear();

                    try
                    {
                        string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                        using (var conn = new NpgsqlConnection(connectionString))
                        {
                            conn.Open();

                            using (var cmd = new NpgsqlCommand(
                                "SELECT CONCAT(Фамилия, ' ', Имя, ' ', Отчество) AS ФИО " +
                                "FROM Преподаватели " +
                                "ORDER BY ФИО", conn))
                            {
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        AvailableTeachersByLesson[lesson].Add(reader.GetString(0));
                                    }
                                }
                            }

                            using (var cmd = new NpgsqlCommand(
                                "SELECT Номер_кабинета AS Номер " +
                                "FROM Кабинеты " +
                                "ORDER BY Номер", conn))
                            {
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        AvailableClassroomsByLesson[lesson].Add(reader.GetString(0));
                                    }
                                }
                            }
                        }
                    }
                    catch (NpgsqlException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка загрузки доступных преподавателей/кабинетов для урока {lesson}: {ex.Message}");
                        MessageBox.Show($"Не удалось загрузить доступных преподавателей/кабинетов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                ClassSchedules[className].Clear();
                try
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = @"
                    SELECT r.Номер_урока, r.Время_начала, r.Время_окончания,
                           p.Название AS Название_предмета,
                           CONCAT(pr.Фамилия, ' ', pr.Имя, ' ', pr.Отчество) AS ФИО_преподавателя,
                           k.Номер_кабинета
                    FROM Расписание r
                    JOIN Предметы p ON r.ID_предмет = p.ID_предмет
                    JOIN Преподаватели pr ON r.ID_преподаватель = pr.ID_преподаватель
                    JOIN Кабинеты k ON r.ID_кабинет = k.ID_кабинет
                    JOIN Классы kl ON r.ID_класс = kl.ID_класс
                    WHERE kl.Название = @className AND r.Дата = @selectedDate AND r.Опубликовано = FALSE
                    ORDER BY r.Номер_урока";

                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("className", className);
                            cmd.Parameters.AddWithValue("selectedDate", SelectedDate.Value);

                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int lessonNumber = reader.GetInt32(0);
                                    if (lessonNumber < 1 || lessonNumber > 8)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Пропущена запись с некорректным номером урока: {lessonNumber} для класса {className}");
                                        continue;
                                    }

                                    var item = new ScheduleItem(UpdateAvailability)
                                    {
                                        LessonNumber = lessonNumber,
                                        StartTime = reader.GetTimeSpan(1).ToString(@"hh\:mm"),
                                        EndTime = reader.GetTimeSpan(2).ToString(@"hh\:mm"),
                                        SubjectName = reader.GetString(3),
                                        TeacherName = reader.GetString(4),
                                        ClassroomNumber = reader.GetString(5)
                                    };
                                    ClassSchedules[className].Add(item);
                                }
                            }
                        }
                    }
                    ClassScheduleStatus[className] = ClassSchedules[className].Any();
                }
                catch (NpgsqlException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки расписания для класса {className}: {ex.Message}");
                    MessageBox.Show($"Не удалось загрузить расписание для класса {className}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            OnPropertyChanged(nameof(ClassSchedules));
            OnPropertyChanged(nameof(ClassScheduleStatus));
            OnPropertyChanged(nameof(AvailableTeachersByLesson));
            OnPropertyChanged(nameof(AvailableClassroomsByLesson));
        }

        private void SaveSchedule(object parameter)
        {
            string className = parameter as string;

            if (string.IsNullOrEmpty(className) || !SelectedDate.HasValue)
            {
                return;
            }

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    foreach (var item in ClassSchedules[className])
                    {
                        if (IsDuplicateSchedule(conn, className, item))
                        {
                            MessageBox.Show($"Дублирующая запись найдена для урока {item.LessonNumber}.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    using (var cmd = new NpgsqlCommand("DELETE FROM Расписание WHERE ID_класс = (SELECT ID_класс FROM Классы WHERE Название = @className) AND Дата = @selectedDate", conn))
                    {
                        cmd.Parameters.AddWithValue("className", className);
                        cmd.Parameters.AddWithValue("selectedDate", SelectedDate.Value);
                        cmd.ExecuteNonQuery();
                    }

                    foreach (var item in ClassSchedules[className])
                    {
                        if (string.IsNullOrEmpty(item.SubjectName) || string.IsNullOrEmpty(item.TeacherName) || string.IsNullOrEmpty(item.ClassroomNumber))
                            continue;

                        using (var cmd = new NpgsqlCommand(
                            "INSERT INTO Расписание (Дата, Время_начала, Время_окончания, Номер_урока, ID_предмет, ID_преподаватель, ID_кабинет, ID_класс, Опубликовано, Является_заменой) " +
                            "VALUES (@date, @startTime, @endTime, @lessonNumber, " +
                            "(SELECT ID_предмет FROM Предметы WHERE Название = @subject), " +
                            "(SELECT ID_преподаватель FROM Преподаватели WHERE CONCAT(Фамилия, ' ', Имя, ' ', Отчество) = @teacher), " +
                            "(SELECT ID_кабинет FROM Кабинеты WHERE Номер_кабинета = @classroom), " +
                            "(SELECT ID_класс FROM Классы WHERE Название = @class), FALSE, @isReplacement)", conn))
                        {
                            cmd.Parameters.AddWithValue("date", SelectedDate.Value);
                            cmd.Parameters.AddWithValue("startTime", TimeSpan.Parse(item.StartTime));
                            cmd.Parameters.AddWithValue("endTime", TimeSpan.Parse(item.EndTime));
                            cmd.Parameters.AddWithValue("lessonNumber", item.LessonNumber);
                            cmd.Parameters.AddWithValue("subject", item.SubjectName);
                            cmd.Parameters.AddWithValue("teacher", item.TeacherName);
                            cmd.Parameters.AddWithValue("classroom", item.ClassroomNumber);
                            cmd.Parameters.AddWithValue("class", className);
                            cmd.Parameters.AddWithValue("isReplacement", item.IsReplacement);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                ClassScheduleStatus[className] = true;
                OnPropertyChanged(nameof(ClassScheduleStatus));

                var editScheduleView = Application.Current.Windows.OfType<Window>()
                    .SelectMany(w => w.FindVisualChildren<EditScheduleView>())
                    .FirstOrDefault();
                if (editScheduleView != null)
                {
                    var itemsControl = editScheduleView.FindName("ItemsControl") as ItemsControl;
                    if (itemsControl != null)
                    {
                        foreach (var item in itemsControl.Items)
                        {
                            var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                            if (container != null && item.ToString() == className)
                            {
                                var expander = container.FindVisualChildren<Expander>().FirstOrDefault(e => e.Name == "ClassExpander");
                                if (expander != null)
                                {
                                    expander.IsExpanded = false;
                                    System.Diagnostics.Debug.WriteLine($"Expander for class {className} collapsed.");
                                }
                            }
                        }
                    }
                }

                MessageBox.Show("Расписание успешно сохранено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsDuplicateSchedule(NpgsqlConnection conn, string className, ScheduleItem item)
        {
            using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM Расписание WHERE Дата = @date AND Номер_урока = @lessonNumber AND ID_класс = (SELECT ID_класс FROM Классы WHERE Название = @className)", conn))
            {
                cmd.Parameters.AddWithValue("date", SelectedDate.Value);
                cmd.Parameters.AddWithValue("lessonNumber", item.LessonNumber);
                cmd.Parameters.AddWithValue("className", className);

                return (long)cmd.ExecuteScalar() > 0;
            }
        }

        private bool CanAddLesson(object parameter)
        {
            if (parameter is string className && ClassSchedules.TryGetValue(className, out var schedule))
            {
                return schedule.Count < 8;
            }
            return true;
        }

        private void AddLesson(object parameter)
        {
            string className = parameter as string;
            if (string.IsNullOrEmpty(className)) return;

            var schedule = ClassSchedules[className];
            if (schedule.Count >= 8)
            {
                MessageBox.Show("Нельзя добавить больше 8 уроков для одного класса.", "Ограничение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int lessonNumber = schedule.Count + 1;

            TimeSpan startTime;
            TimeSpan endTime;
            switch (lessonNumber)
            {
                case 1:
                    startTime = new TimeSpan(8, 0, 0);
                    endTime = new TimeSpan(8, 45, 0);
                    break;
                case 2:
                    startTime = new TimeSpan(8, 50, 0);
                    endTime = new TimeSpan(9, 35, 0);
                    break;
                case 3:
                    startTime = new TimeSpan(9, 40, 0);
                    endTime = new TimeSpan(10, 25, 0);
                    break;
                case 4:
                    startTime = new TimeSpan(10, 30, 0);
                    endTime = new TimeSpan(11, 15, 0);
                    break;
                case 5:
                    startTime = new TimeSpan(11, 20, 0);
                    endTime = new TimeSpan(12, 05, 0);
                    break;
                case 6:
                    startTime = new TimeSpan(12, 10, 0);
                    endTime = new TimeSpan(12, 55, 0);
                    break;
                case 7:
                    startTime = new TimeSpan(13, 0, 0);
                    endTime = new TimeSpan(13, 45, 0);
                    break;
                case 8:
                    startTime = new TimeSpan(14, 0, 0);
                    endTime = new TimeSpan(14, 45, 0);
                    break;
                default:
                    startTime = new TimeSpan(14, 0, 0);
                    endTime = new TimeSpan(14, 45, 0);
                    break;
            }

            schedule.Add(new ScheduleItem(UpdateAvailability)
            {
                LessonNumber = lessonNumber,
                StartTime = startTime.ToString(@"hh\:mm"),
                EndTime = endTime.ToString(@"hh\:mm"),
                SubjectName = null,
                TeacherName = null,
                ClassroomNumber = null
            });

            OnPropertyChanged(nameof(ClassSchedules));
            (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void RemoveLesson(object parameter)
        {
            string className = parameter as string;
            if (string.IsNullOrEmpty(className)) return;

            var schedule = ClassSchedules[className];
            if (schedule.Count > 0)
            {
                var lastLesson = schedule[schedule.Count - 1];
                if (!string.IsNullOrEmpty(lastLesson.TeacherName))
                {
                    UpdateAvailability(lastLesson.TeacherName, null, lastLesson.LessonNumber, true);
                }
                if (!string.IsNullOrEmpty(lastLesson.ClassroomNumber))
                {
                    UpdateAvailability(null, lastLesson.ClassroomNumber, lastLesson.LessonNumber, true);
                }
                schedule.RemoveAt(schedule.Count - 1);
            }

            OnPropertyChanged(nameof(ClassSchedules));
            OnPropertyChanged(nameof(AvailableTeachersByLesson));
            OnPropertyChanged(nameof(AvailableClassroomsByLesson));
            (AddLessonCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void PublishSchedule(object parameter)
        {
            if (!Classes.Any() || !SelectedDate.HasValue)
            {
                MessageBox.Show("Нет классов для публикации или не выбрана дата!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var sortedClasses = Classes.ToList();
                sortedClasses.Sort((a, b) =>
                {
                    int aNumber = ExtractNumber(a);
                    int bNumber = ExtractNumber(b);
                    return aNumber.CompareTo(bNumber);
                });

                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string scheduleFolder = Path.Combine(documentsPath, "Расписание");
                if (!Directory.Exists(scheduleFolder))
                {
                    Directory.CreateDirectory(scheduleFolder);
                }

                string excelPath = Path.Combine(scheduleFolder, $"Расписание{SelectedDate.Value.ToString("dd_MM_yyyy")}.xlsx");
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Расписание");

                    int lastClassColumn = 2 + sortedClasses.Count;
                    worksheet.Range(1, 1, 1, lastClassColumn).Merge();
                    worksheet.Cell(1, 1).Value = $"Schedule for {SelectedDate.Value.ToString("dd/MM/yyyy")}";
                    worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    worksheet.Cell(2, 1).Value = "№ урока";
                    worksheet.Cell(2, 1).Style.Font.FontSize = 8;
                    worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.White;
                    worksheet.Cell(2, 1).Style.Alignment.SetTextRotation(90);
                    worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(2, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    worksheet.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4A2E5F");

                    worksheet.Cell(2, 2).Value = "Время";
                    worksheet.Cell(2, 2).Style.Font.FontSize = 8;
                    worksheet.Cell(2, 2).Style.Font.FontColor = XLColor.White;
                    worksheet.Cell(2, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(2, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    worksheet.Cell(2, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#4A2E5F");

                    worksheet.Column(1).Width = 1.71;
                    worksheet.Column(2).Width = 10.14;

                    int col = 3;
                    foreach (var className in sortedClasses)
                    {
                        worksheet.Cell(2, col).Value = "Дисциплина" + "\n" + "Преподаватель";
                        worksheet.Cell(2, col).Style.Font.FontSize = 8;
                        worksheet.Cell(2, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        worksheet.Cell(2, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        worksheet.Cell(3, col).Value = $"Класс {className}";
                        worksheet.Cell(3, col).Style.Font.FontSize = 11;
                        worksheet.Cell(3, col).Style.Font.FontColor = XLColor.White;
                        worksheet.Cell(3, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(3, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        worksheet.Cell(3, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#4A2E5F");
                        col++;
                    }

                    int maxLessons = ClassSchedules.Values.Max(s => s.Count > 0 ? s.Max(l => l.LessonNumber) : 1);
                    for (int lesson = 1; lesson <= maxLessons; lesson++)
                    {
                        int row = lesson + 3;
                        worksheet.Cell(row, 1).Value = lesson;
                        worksheet.Cell(row, 1).Style.Font.FontSize = 8;
                        worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.White;
                        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(row, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4A2E5F");

                        var firstSchedule = ClassSchedules.Values.FirstOrDefault(s => s.Any(l => l.LessonNumber == lesson));
                        if (firstSchedule != null)
                        {
                            var lessonItem = firstSchedule.FirstOrDefault(l => l.LessonNumber == lesson);
                            if (lessonItem != null)
                            {
                                worksheet.Cell(row, 2).Value = $"{lessonItem.StartTime}-{lessonItem.EndTime}";
                                worksheet.Cell(row, 2).Style.Font.FontSize = 8;
                                worksheet.Cell(row, 2).Style.Font.FontColor = XLColor.White;
                                worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                worksheet.Cell(row, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                worksheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#4A2E5F");
                            }
                        }

                        col = 3;
                        foreach (var className in sortedClasses)
                        {
                            var schedule = ClassSchedules[className].FirstOrDefault(s => s.LessonNumber == lesson);
                            if (schedule != null)
                            {
                                worksheet.Cell(row, col).Value = $"{schedule.SubjectName}\n{schedule.TeacherName}\nКабинет: {schedule.ClassroomNumber}";
                                worksheet.Cell(row, col).Style.Font.FontSize = 11;
                                worksheet.Cell(row, col).Style.Alignment.WrapText = true;
                            }
                            col++;
                        }
                    }

                    int lastRow = maxLessons + 4;
                    worksheet.Range(lastRow, 1, lastRow, lastClassColumn).Merge();
                    worksheet.Cell(lastRow, 1).Value = $"Расписание составлено: {DateTime.Now.ToString("dd/MM/yyyy")}";
                    worksheet.Cell(lastRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(lastRow, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Установка границ для таблицы
                    var tableRange = worksheet.Range(2, 1, lastRow - 1, lastClassColumn);
                    tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(excelPath);
                }

                string pdfPath = Path.Combine(scheduleFolder, $"Расписание{SelectedDate.Value.ToString("dd_MM_yyyy")}.pdf");
                using (var pdf = new PdfDocument())
                {
                    var page = pdf.AddPage();
                    page.Orientation = PdfSharp.PageOrientation.Landscape;
                    var gfx = XGraphics.FromPdfPage(page);

                    var headerFont = new XFont("Verdana", 8, XFontStyleEx.Regular);
                    var dataFont = new XFont("Verdana", 11, XFontStyleEx.Regular);

                    var fillColor = XColor.FromArgb(74, 46, 95);
                    var fillBrush = new XSolidBrush(fillColor);
                    var whiteBrush = XBrushes.White;
                    var blackBrush = XBrushes.Black;

                    double lessonNumberWidth = 1.71 * 7;
                    double timeWidth = 10.14 * 7;
                    double classWidth = 100;
                    double rowHeight = 30;

                    double marginLeft = 20;
                    double marginTop = 20;
                    double x = marginLeft;
                    double y = marginTop;

                    double totalWidth = lessonNumberWidth + timeWidth + (sortedClasses.Count * classWidth);
                    gfx.DrawString($"Расписание для {SelectedDate.Value.ToString("dd/MM/yyyy")}", headerFont, blackBrush,
                        new XRect(marginLeft, y, totalWidth, rowHeight), XStringFormats.Center);
                    y += rowHeight;

                    double[] columnPositions = new double[2 + sortedClasses.Count];
                    columnPositions[0] = x;
                    x += lessonNumberWidth;
                    columnPositions[1] = x;
                    x += timeWidth;
                    for (int i = 0; i < sortedClasses.Count; i++)
                    {
                        columnPositions[2 + i] = x;
                        x += classWidth;
                    }

                    gfx.DrawRectangle(fillBrush, columnPositions[0], y, lessonNumberWidth, rowHeight);
                    gfx.RotateAtTransform(90, new XPoint(columnPositions[0] + lessonNumberWidth / 2, y + rowHeight / 2));
                    gfx.DrawString("№ урока", headerFont, whiteBrush,
                        new XRect(columnPositions[0], y, lessonNumberWidth, rowHeight), XStringFormats.Center);
                    gfx.RotateAtTransform(-90, new XPoint(columnPositions[0] + lessonNumberWidth / 2, y + rowHeight / 2));

                    gfx.DrawRectangle(fillBrush, columnPositions[1], y, timeWidth, rowHeight);
                    gfx.DrawString("Время", headerFont, whiteBrush,
                        new XRect(columnPositions[1], y, timeWidth, rowHeight), XStringFormats.Center);

                    for (int i = 0; i < sortedClasses.Count; i++)
                    {
                        var className = sortedClasses[i];
                        gfx.DrawString("Дисциплина Преподаватель", headerFont, blackBrush,
                            new XRect(columnPositions[2 + i] + 5, y, classWidth - 10, rowHeight), XStringFormats.CenterLeft);
                    }
                    y += rowHeight;

                    for (int i = 0; i < sortedClasses.Count; i++)
                    {
                        var className = sortedClasses[i];
                        gfx.DrawRectangle(fillBrush, columnPositions[2 + i], y, classWidth, rowHeight);
                        gfx.DrawString($"Класс {className}", headerFont, whiteBrush,
                            new XRect(columnPositions[2 + i], y, classWidth, rowHeight), XStringFormats.Center);
                    }

                    int maxLessons = ClassSchedules.Values.Max(s => s.Count > 0 ? s.Max(l => l.LessonNumber) : 1);
                    for (int lesson = 1; lesson <= maxLessons; lesson++)
                    {
                        y += rowHeight;
                        gfx.DrawRectangle(fillBrush, columnPositions[0], y, lessonNumberWidth, rowHeight);
                        gfx.DrawString(lesson.ToString(), headerFont, whiteBrush,
                            new XRect(columnPositions[0], y, lessonNumberWidth, rowHeight), XStringFormats.Center);

                        var firstSchedule = ClassSchedules.Values.FirstOrDefault(s => s.Any(l => l.LessonNumber == lesson));
                        if (firstSchedule != null)
                        {
                            var lessonItem = firstSchedule.FirstOrDefault(l => l.LessonNumber == lesson);
                            if (lessonItem != null)
                            {
                                gfx.DrawRectangle(fillBrush, columnPositions[1], y, timeWidth, rowHeight);
                                gfx.DrawString($"{lessonItem.StartTime}-{lessonItem.EndTime}", headerFont, whiteBrush,
                                    new XRect(columnPositions[1], y, timeWidth, rowHeight), XStringFormats.Center);
                            }
                        }

                        for (int i = 0; i < sortedClasses.Count; i++)
                        {
                            var className = sortedClasses[i];
                            var schedule = ClassSchedules[className].FirstOrDefault(s => s.LessonNumber == lesson);
                            if (schedule != null)
                            {
                                string text = $"{schedule.SubjectName}\n{schedule.TeacherName}\nКабинет: {schedule.ClassroomNumber}";
                                gfx.DrawString(text, dataFont, blackBrush,
                                    new XRect(columnPositions[2 + i] + 5, y, classWidth - 10, rowHeight * 2), XStringFormats.TopLeft);
                            }
                        }
                    }

                    y += rowHeight * 2;
                    gfx.DrawString($"Расписание составлено: {DateTime.Now.ToString("dd/MM/yyyy")}", headerFont, blackBrush,
                        new XRect(marginLeft, y, totalWidth, rowHeight), XStringFormats.Center);

                    double tableHeight = (maxLessons + 2) * rowHeight;
                    for (int i = 0; i < columnPositions.Length; i++)
                    {
                        double colX = columnPositions[i];
                        double colWidth = i == 0 ? lessonNumberWidth : (i == 1 ? timeWidth : classWidth);
                        gfx.DrawLine(XPens.Black, colX, marginTop + rowHeight, colX, marginTop + rowHeight + tableHeight);
                        for (int row = 0; row <= maxLessons + 2; row++)
                        {
                            double rowY = marginTop + rowHeight + (row * rowHeight);
                            gfx.DrawLine(XPens.Black, colX, rowY, colX + colWidth, rowY);
                        }
                    }
                    gfx.DrawLine(XPens.Black, columnPositions[columnPositions.Length - 1] + classWidth, marginTop + rowHeight,
                        columnPositions[columnPositions.Length - 1] + classWidth, marginTop + rowHeight + tableHeight);

                    pdf.Save(pdfPath);
                }

                Application.Current.Properties["LastPublishedExcelPath"] = excelPath;

                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("UPDATE Расписание SET Опубликовано = TRUE, Дата_публикации = @publishDate WHERE Дата = @selectedDate", conn))
                    {
                        cmd.Parameters.AddWithValue("publishDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("selectedDate", SelectedDate.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                if (Application.Current.MainWindow.DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.HomeVM?.LoadLastPublishedDate();
                    mainViewModel.UpdateScheduleViewDate(SelectedDate);
                    mainViewModel.CurrentView = mainViewModel.ScheduleVM;
                }

                foreach (var className in Classes)
                {
                    ClassSchedules[className].Clear();
                    ClassScheduleStatus[className] = false;
                }
                OnPropertyChanged(nameof(ClassSchedules));
                OnPropertyChanged(nameof(ClassScheduleStatus));

                MessageBox.Show("Расписание успешно опубликовано!\\Файлы PDF и Excel сохранены в папке 'Документы'\\Расписание'.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при публикации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanPublishSchedule(object parameter)
        {
            return ClassScheduleStatus.All(s => s.Value);
        }
    }
}
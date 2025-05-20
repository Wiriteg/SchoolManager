using SchoolManager.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Windows;
using Npgsql;

namespace SchoolManager.MVVM.ViewModel
{
    public class LessonInfo
    {
        public string Subject { get; set; }
        public string Teacher { get; set; }
        public string Classroom { get; set; }

        public override string ToString()
        {
            return $"{Subject}\n{Teacher}\n{Classroom}";
        }
    }

    public class ScheduleRow
    {
        public string LessonNumber { get; set; }
        public string Time { get; set; }
        public Dictionary<string, LessonInfo> ClassLessons { get; set; }
    }

    public class ScheduleViewModel : ObservableObject
    {
        private ObservableCollection<ScheduleRow> _scheduleData;
        private ObservableCollection<string> _classes;
        private DateTime? _selectedDate;
        private string _scheduleDate;
        private string _dateHeader;

        public ObservableCollection<ScheduleRow> ScheduleData
        {
            get => _scheduleData;
            set
            {
                _scheduleData = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Classes
        {
            get => _classes;
            set
            {
                _classes = value;
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
                UpdateDateHeader();
                LoadSchedule();
            }
        }

        public string ScheduleDate
        {
            get => _scheduleDate;
            set
            {
                _scheduleDate = value;
                if (!string.IsNullOrEmpty(_scheduleDate) && _scheduleDate.Contains(" "))
                {
                    _scheduleDate = _scheduleDate.Substring(0, _scheduleDate.IndexOf(" ") + 11);
                }
                OnPropertyChanged();
            }
        }

        public string DateHeader
        {
            get => _dateHeader;
            set
            {
                _dateHeader = value;
                OnPropertyChanged();
            }
        }

        public ScheduleViewModel()
        {
            ScheduleData = new ObservableCollection<ScheduleRow>();
            Classes = new ObservableCollection<string>();
            LoadClasses();

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                SelectedDate = DateTime.Today;
            }
        }

        private void LoadClasses()
        {
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
                            classList.Add(reader.GetString(0));
                        }

                        classList.Sort((a, b) =>
                        {
                            int aNumber = ExtractNumber(a);
                            int bNumber = ExtractNumber(b);
                            return aNumber.CompareTo(bNumber);
                        });

                        foreach (var className in classList)
                        {
                            Classes.Add(className);
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки классов: {ex.Message}");
            }
        }

        private int ExtractNumber(string name)
        {
            string numberPart = new string(name.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(numberPart, out int number) ? number : 0;
        }

        private void UpdateDateHeader()
        {
            DateHeader = SelectedDate.HasValue ? $"на {SelectedDate.Value:dd.MM.yyyy}" : "на неопределенную дату";
        }

        public void LoadSchedule()
        {
            ScheduleData.Clear();

            if (!SelectedDate.HasValue)
            {
                CreateEmptySchedule();
                return;
            }

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT r.Дата, r.Номер_урока, r.Время_начала, r.Время_окончания,
                               p.Название AS Название_предмета,
                               CONCAT(pr.Фамилия, ' ', pr.Имя, ' ', pr.Отчество) AS ФИО_преподавателя,
                               k.Номер_кабинета,
                               kl.Название AS Название_класса,
                               r.Дата_публикации,
                               r.Опубликовано
                        FROM Расписание r
                        JOIN Предметы p ON r.ID_предмет = p.ID_предмет
                        JOIN Преподаватели pr ON r.ID_преподаватель = pr.ID_преподаватель
                        JOIN Кабинеты k ON r.ID_кабинет = k.ID_кабинет
                        JOIN Классы kl ON r.ID_класс = kl.ID_класс
                        WHERE r.Дата = @selectedDate AND r.Опубликовано = TRUE
                        ORDER BY r.Номер_урока, kl.Название";

                    var tempData = new List<(int LessonNumber, string Time, string Subject, string Teacher, string Classroom, string ClassName, bool IsPublished)>();
                    DateTime? publishDate = null;

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("selectedDate", SelectedDate.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = (
                                    LessonNumber: reader.GetInt32(1),
                                    Time: $"{reader.GetTimeSpan(2).ToString(@"hh\:mm")}-{reader.GetTimeSpan(3).ToString(@"hh\:mm")}",
                                    Subject: reader.GetString(4),
                                    Teacher: reader.GetString(5),
                                    Classroom: reader.GetString(6),
                                    ClassName: reader.GetString(7),
                                    IsPublished: reader.GetBoolean(9)
                                );
                                if (!reader.IsDBNull(8) && item.IsPublished)
                                {
                                    publishDate = reader.GetDateTime(8);
                                }
                                tempData.Add(item);
                            }
                        }
                    }

                    if (tempData.Count == 0)
                    {
                        CreateEmptySchedule();
                    }
                    else
                    {
                        var sortedClasses = tempData.Select(d => d.ClassName).Distinct().ToList();
                        sortedClasses.Sort((a, b) =>
                        {
                            int aNumber = ExtractNumber(a);
                            int bNumber = ExtractNumber(b);
                            return aNumber.CompareTo(bNumber);
                        });

                        var groupedData = tempData
                            .GroupBy(d => (d.LessonNumber, d.ClassName))
                            .Select(g => g.OrderByDescending(d => d.IsPublished).First())
                            .GroupBy(d => d.LessonNumber)
                            .OrderBy(g => g.Key)
                            .ToList();

                        foreach (var group in groupedData)
                        {
                            var firstItem = group.First();
                            var rowData = new ScheduleRow
                            {
                                LessonNumber = firstItem.LessonNumber.ToString(),
                                Time = firstItem.Time,
                                ClassLessons = new Dictionary<string, LessonInfo>()
                            };

                            foreach (var item in group)
                            {
                                rowData.ClassLessons[item.ClassName] = new LessonInfo
                                {
                                    Subject = item.Subject,
                                    Teacher = item.Teacher,
                                    Classroom = item.Classroom
                                };
                            }

                            foreach (var className in sortedClasses)
                            {
                                if (!rowData.ClassLessons.ContainsKey(className))
                                {
                                    rowData.ClassLessons[className] = new LessonInfo
                                    {
                                        Subject = "",
                                        Teacher = "",
                                        Classroom = ""
                                    };
                                }
                            }

                            ScheduleData.Add(rowData);
                        }
                    }

                    ScheduleDate = publishDate.HasValue
                        ? $"Расписание составлено: {publishDate.Value:dd.MM.yyyy}"
                        : "Дата составления неизвестна";
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка при загрузке расписания из базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                CreateEmptySchedule();
            }
        }

        private void CreateEmptySchedule()
        {
            ScheduleData.Clear();
            var defaultTimes = new Dictionary<int, string>
            {
                { 1, "08:00-08:45" },
                { 2, "08:50-09:35" },
                { 3, "09:45-10:30" },
                { 4, "10:40-11:25" },
                { 5, "11:35-12:20" },
                { 6, "12:30-13:15" },
                { 7, "13:25-14:10" },
                { 8, "14:20-15:05" }
            };

            foreach (var lesson in defaultTimes)
            {
                var rowData = new ScheduleRow
                {
                    LessonNumber = lesson.Key.ToString(),
                    Time = lesson.Value,
                    ClassLessons = new Dictionary<string, LessonInfo>()
                };

                foreach (var className in Classes)
                {
                    rowData.ClassLessons[className] = new LessonInfo
                    {
                        Subject = "",
                        Teacher = "",
                        Classroom = ""
                    };
                }

                ScheduleData.Add(rowData);
            }

            ScheduleDate = "Расписание не составлено";
        }

        public void RefreshSchedule()
        {
            LoadSchedule();
        }
    }
}
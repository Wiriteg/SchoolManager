using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using Npgsql;
using SchoolManager.Core;
using SchoolManager.Model;

namespace SchoolManager.MVVM.ViewModel
{
    public class PerformanceAttendanceViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        private ObservableCollection<Student> _students;
        private ObservableCollection<Subject> _subjects;
        private ObservableCollection<Attendance> _attendances;
        private ObservableCollection<Performance> _performances;
        private ObservableCollection<Class> _classes;
        private string _selectedAcademicYear;
        private int _selectedQuarter;
        private Class _selectedClass;
        private Subject _selectedSubject;
        private DateTime _startDate;
        private DateTime _endDate;
        private int _totalAbsencesCount;
        private int _totalUnexcusedAbsencesCount;
        private int _totalExcusedAbsencesCount;
        private double _totalAverageGrade;

        public ObservableCollection<Student> Students
        {
            get => _students;
            set
            {
                _students = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Subject> Subjects
        {
            get => _subjects;
            set
            {
                _subjects = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Attendance> Attendances
        {
            get => _attendances;
            set
            {
                _attendances = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Performance> Performances
        {
            get => _performances;
            set
            {
                _performances = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Class> Classes
        {
            get => _classes;
            set
            {
                _classes = value;
                OnPropertyChanged();
            }
        }

        public string SelectedAcademicYear
        {
            get => _selectedAcademicYear;
            set
            {
                if (_selectedAcademicYear != value)
                {
                    _selectedAcademicYear = value;
                    System.Diagnostics.Debug.WriteLine($"SelectedAcademicYear changed to: {_selectedAcademicYear}");
                    OnPropertyChanged();
                    UpdateQuarterDates();
                    if (CanLoadData()) LoadData();
                    OnPropertyChanged(nameof(IsDataLoaded));
                }
            }
        }

        public int SelectedQuarter
        {
            get => _selectedQuarter;
            set
            {
                if (_selectedQuarter != value)
                {
                    _selectedQuarter = value;
                    System.Diagnostics.Debug.WriteLine($"SelectedQuarter changed to: {_selectedQuarter}");
                    OnPropertyChanged();
                    UpdateQuarterDates();
                    if (CanLoadData()) LoadData();
                    OnPropertyChanged(nameof(IsDataLoaded));
                }
            }
        }

        public Class SelectedClass
        {
            get => _selectedClass;
            set
            {
                if (_selectedClass != value)
                {
                    _selectedClass = value;
                    System.Diagnostics.Debug.WriteLine($"SelectedClass changed to: {(_selectedClass != null ? _selectedClass.Name : "null")}");
                    OnPropertyChanged();
                    if (CanLoadData()) LoadData();
                    OnPropertyChanged(nameof(IsDataLoaded));
                }
            }
        }

        public Subject SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                if (_selectedSubject != value)
                {
                    _selectedSubject = value;
                    System.Diagnostics.Debug.WriteLine($"SelectedSubject changed to: {(_selectedSubject != null ? _selectedSubject.Name : "null")}");
                    OnPropertyChanged();
                    if (CanLoadData()) LoadData();
                    OnPropertyChanged(nameof(IsDataLoaded));
                }
            }
        }

        public ObservableCollection<string> AcademicYears { get; }

        public ObservableCollection<int> Quarters { get; }
        public DateTime StartDate => _startDate;
        public DateTime EndDate => _endDate;

        public int TotalAbsencesCount
        {
            get => _totalAbsencesCount;
            set
            {
                _totalAbsencesCount = value;
                OnPropertyChanged();
            }
        }

        public int TotalUnexcusedAbsencesCount
        {
            get => _totalUnexcusedAbsencesCount;
            set
            {
                _totalUnexcusedAbsencesCount = value;
                OnPropertyChanged();
            }
        }

        public int TotalExcusedAbsencesCount
        {
            get => _totalExcusedAbsencesCount;
            set
            {
                _totalExcusedAbsencesCount = value;
                OnPropertyChanged();
            }
        }

        public double TotalAverageGrade
        {
            get => _totalAverageGrade;
            set
            {
                _totalAverageGrade = value;
                OnPropertyChanged();
            }
        }

        public bool IsDataLoaded => CanLoadData();

        public PerformanceAttendanceViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            Students = new ObservableCollection<Student>();
            Subjects = new ObservableCollection<Subject>();
            Attendances = new ObservableCollection<Attendance>();
            Performances = new ObservableCollection<Performance>();
            Classes = _mainViewModel.ClassManagementVM?.Classes ?? new ObservableCollection<Class>();

            System.Diagnostics.Debug.WriteLine($"Classes count: {Classes.Count}");
            AcademicYears = new ObservableCollection<string> { "2024-2025", "2025-2026", "2026-2027", "2027-2028", "2028-2029" };
            Quarters = new ObservableCollection<int> { 1, 2, 3, 4 };

            LoadSubjects();
        }

        private bool CanLoadData()
        {
            bool canLoad = SelectedClass != null && !string.IsNullOrEmpty(SelectedAcademicYear) && SelectedQuarter != 0 && SelectedSubject != null;
            System.Diagnostics.Debug.WriteLine($"CanLoadData: {canLoad} (SelectedClass: {SelectedClass != null}, SelectedAcademicYear: {SelectedAcademicYear}, SelectedQuarter: {SelectedQuarter}, SelectedSubject: {SelectedSubject != null})");
            return canLoad;
        }

        private void LoadSubjects()
        {
            Subjects.Clear();
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"]?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                    throw new ConfigurationErrorsException("Connection string 'SchoolDbConnection' not found.");

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    System.Diagnostics.Debug.WriteLine("Loading subjects...");
                    using (var cmd = new NpgsqlCommand("SELECT ID_предмет, Название FROM Предметы ORDER BY Название", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        int rowCount = 0;
                        while (reader.Read())
                        {
                            Subjects.Add(new Subject
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                            rowCount++;
                        }
                        System.Diagnostics.Debug.WriteLine($"Total subjects loaded: {rowCount}");
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки предметов: {ex.Message}");
                System.Windows.MessageBox.Show($"Ошибка загрузки предметов: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void UpdateQuarterDates()
        {
            if (string.IsNullOrEmpty(SelectedAcademicYear) || SelectedQuarter == 0)
            {
                System.Diagnostics.Debug.WriteLine("SelectedAcademicYear or SelectedQuarter is invalid, skipping UpdateQuarterDates.");
                return;
            }

            int yearStart = int.Parse(SelectedAcademicYear.Split('-')[0]);
            int yearEnd = int.Parse(SelectedAcademicYear.Split('-')[1]);

            switch (SelectedQuarter)
            {
                case 1:
                    _startDate = new DateTime(yearStart, 9, 2);
                    _endDate = new DateTime(yearStart, 10, 25);
                    break;
                case 2:
                    _startDate = new DateTime(yearStart, 11, 5);
                    _endDate = new DateTime(yearStart, 12, 27);
                    break;
                case 3:
                    _startDate = new DateTime(yearEnd, 1, 9);
                    _endDate = new DateTime(yearEnd, 3, 21);
                    break;
                case 4:
                    _startDate = new DateTime(yearEnd, 3, 31);
                    _endDate = new DateTime(yearEnd, 5, 28);
                    break;
            }
            System.Diagnostics.Debug.WriteLine($"Quarter dates updated: StartDate={_startDate:yyyy-MM-dd}, EndDate={_endDate:yyyy-MM-dd}");
        }

        private void LoadData()
        {
            if (!CanLoadData())
            {
                System.Diagnostics.Debug.WriteLine("Cannot load data: Not all filters are selected.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Loading data for Quarter: {SelectedQuarter}, Class: {SelectedClass.Name}, Subject: {SelectedSubject.Name}");

            Students.Clear();
            Attendances.Clear();
            Performances.Clear();

            LoadStudents();
            LoadAttendances();
            LoadPerformances();

            System.Diagnostics.Debug.WriteLine($"Loaded {Students.Count} students, {Attendances.Count} attendances, {Performances.Count} performances");

            foreach (var student in Students)
            {
                var studentAttendancesBySubject = new Dictionary<int, Dictionary<string, Attendance>>();
                if (SelectedSubject != null)
                {
                    var subjectAttendances = Attendances
                        .Where(a => a.StudentId == student.Id && a.SubjectId == SelectedSubject.Id)
                        .ToDictionary(a => a.Date.ToString("yyyy-MM-dd"), a => a);
                    studentAttendancesBySubject[SelectedSubject.Id] = subjectAttendances;
                }
                student.SetAttendances(studentAttendancesBySubject, this);

                var studentPerformances = Performances
                    .Where(p => p.StudentId == student.Id && p.SubjectId == SelectedSubject.Id)
                    .ToDictionary(p => p.Date.ToString("yyyy-MM-dd"), p => p.Grade.ToString());
                student.SetPerformances(new Dictionary<int, Dictionary<string, string>> { { SelectedSubject.Id, studentPerformances } }, this);
            }

            UpdateTotals();
            OnPropertyChanged(nameof(Students));
        }

        private void LoadStudents()
        {
            Students.Clear();
            if (SelectedClass == null)
            {
                System.Diagnostics.Debug.WriteLine("SelectedClass is null, cannot load students.");
                return;
            }

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"]?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                    throw new ConfigurationErrorsException("Connection string 'SchoolDbConnection' not found.");

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    System.Diagnostics.Debug.WriteLine($"Loading students for class ID: {SelectedClass.Id}");
                    using (var cmd = new NpgsqlCommand("SELECT ID_ученик, Фамилия, Имя, Отчество FROM Ученики WHERE ID_класс = @classId ORDER BY Фамилия", conn))
                    {
                        cmd.Parameters.AddWithValue("classId", SelectedClass.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            int rowCount = 0;
                            while (reader.Read())
                            {
                                var student = new Student
                                {
                                    Id = reader.GetInt32(0),
                                    LastName = reader.GetString(1),
                                    FirstName = reader.GetString(2),
                                    MiddleName = reader.IsDBNull(3) ? null : reader.GetString(3)
                                };
                                System.Diagnostics.Debug.WriteLine($"Loaded student: ID={student.Id}, LastName={student.LastName}, FirstName={student.FirstName}, MiddleName={student.MiddleName}, FullName={student.FullName}");
                                Students.Add(student);
                                rowCount++;
                            }
                            System.Diagnostics.Debug.WriteLine($"Total students loaded: {rowCount}");
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки учеников: {ex.Message}");
                System.Windows.MessageBox.Show($"Ошибка загрузки учеников: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void LoadAttendances()
        {
            Attendances.Clear();
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"]?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                    throw new ConfigurationErrorsException("Connection string 'SchoolDbConnection' not found.");

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    System.Diagnostics.Debug.WriteLine($"Loading attendances for class ID: {SelectedClass?.Id}, StartDate: {_startDate}, EndDate: {_endDate}, Subject ID: {SelectedSubject?.Id}");
                    using (var cmd = new NpgsqlCommand(
                        "SELECT p.ID_посещаемость, p.ID_ученик, p.ID_предмет, p.Дата, p.Присутствие, p.Причина " +
                        "FROM Посещаемость p " +
                        "JOIN Ученики u ON p.ID_ученик = u.ID_ученик " +
                        "WHERE u.ID_класс = @classId AND p.Дата BETWEEN @startDate AND @endDate AND p.ID_предмет = @subjectId", conn))
                    {
                        cmd.Parameters.AddWithValue("classId", SelectedClass?.Id ?? 0);
                        cmd.Parameters.AddWithValue("startDate", _startDate);
                        cmd.Parameters.AddWithValue("endDate", _endDate);
                        cmd.Parameters.AddWithValue("subjectId", SelectedSubject?.Id ?? 0);
                        using (var reader = cmd.ExecuteReader())
                        {
                            int rowCount = 0;
                            while (reader.Read())
                            {
                                bool presence = reader.GetBoolean(4);
                                Attendances.Add(new Attendance
                                {
                                    Id = reader.GetInt32(0),
                                    StudentId = reader.GetInt32(1),
                                    SubjectId = reader.GetInt32(2),
                                    Date = reader.GetDateTime(3),
                                    Presence = presence ? "Присутствовал" : "Отсутствовал",
                                    Reason = reader.GetString(5)
                                });
                                rowCount++;
                            }
                            System.Diagnostics.Debug.WriteLine($"Total attendances loaded: {rowCount}");
                        }
                    }
                }

                EnsureAttendanceEntries();
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки посещаемости: {ex.Message}");
                System.Windows.MessageBox.Show($"Ошибка загрузки посещаемости: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void EnsureAttendanceEntries()
        {
            if (Students == null || !Students.Any() || SelectedSubject == null) return;

            var currentDate = _startDate;
            while (currentDate <= _endDate)
            {
                if (currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    foreach (var student in Students)
                    {
                        if (!Attendances.Any(a => a.StudentId == student.Id && a.SubjectId == SelectedSubject.Id && a.Date.Date == currentDate.Date))
                        {
                            try
                            {
                                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"]?.ConnectionString;
                                if (string.IsNullOrEmpty(connectionString))
                                    throw new ConfigurationErrorsException("Connection string 'SchoolDbConnection' not found.");

                                using (var conn = new NpgsqlConnection(connectionString))
                                {
                                    conn.Open();
                                    using (var cmd = new NpgsqlCommand(
                                        "INSERT INTO Посещаемость (ID_ученик, ID_предмет, Дата, Присутствие, Причина) " +
                                        "VALUES (@studentId, @subjectId, @date, @presence, @reason) RETURNING ID_посещаемость", conn))
                                    {
                                        cmd.Parameters.AddWithValue("studentId", student.Id);
                                        cmd.Parameters.AddWithValue("subjectId", SelectedSubject.Id);
                                        cmd.Parameters.AddWithValue("date", currentDate.Date);
                                        cmd.Parameters.AddWithValue("presence", true);
                                        cmd.Parameters.AddWithValue("reason", "");
                                        int newId = (int)cmd.ExecuteScalar();

                                        var newAttendance = new Attendance
                                        {
                                            Id = newId,
                                            StudentId = student.Id,
                                            SubjectId = SelectedSubject.Id,
                                            Date = currentDate.Date,
                                            Presence = "Присутствовал",
                                            Reason = ""
                                        };
                                        Attendances.Add(newAttendance);
                                        System.Diagnostics.Debug.WriteLine($"Added attendance for student ID={student.Id}, Subject ID={SelectedSubject.Id}, Date={currentDate:yyyy-MM-dd}");
                                    }
                                }
                            }
                            catch (NpgsqlException ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Ошибка добавления записи посещаемости: {ex.Message}");
                                System.Windows.MessageBox.Show($"Ошибка добавления записи посещаемости: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            }
                        }
                    }
                }
                currentDate = currentDate.AddDays(1);
            }

            foreach (var student in Students)
            {
                var studentAttendancesBySubject = new Dictionary<int, Dictionary<string, Attendance>>();
                var subjectAttendances = Attendances
                    .Where(a => a.StudentId == student.Id && a.SubjectId == SelectedSubject.Id)
                    .ToDictionary(a => a.Date.ToString("yyyy-MM-dd"), a => a);
                studentAttendancesBySubject[SelectedSubject.Id] = subjectAttendances;
                student.SetAttendances(studentAttendancesBySubject, this);
            }
        }

        private void LoadPerformances()
        {
            Performances.Clear();
            if (SelectedSubject == null) return;

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"]?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                    throw new ConfigurationErrorsException("Connection string 'SchoolDbConnection' not found.");

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    System.Diagnostics.Debug.WriteLine($"Loading performances for class ID: {SelectedClass?.Id}, Subject ID: {SelectedSubject.Id}");
                    using (var cmd = new NpgsqlCommand(
                        "SELECT p.ID_успеваемость, p.ID_ученик, p.ID_предмет, p.Дата, p.Оценка, p.Дата_создания " +
                        "FROM Успеваемость p " +
                        "JOIN Ученики u ON p.ID_ученик = u.ID_ученик " +
                        "WHERE u.ID_класс = @classId AND p.Дата BETWEEN @startDate AND @endDate AND p.ID_предмет = @subjectId", conn))
                    {
                        cmd.Parameters.AddWithValue("classId", SelectedClass?.Id ?? 0);
                        cmd.Parameters.AddWithValue("startDate", _startDate);
                        cmd.Parameters.AddWithValue("endDate", _endDate);
                        cmd.Parameters.AddWithValue("subjectId", SelectedSubject.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            int rowCount = 0;
                            while (reader.Read())
                            {
                                Performances.Add(new Performance
                                {
                                    Id = reader.GetInt32(0),
                                    StudentId = reader.GetInt32(1),
                                    SubjectId = reader.GetInt32(2),
                                    Date = reader.GetDateTime(3),
                                    Grade = reader.GetInt32(4),
                                    CreatedAt = reader.GetDateTime(5)
                                });
                                rowCount++;
                            }
                            System.Diagnostics.Debug.WriteLine($"Total performances loaded: {rowCount}");
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки успеваемости: {ex.Message}");
                System.Windows.MessageBox.Show($"Ошибка загрузки успеваемости: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void UpdateAttendance(Attendance attendance)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"]?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                    throw new ConfigurationErrorsException("Connection string 'SchoolDbConnection' not found.");

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "UPDATE Посещаемость SET Присутствие = @presence, Причина = @reason " +
                        "WHERE ID_посещаемость = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("presence", attendance.Presence == "Присутствовал");
                        cmd.Parameters.AddWithValue("reason", attendance.Reason ?? "");
                        cmd.Parameters.AddWithValue("id", attendance.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                UpdateTotals();
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления посещаемости: {ex.Message}");
                System.Windows.MessageBox.Show($"Ошибка обновления посещаемости: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void UpdatePerformance(int studentId, int subjectId, DateTime date, int grade)
        {
            var existingPerformance = Performances.FirstOrDefault(p => p.StudentId == studentId && p.SubjectId == subjectId && p.Date.Date == date.Date);
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"]?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                    throw new ConfigurationErrorsException("Connection string 'SchoolDbConnection' not found.");

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    if (existingPerformance != null)
                    {
                        using (var cmd = new NpgsqlCommand(
                            "UPDATE Успеваемость SET Оценка = @grade " +
                            "WHERE ID_успеваемость = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("grade", grade);
                            cmd.Parameters.AddWithValue("id", existingPerformance.Id);
                            cmd.ExecuteNonQuery();
                            existingPerformance.Grade = grade;
                        }
                    }
                    else
                    {
                        using (var cmd = new NpgsqlCommand(
                            "INSERT INTO Успеваемость (ID_ученик, ID_предмет, Дата, Оценка, Дата_создания) " +
                            "VALUES (@studentId, @subjectId, @date, @grade, @createdAt) RETURNING ID_успеваемость", conn))
                        {
                            cmd.Parameters.AddWithValue("studentId", studentId);
                            cmd.Parameters.AddWithValue("subjectId", subjectId);
                            cmd.Parameters.AddWithValue("date", date.Date);
                            cmd.Parameters.AddWithValue("grade", grade);
                            cmd.Parameters.AddWithValue("createdAt", DateTime.Now);
                            int newId = (int)cmd.ExecuteScalar();

                            Performances.Add(new Performance
                            {
                                Id = newId,
                                StudentId = studentId,
                                SubjectId = subjectId,
                                Date = date.Date,
                                Grade = grade,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                }
                UpdateTotals();
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления успеваемости: {ex.Message}");
                System.Windows.MessageBox.Show($"Ошибка обновления успеваемости: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public int GetTotalAbsencesCount(Student student)
        {
            if (student == null || Attendances == null || SelectedSubject == null) return 0;
            return Attendances.Count(a => a.StudentId == student.Id && a.SubjectId == SelectedSubject.Id && a.Presence == "Отсутствовал");
        }

        public int GetUnexcusedAbsencesCount(Student student)
        {
            if (student == null || Attendances == null || SelectedSubject == null) return 0;
            return Attendances.Count(a => a.StudentId == student.Id && a.SubjectId == SelectedSubject.Id && a.Presence == "Отсутствовал" && a.Reason == "Неуважительная");
        }

        public int GetExcusedAbsencesCount(Student student)
        {
            if (student == null || Attendances == null || SelectedSubject == null) return 0;
            return Attendances.Count(a => a.StudentId == student.Id && a.SubjectId == SelectedSubject.Id && a.Presence == "Отсутствовал" && a.Reason == "Уважительная");
        }

        public double GetAverageGrade(Student student)
        {
            if (student == null || Performances == null || SelectedSubject == null) return 0;
            var grades = Performances.Where(p => p.StudentId == student.Id && p.SubjectId == SelectedSubject.Id)
                                     .Select(p => p.Grade)
                                     .ToList();
            return grades.Any() ? grades.Average() : 0;
        }

        private int GetTotalAbsencesCount()
        {
            if (Students == null || Attendances == null) return 0;
            return Students.Sum(student => GetTotalAbsencesCount(student));
        }

        private int GetTotalUnexcusedAbsencesCount()
        {
            if (Students == null || Attendances == null) return 0;
            return Students.Sum(student => GetUnexcusedAbsencesCount(student));
        }

        private int GetTotalExcusedAbsencesCount()
        {
            if (Students == null || Attendances == null) return 0;
            return Students.Sum(student => GetExcusedAbsencesCount(student));
        }

        private double GetTotalAverageGrade()
        {
            if (Students == null || Performances == null || SelectedSubject == null) return 0;
            var studentAverages = Students.Select(student => GetAverageGrade(student))
                                         .Where(avg => avg > 0)
                                         .ToList();
            return studentAverages.Any() ? studentAverages.Average() : 0;
        }

        private void UpdateTotals()
        {
            TotalAbsencesCount = GetTotalAbsencesCount();
            TotalUnexcusedAbsencesCount = GetTotalUnexcusedAbsencesCount();
            TotalExcusedAbsencesCount = GetTotalExcusedAbsencesCount();
            TotalAverageGrade = GetTotalAverageGrade();
            System.Diagnostics.Debug.WriteLine($"Updated totals: TotalAbsences={TotalAbsencesCount}, UnexcusedAbsences={TotalUnexcusedAbsencesCount}, ExcusedAbsences={TotalExcusedAbsencesCount}, AverageGrade={TotalAverageGrade:F2}");
        }
    }
}
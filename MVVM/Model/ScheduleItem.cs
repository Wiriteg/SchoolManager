using SchoolManager.Core;
using System;

namespace SchoolManager.Models
{
    public class ScheduleItem : ObservableObject
    {
        private readonly Action<string, string, int, bool> _updateAvailability;
        private int _lessonNumber;
        private string _startTime;
        private string _endTime;
        private int _subjectId;
        private int _teacherId;
        private int _classroomId;
        private DateTime _date;
        private bool _isPublished;
        private bool _isReplacement;
        private string _subjectName;

        public ScheduleItem(Action<string, string, int, bool> updateAvailability)
        {
            _updateAvailability = updateAvailability ?? throw new ArgumentNullException(nameof(updateAvailability));
        }

        public int LessonNumber
        {
            get => _lessonNumber;
            set
            {
                if (value < 1 || value > 8)
                    throw new ArgumentOutOfRangeException(nameof(value), "LessonNumber должен быть в диапазоне от 1 до 8.");
                _lessonNumber = value;
                OnPropertyChanged();
            }
        }

        public string SubjectName
        {
            get => _subjectName;
            set
            {
                _subjectName = value;
                OnPropertyChanged();
            }
        }

        public string StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
            }
        }

        public string EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
            }
        }

        public int SubjectId
        {
            get => _subjectId;
            set
            {
                _subjectId = value;
                OnPropertyChanged();
            }
        }

        public int TeacherId
        {
            get => _teacherId;
            set
            {
                _teacherId = value;
                OnPropertyChanged();
            }
        }

        public int ClassroomId
        {
            get => _classroomId;
            set
            {
                _classroomId = value;
                OnPropertyChanged();
            }
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged();
            }
        }

        public bool IsPublished
        {
            get => _isPublished;
            set
            {
                _isPublished = value;
                OnPropertyChanged();
            }
        }

        public bool IsReplacement
        {
            get => _isReplacement;
            set
            {
                _isReplacement = value;
                OnPropertyChanged();
            }
        }

        public string TeacherName
        {
            get => GetTeacherNameById(_teacherId);
            set
            {
                if (value != GetTeacherNameById(_teacherId))
                {
                    if (LessonNumber < 1 || LessonNumber > 8)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка: LessonNumber ({LessonNumber}) вне допустимого диапазона (1–8). Пропускаем обновление доступности преподавателя.");
                    }
                    else
                    {
                        if (_teacherId != 0)
                        {
                            _updateAvailability(GetTeacherNameById(_teacherId), null, LessonNumber, true);
                        }

                        int newTeacherId = GetTeacherIdByName(value);
                        if (newTeacherId != 0)
                        {
                            _updateAvailability(value, null, LessonNumber, false);
                            _teacherId = newTeacherId;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка: Преподаватель с именем '{value}' не найден.");
                        }
                    }

                    OnPropertyChanged();
                }
            }
        }

        public string ClassroomNumber
        {
            get => GetClassroomNumberById(_classroomId);
            set
            {
                if (value != GetClassroomNumberById(_classroomId))
                {
                    if (LessonNumber < 1 || LessonNumber > 8)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка: LessonNumber ({LessonNumber}) вне допустимого диапазона (1–8). Пропускаем обновление доступности кабинета.");
                    }
                    else
                    {
                        if (_classroomId != 0)
                        {
                            _updateAvailability(null, GetClassroomNumberById(_classroomId), LessonNumber, true);
                        }

                        int newClassroomId = GetClassroomIdByNumber(value);
                        if (newClassroomId != 0)
                        {
                            _updateAvailability(null, value, LessonNumber, false);
                            _classroomId = newClassroomId;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка: Кабинет с номером '{value}' не найден.");
                        }
                    }

                    OnPropertyChanged();
                }
            }
        }

        private string GetTeacherNameById(int teacherId)
        {
            if (teacherId <= 0) return null;

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
            using (var connection = new Npgsql.NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new Npgsql.NpgsqlCommand("SELECT CONCAT(Фамилия, ' ', Имя, ' ', Отчество) FROM Преподаватели WHERE ID_преподаватель = @teacherId", connection))
                {
                    command.Parameters.AddWithValue("teacherId", teacherId);
                    var result = command.ExecuteScalar();
                    return result?.ToString();
                }
            }
        }

        private int GetTeacherIdByName(string teacherName)
        {
            if (string.IsNullOrWhiteSpace(teacherName)) return 0;

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
            using (var connection = new Npgsql.NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new Npgsql.NpgsqlCommand("SELECT ID_преподаватель FROM Преподаватели WHERE CONCAT(Фамилия, ' ', Имя, ' ', Отчество) = @teacherName", connection))
                {
                    command.Parameters.AddWithValue("teacherName", teacherName);
                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        private string GetClassroomNumberById(int classroomId)
        {
            if (classroomId <= 0) return null;

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
            using (var connection = new Npgsql.NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new Npgsql.NpgsqlCommand("SELECT Номер_кабинета FROM Кабинеты WHERE ID_кабинет = @classroomId", connection))
                {
                    command.Parameters.AddWithValue("classroomId", classroomId);
                    var result = command.ExecuteScalar();
                    return result?.ToString();
                }
            }
        }

        private int GetClassroomIdByNumber(string classroomNumber)
        {
            if (string.IsNullOrWhiteSpace(classroomNumber)) return 0;

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
            using (var connection = new Npgsql.NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new Npgsql.NpgsqlCommand("SELECT ID_кабинет FROM Кабинеты WHERE Номер_кабинета = @classroomNumber", connection))
                {
                    command.Parameters.AddWithValue("classroomNumber", classroomNumber);
                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }
    }
}
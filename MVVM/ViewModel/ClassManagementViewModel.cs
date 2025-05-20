using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SchoolManager.Core;
using Npgsql;
using System.Configuration;
using System.Linq;
using System;
using SchoolManager.Model;
using System.Text.RegularExpressions;

namespace SchoolManager.MVVM.ViewModel
{
    public class ClassManagementViewModel : ObservableObject
    {
        private ObservableCollection<Class> _classes;
        private ObservableCollection<Student> _students;
        private string _selectedClass;
        private Student _selectedStudent;
        private bool _isPopupOpen;
        private string _popupFirstName;
        private string _popupLastName;
        private string _popupPatronymic;
        private bool _isEditing;
        private bool _isTransferPopupOpen;
        private string _selectedNewClass;

        public ObservableCollection<Class> Classes
        {
            get => _classes;
            set
            {
                _classes = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Student> Students
        {
            get => _students;
            set
            {
                _students = value;
                OnPropertyChanged();
            }
        }

        public string SelectedClass
        {
            get => _selectedClass;
            set
            {
                _selectedClass = value;
                OnPropertyChanged();
                LoadStudents();
            }
        }

        public Student SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                _selectedStudent = value;
                OnPropertyChanged();
            }
        }

        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set
            {
                _isPopupOpen = value;
                OnPropertyChanged();
            }
        }

        public string PopupFirstName
        {
            get => _popupFirstName;
            set
            {
                _popupFirstName = value;
                OnPropertyChanged();
            }
        }

        public string PopupLastName
        {
            get => _popupLastName;
            set
            {
                _popupLastName = value;
                OnPropertyChanged();
            }
        }

        public string PopupPatronymic
        {
            get => _popupPatronymic;
            set
            {
                _popupPatronymic = value;
                OnPropertyChanged();
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged();
            }
        }

        public bool IsTransferPopupOpen
        {
            get => _isTransferPopupOpen;
            set
            {
                _isTransferPopupOpen = value;
                OnPropertyChanged();
            }
        }

        public string SelectedNewClass
        {
            get => _selectedNewClass;
            set
            {
                _selectedNewClass = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddClassCommand { get; set; }
        public ICommand OpenAddStudentPopupCommand { get; set; }
        public ICommand OpenEditStudentPopupCommand { get; set; }
        public ICommand DeleteStudentCommand { get; set; }
        public ICommand SaveStudentCommand { get; set; }
        public ICommand CancelPopupCommand { get; set; }
        public ICommand OpenTransferStudentPopupCommand { get; set; }
        public ICommand TransferStudentCommand { get; set; }
        public ICommand CancelTransferPopupCommand { get; set; }

        public ClassManagementViewModel()
        {
            Classes = new ObservableCollection<Class>();
            Students = new ObservableCollection<Student>();
            LoadClasses();

            AddClassCommand = new RelayCommand(AddClass);
            OpenAddStudentPopupCommand = new RelayCommand(OpenAddStudentPopup);
            OpenEditStudentPopupCommand = new RelayCommand(OpenEditStudentPopup, o => SelectedStudent != null);
            DeleteStudentCommand = new RelayCommand(DeleteStudent, o => SelectedStudent != null);
            SaveStudentCommand = new RelayCommand(SaveStudent);
            CancelPopupCommand = new RelayCommand(CancelPopup);
            OpenTransferStudentPopupCommand = new RelayCommand(OpenTransferStudentPopup, o => SelectedStudent != null);
            TransferStudentCommand = new RelayCommand(TransferStudent);
            CancelTransferPopupCommand = new RelayCommand(CancelTransferPopup);
        }

        private void AddClass(object parameter)
        {
            string newClassName = parameter as string;
            if (string.IsNullOrWhiteSpace(newClassName))
            {
                MessageBox.Show("Название класса не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Classes.Any(c => c.Name == newClassName))
            {
                MessageBox.Show("Класс с таким названием уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("INSERT INTO Классы (Название) VALUES (@name) RETURNING ID_класс", conn))
                    {
                        cmd.Parameters.AddWithValue("name", newClassName);
                        int newId = (int)cmd.ExecuteScalar();

                        Classes.Add(new Class { Id = newId, Name = newClassName });
                        MessageBox.Show("Класс успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    using (var cmd = new NpgsqlCommand("SELECT ID_класс, Название FROM Классы", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Classes.Add(new Class
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }

                var sortedClasses = Classes
                    .OrderBy(c =>
                    {
                        var match = Regex.Match(c.Name, @"\d+");
                        return match.Success ? int.Parse(match.Value) : int.MaxValue;
                    })
                    .ThenBy(c => c.Name)
                    .ToList();

                Classes.Clear();
                foreach (var classItem in sortedClasses)
                {
                    Classes.Add(classItem);
                }

                if (Classes.Any())
                {
                    SelectedClass = Classes.First().Name;
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка загрузки классов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStudents()
        {
            Students.Clear();
            if (string.IsNullOrEmpty(SelectedClass))
                return;

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        @"SELECT s.ID_ученик, s.Имя, s.Фамилия, s.Отчество, k.Название 
                          FROM Ученики s 
                          JOIN Классы k ON s.ID_класс = k.ID_класс 
                          WHERE k.Название = @className 
                          ORDER BY s.Фамилия, s.Имя", conn))
                    {
                        cmd.Parameters.AddWithValue("className", SelectedClass);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Students.Add(new Student
                                {
                                    Id = reader.GetInt32(0),
                                    FirstName = reader.GetString(1),
                                    LastName = reader.GetString(2),
                                    MiddleName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                    ClassName = reader.GetString(4)
                                });
                            }
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка загрузки учеников: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAddStudentPopup(object parameter)
        {
            if (string.IsNullOrEmpty(SelectedClass))
            {
                MessageBox.Show("Сначала выберите класс.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsEditing = false;
            PopupFirstName = string.Empty;
            PopupLastName = string.Empty;
            PopupPatronymic = string.Empty;
            IsPopupOpen = true;
        }

        private void OpenEditStudentPopup(object parameter)
        {
            if (SelectedStudent == null) return;

            IsEditing = true;
            PopupFirstName = SelectedStudent.FirstName;
            PopupLastName = SelectedStudent.LastName;
            PopupPatronymic = SelectedStudent.MiddleName;
            IsPopupOpen = true;
        }

        private void DeleteStudent(object parameter)
        {
            if (SelectedStudent == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите удалить ученика {SelectedStudent.LastName} {SelectedStudent.FirstName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("DELETE FROM Ученики WHERE ID_ученик = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", SelectedStudent.Id);
                        cmd.ExecuteNonQuery();
                    }
                }

                Students.Remove(SelectedStudent);
                MessageBox.Show("Ученик успешно удалён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка удаления ученика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveStudent(object parameter)
        {
            if (string.IsNullOrWhiteSpace(PopupFirstName) || string.IsNullOrWhiteSpace(PopupLastName))
            {
                MessageBox.Show("Имя и фамилия обязательны для заполнения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    int classId;
                    using (var cmd = new NpgsqlCommand("SELECT ID_класс FROM Классы WHERE Название = @className", conn))
                    {
                        cmd.Parameters.AddWithValue("className", SelectedClass);
                        classId = (int)cmd.ExecuteScalar();
                    }

                    if (IsEditing)
                    {
                        using (var cmd = new NpgsqlCommand(
                            "UPDATE Ученики SET Имя = @firstName, Фамилия = @lastName, Отчество = @patronymic WHERE ID_ученик = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("firstName", PopupFirstName);
                            cmd.Parameters.AddWithValue("lastName", PopupLastName);
                            cmd.Parameters.AddWithValue("patronymic", string.IsNullOrEmpty(PopupPatronymic) ? (object)DBNull.Value : PopupPatronymic);
                            cmd.Parameters.AddWithValue("id", SelectedStudent.Id);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                SelectedStudent.FirstName = PopupFirstName;
                                SelectedStudent.LastName = PopupLastName;
                                SelectedStudent.MiddleName = PopupPatronymic;

                                IsPopupOpen = false;
                                MessageBox.Show("Данные ученика обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadStudents();
                            }
                            else
                            {
                                MessageBox.Show("Не удалось обновить данные ученика. Ученик не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    else
                    {
                        using (var cmd = new NpgsqlCommand(
                            "INSERT INTO Ученики (Имя, Фамилия, Отчество, ID_класс) VALUES (@firstName, @lastName, @patronymic, @classId) RETURNING ID_ученик", conn))
                        {
                            cmd.Parameters.AddWithValue("firstName", PopupFirstName);
                            cmd.Parameters.AddWithValue("lastName", PopupLastName);
                            cmd.Parameters.AddWithValue("patronymic", string.IsNullOrEmpty(PopupPatronymic) ? (object)DBNull.Value : PopupPatronymic);
                            cmd.Parameters.AddWithValue("classId", classId);
                            int newId = (int)cmd.ExecuteScalar();

                            IsPopupOpen = false;
                            MessageBox.Show("Ученик успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadStudents();
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка сохранения ученика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelPopup(object parameter)
        {
            IsPopupOpen = false;
        }

        private void OpenTransferStudentPopup(object parameter)
        {
            if (SelectedStudent == null) return;

            IsTransferPopupOpen = true;
            SelectedNewClass = null;
        }

        private void TransferStudent(object parameter)
        {
            if (SelectedStudent == null || string.IsNullOrEmpty(SelectedNewClass))
            {
                MessageBox.Show("Выберите новый класс для перевода.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedNewClass == SelectedStudent.ClassName)
            {
                MessageBox.Show("Ученик уже находится в этом классе.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    int newClassId;
                    using (var cmd = new NpgsqlCommand("SELECT ID_класс FROM Классы WHERE Название = @className", conn))
                    {
                        cmd.Parameters.AddWithValue("className", SelectedNewClass);
                        newClassId = (int)cmd.ExecuteScalar();
                    }

                    using (var cmd = new NpgsqlCommand(
                        "UPDATE Ученики SET ID_класс = @newClassId WHERE ID_ученик = @studentId", conn))
                    {
                        cmd.Parameters.AddWithValue("newClassId", newClassId);
                        cmd.Parameters.AddWithValue("studentId", SelectedStudent.Id);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            SelectedStudent.ClassName = SelectedNewClass;
                            IsTransferPopupOpen = false;
                            MessageBox.Show($"Ученик {SelectedStudent.LastName} {SelectedStudent.FirstName} успешно переведён в класс {SelectedNewClass}.",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadStudents();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось перевести ученика.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка перевода ученика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelTransferPopup(object parameter)
        {
            IsTransferPopupOpen = false;
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SchoolManager.Core;
using Npgsql;
using System.Configuration;
using System.Linq;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SchoolManager.MVVM.ViewModel
{
    public class TeacherManagementViewModel : ObservableObject
    {
        private ObservableCollection<Teacher> _teachers;
        private Teacher _selectedTeacher;
        private bool _isPopupOpen;
        private string _popupLastName;
        private string _popupFirstName;
        private string _popupMiddleName;
        private string _popupPosition;
        private string _popupSpecialization;
        private string _popupWorkingDays;
        private int _popupExperience;
        private string _popupExtracurricular;
        private string _popupEmail;
        private string _popupPassword;
        private bool _isEditing;

        public ObservableCollection<Teacher> Teachers
        {
            get => _teachers;
            set
            {
                _teachers = value;
                OnPropertyChanged();
            }
        }

        public Teacher SelectedTeacher
        {
            get => _selectedTeacher;
            set
            {
                _selectedTeacher = value;
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

        public string PopupLastName
        {
            get => _popupLastName;
            set
            {
                _popupLastName = value;
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

        public string PopupMiddleName
        {
            get => _popupMiddleName;
            set
            {
                _popupMiddleName = value;
                OnPropertyChanged();
            }
        }

        public string PopupPosition
        {
            get => _popupPosition;
            set
            {
                _popupPosition = value;
                OnPropertyChanged();
            }
        }

        public string PopupSpecialization
        {
            get => _popupSpecialization;
            set
            {
                _popupSpecialization = value;
                OnPropertyChanged();
            }
        }

        public string PopupWorkingDays
        {
            get => _popupWorkingDays;
            set
            {
                _popupWorkingDays = value;
                OnPropertyChanged();
            }
        }

        public int PopupExperience
        {
            get => _popupExperience;
            set
            {
                _popupExperience = value;
                OnPropertyChanged();
            }
        }

        public string PopupExtracurricular
        {
            get => _popupExtracurricular;
            set
            {
                _popupExtracurricular = value;
                OnPropertyChanged();
            }
        }

        public string PopupEmail
        {
            get => _popupEmail;
            set
            {
                _popupEmail = value;
                OnPropertyChanged();
            }
        }

        public string PopupPassword
        {
            get => _popupPassword;
            set
            {
                _popupPassword = value;
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

        public ICommand AddTeacherCommand { get; set; }
        public ICommand OpenAddTeacherPopupCommand { get; set; }
        public ICommand OpenEditTeacherPopupCommand { get; set; }
        public ICommand DeleteTeacherCommand { get; set; }
        public ICommand SaveTeacherCommand { get; set; }
        public ICommand CancelPopupCommand { get; set; }

        public TeacherManagementViewModel()
        {
            Teachers = new ObservableCollection<Teacher>();
            LoadTeachers();

            OpenAddTeacherPopupCommand = new RelayCommand(OpenAddTeacherPopup);
            OpenEditTeacherPopupCommand = new RelayCommand(OpenEditTeacherPopup, o => SelectedTeacher != null);
            DeleteTeacherCommand = new RelayCommand(DeleteTeacher, o => SelectedTeacher != null);
            SaveTeacherCommand = new RelayCommand(SaveTeacher);
            CancelPopupCommand = new RelayCommand(CancelPopup);

            AddTeacherCommand = OpenAddTeacherPopupCommand;
        }

        private void LoadTeachers()
        {
            Teachers.Clear();
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT p.ID_преподаватель, p.Фамилия, p.Имя, p.Отчество, p.ID_пользователя, p.Должность, p.Специализация, " +
                        "p.Рабочие_дни, p.Стаж, p.Внеурочная_деятельность, p.Дата_создания, a.Почта, a.Пароль " +
                        "FROM Преподаватели p " +
                        "LEFT JOIN Авторизация a ON p.ID_пользователя = a.ID_пользователя " +
                        "ORDER BY p.Фамилия, p.Имя", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Teachers.Add(new Teacher
                            {
                                Id = reader.GetInt32(0),
                                LastName = reader.GetString(1),
                                FirstName = reader.GetString(2),
                                MiddleName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                UserId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                                Position = reader.GetString(5),
                                Specialization = reader.GetString(6),
                                WorkingDays = reader.GetFieldValue<string[]>(7),
                                Experience = reader.GetInt32(8),
                                Extracurricular = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                                CreatedAt = reader.GetDateTime(10),
                                Email = reader.IsDBNull(11) ? null : reader.GetString(11),
                                PasswordHash = reader.IsDBNull(12) ? null : reader.GetString(12),
                                Password = string.Empty
                            });
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка загрузки преподавателей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAddTeacherPopup(object parameter)
        {
            IsEditing = false;
            PopupLastName = string.Empty;
            PopupFirstName = string.Empty;
            PopupMiddleName = string.Empty;
            PopupPosition = string.Empty;
            PopupSpecialization = string.Empty;
            PopupWorkingDays = string.Empty;
            PopupExperience = 0;
            PopupExtracurricular = string.Empty;
            PopupEmail = string.Empty;
            PopupPassword = string.Empty;
            IsPopupOpen = true;
        }

        private void OpenEditTeacherPopup(object parameter)
        {
            if (SelectedTeacher == null) return;

            IsEditing = true;
            PopupLastName = SelectedTeacher.LastName;
            PopupFirstName = SelectedTeacher.FirstName;
            PopupMiddleName = SelectedTeacher.MiddleName;
            PopupPosition = SelectedTeacher.Position;
            PopupSpecialization = SelectedTeacher.Specialization;
            PopupWorkingDays = SelectedTeacher.WorkingDays != null ? string.Join(", ", SelectedTeacher.WorkingDays) : string.Empty;
            PopupExperience = SelectedTeacher.Experience;
            PopupExtracurricular = SelectedTeacher.Extracurricular;
            PopupEmail = SelectedTeacher.Email ?? string.Empty;
            PopupPassword = string.Empty;
            IsPopupOpen = true;
        }

        private void DeleteTeacher(object parameter)
        {
            if (SelectedTeacher == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите удалить преподавателя {SelectedTeacher.LastName} {SelectedTeacher.FirstName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    if (SelectedTeacher.UserId.HasValue)
                    {
                        using (var cmd = new NpgsqlCommand("DELETE FROM Авторизация WHERE ID_пользователя = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("id", SelectedTeacher.UserId.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (var cmd = new NpgsqlCommand("DELETE FROM Преподаватели WHERE ID_преподаватель = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", SelectedTeacher.Id);
                        cmd.ExecuteNonQuery();
                    }
                }

                Teachers.Remove(SelectedTeacher);
                MessageBox.Show("Преподаватель успешно удалён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка удаления преподавателя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveTeacher(object parameter)
        {
            if (string.IsNullOrWhiteSpace(PopupLastName) || string.IsNullOrWhiteSpace(PopupFirstName) ||
                string.IsNullOrWhiteSpace(PopupPosition) || string.IsNullOrWhiteSpace(PopupSpecialization) ||
                string.IsNullOrWhiteSpace(PopupWorkingDays))
            {
                IsPopupOpen = false;
                MessageBox.Show("Фамилия, имя, должность, специализация и рабочие дни обязательны для заполнения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string experienceInput = PopupExperience.ToString();
            if (string.IsNullOrWhiteSpace(experienceInput) || !int.TryParse(experienceInput, out int experience) || experience < 0)
            {
                IsPopupOpen = false;
                MessageBox.Show("Поле 'Стаж' должно содержать только положительное целое число и не может быть пустым.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string[] workingDaysArray = PopupWorkingDays.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(day => day.Trim()).ToArray();

            if (!workingDaysArray.Any())
            {
                IsPopupOpen = false;
                MessageBox.Show("Укажите хотя бы один рабочий день.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    int userId = SelectedTeacher?.UserId ?? 0;
                    string hashedPassword = null;

                    if (!string.IsNullOrEmpty(PopupEmail) && !string.IsNullOrEmpty(PopupPassword))
                    {
                        hashedPassword = BCryptNet.HashPassword(PopupPassword);

                        if (IsEditing && userId > 0)
                        {
                            string updateAuthQuery = @"
                                UPDATE Авторизация
                                SET Почта = @email, Пароль = COALESCE(@password, Пароль)
                                WHERE ID_пользователя = @userId";
                            using (var cmd = new NpgsqlCommand(updateAuthQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("email", PopupEmail);
                                cmd.Parameters.AddWithValue("password", (object)hashedPassword ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("userId", userId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string insertAuthQuery = @"
                                INSERT INTO Авторизация (Почта, Пароль)
                                VALUES (@email, @password)
                                RETURNING ID_пользователя";
                            using (var cmd = new NpgsqlCommand(insertAuthQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("email", PopupEmail);
                                cmd.Parameters.AddWithValue("password", hashedPassword);
                                userId = (int)cmd.ExecuteScalar();
                            }
                        }
                    }

                    if (IsEditing)
                    {
                        using (var cmd = new NpgsqlCommand(
                            "UPDATE Преподаватели SET Фамилия = @lastName, Имя = @firstName, Отчество = @middleName, " +
                            "ID_пользователя = @userId, Должность = @position, Специализация = @specialization, Рабочие_дни = @workingDays, " +
                            "Стаж = @experience, Внеурочная_деятельность = @extracurricular " +
                            "WHERE ID_преподаватель = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("lastName", PopupLastName);
                            cmd.Parameters.AddWithValue("firstName", PopupFirstName);
                            cmd.Parameters.AddWithValue("middleName", string.IsNullOrEmpty(PopupMiddleName) ? (object)DBNull.Value : PopupMiddleName);
                            cmd.Parameters.AddWithValue("userId", userId == 0 ? (object)DBNull.Value : userId);
                            cmd.Parameters.AddWithValue("position", PopupPosition);
                            cmd.Parameters.AddWithValue("specialization", PopupSpecialization);
                            cmd.Parameters.AddWithValue("workingDays", workingDaysArray);
                            cmd.Parameters.AddWithValue("experience", PopupExperience);
                            cmd.Parameters.AddWithValue("extracurricular", string.IsNullOrEmpty(PopupExtracurricular) ? (object)DBNull.Value : PopupExtracurricular);
                            cmd.Parameters.AddWithValue("id", SelectedTeacher.Id);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                SelectedTeacher.LastName = PopupLastName;
                                SelectedTeacher.FirstName = PopupFirstName;
                                SelectedTeacher.MiddleName = PopupMiddleName;
                                SelectedTeacher.UserId = userId == 0 ? null : userId;
                                SelectedTeacher.Position = PopupPosition;
                                SelectedTeacher.Specialization = PopupSpecialization;
                                SelectedTeacher.WorkingDays = workingDaysArray;
                                SelectedTeacher.Experience = PopupExperience;
                                SelectedTeacher.Extracurricular = PopupExtracurricular;
                                SelectedTeacher.Email = PopupEmail;
                                SelectedTeacher.PasswordHash = hashedPassword;

                                IsPopupOpen = false;
                                MessageBox.Show("Данные преподавателя обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadTeachers();
                            }
                            else
                            {
                                IsPopupOpen = false;
                                MessageBox.Show("Не удалось обновить данные преподавателя. Преподаватель не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    else
                    {
                        using (var cmd = new NpgsqlCommand(
                            "INSERT INTO Преподаватели (Фамилия, Имя, Отчество, ID_пользователя, Должность, Специализация, " +
                            "Рабочие_дни, Стаж, Внеурочная_деятельность, Дата_создания) " +
                            "VALUES (@lastName, @firstName, @middleName, @userId, @position, @specialization, " +
                            "@workingDays, @experience, @extracurricular, @createdAt) RETURNING ID_преподаватель", conn))
                        {
                            cmd.Parameters.AddWithValue("lastName", PopupLastName);
                            cmd.Parameters.AddWithValue("firstName", PopupFirstName);
                            cmd.Parameters.AddWithValue("middleName", string.IsNullOrEmpty(PopupMiddleName) ? (object)DBNull.Value : PopupMiddleName);
                            cmd.Parameters.AddWithValue("userId", userId == 0 ? (object)DBNull.Value : userId);
                            cmd.Parameters.AddWithValue("position", PopupPosition);
                            cmd.Parameters.AddWithValue("specialization", PopupSpecialization);
                            cmd.Parameters.AddWithValue("workingDays", workingDaysArray);
                            cmd.Parameters.AddWithValue("experience", PopupExperience);
                            cmd.Parameters.AddWithValue("extracurricular", string.IsNullOrEmpty(PopupExtracurricular) ? (object)DBNull.Value : PopupExtracurricular);
                            cmd.Parameters.AddWithValue("createdAt", DateTime.Now);
                            int newId = (int)cmd.ExecuteScalar();

                            IsPopupOpen = false;
                            MessageBox.Show("Преподаватель успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadTeachers();
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                IsPopupOpen = false;
                MessageBox.Show($"Ошибка сохранения преподавателя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelPopup(object parameter)
        {
            IsPopupOpen = false;
        }
    }

    public class Teacher
    {
        public int Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public int? UserId { get; set; }
        public string Position { get; set; }
        public string Specialization { get; set; }
        public string[] WorkingDays { get; set; }
        public int Experience { get; set; }
        public string Extracurricular { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
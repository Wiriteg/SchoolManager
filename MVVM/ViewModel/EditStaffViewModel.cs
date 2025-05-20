using Npgsql;
using SchoolManager.Core;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Windows;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SchoolManager.MVVM.ViewModel
{
    public class EditStaffViewModel : ObservableObject
    {
        private ObservableCollection<StaffEntry> _staffEntries;
        private StaffEntry _selectedStaffEntry;
        private bool _isPopupOpen;
        private bool _isEditing;
        private string _popupLastName;
        private string _popupFirstName;
        private string _popupMiddleName;
        private string _popupRole;
        private int _popupExperience;
        private string _popupEmail;
        private string _popupPassword;
        private RelayCommand _openAddStaffPopupCommand;
        private RelayCommand _openEditStaffPopupCommand;
        private RelayCommand _saveStaffPopupCommand;
        private RelayCommand _cancelPopupCommand;
        private RelayCommand _deleteStaffCommand;
        private readonly MainViewModel _mainViewModel;

        public ObservableCollection<StaffEntry> StaffEntries
        {
            get => _staffEntries;
            set
            {
                _staffEntries = value;
                OnPropertyChanged();
            }
        }

        public StaffEntry SelectedStaffEntry
        {
            get => _selectedStaffEntry;
            set
            {
                _selectedStaffEntry = value;
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

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
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

        public string PopupRole
        {
            get => _popupRole;
            set
            {
                _popupRole = value;
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

        public RelayCommand OpenAddStaffPopupCommand => _openAddStaffPopupCommand ??= new RelayCommand(obj => OpenAddStaffPopup());
        public RelayCommand OpenEditStaffPopupCommand => _openEditStaffPopupCommand ??= new RelayCommand(obj => OpenEditStaffPopup());
        public RelayCommand SaveStaffPopupCommand => _saveStaffPopupCommand ??= new RelayCommand(obj => SaveStaffPopup());
        public RelayCommand CancelPopupCommand => _cancelPopupCommand ??= new RelayCommand(obj => CancelPopup());
        public RelayCommand DeleteStaffCommand => _deleteStaffCommand ??= new RelayCommand(obj => DeleteStaff());

        public EditStaffViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            StaffEntries = new ObservableCollection<StaffEntry>();
            LoadStaffEntries();
        }

        private void LoadStaffEntries()
        {
            StaffEntries.Clear();
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                string query = @"
                    SELECT s.ID_сотрудник, s.Фамилия, s.Имя, s.Отчество, s.ID_пользователя, s.Роль, s.Стаж,
                           a.Почта, a.Пароль
                    FROM Сотрудники s
                    LEFT JOIN Авторизация a ON s.ID_пользователя = a.ID_пользователя
                    ORDER BY s.Фамилия";

                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var entry = new StaffEntry
                    {
                        StaffId = reader.GetInt32(0),
                        LastName = reader.GetString(1),
                        FirstName = reader.GetString(2),
                        MiddleName = reader.GetString(3),
                        UserId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                        Role = reader.GetString(5),
                        Experience = reader.GetInt32(6),
                        Email = reader.IsDBNull(7) ? null : reader.GetString(7),
                        PasswordHash = reader.IsDBNull(8) ? null : reader.GetString(8),
                        Password = string.Empty
                    };
                    StaffEntries.Add(entry);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке сотрудников: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAddStaffPopup()
        {
            IsEditing = false;
            ClearPopupFields();
            IsPopupOpen = true;
        }

        private void OpenEditStaffPopup()
        {
            if (SelectedStaffEntry == null)
            {
                MessageBox.Show("Выберите сотрудника для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsEditing = true;
            PopupLastName = SelectedStaffEntry.LastName;
            PopupFirstName = SelectedStaffEntry.FirstName;
            PopupMiddleName = SelectedStaffEntry.MiddleName;
            PopupRole = SelectedStaffEntry.Role;
            PopupExperience = SelectedStaffEntry.Experience;
            PopupEmail = SelectedStaffEntry.Email ?? string.Empty;
            PopupPassword = string.Empty;
            IsPopupOpen = true;
        }

        private void SaveStaffPopup()
        {
            if (string.IsNullOrWhiteSpace(PopupLastName) || string.IsNullOrWhiteSpace(PopupFirstName) || string.IsNullOrWhiteSpace(PopupRole))
            {
                MessageBox.Show("Пожалуйста, заполните обязательные поля (Фамилия, Имя, Роль).", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                int userId = SelectedStaffEntry?.UserId ?? 0;
                string hashedPassword = null;

                if (!string.IsNullOrEmpty(PopupPassword))
                {
                    hashedPassword = BCryptNet.HashPassword(PopupPassword);
                }

                if (IsEditing)
                {
                    if (userId > 0)
                    {
                        string updateAuthQuery = @"
                            UPDATE Авторизация
                            SET Почта = @email, Пароль = COALESCE(@password, Пароль)
                            WHERE ID_пользователя = @userId";
                        using var cmd = new NpgsqlCommand(updateAuthQuery, conn);
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
                    using var cmd = new NpgsqlCommand(insertAuthQuery, conn);
                    cmd.Parameters.AddWithValue("email", PopupEmail);
                    cmd.Parameters.AddWithValue("password", BCryptNet.HashPassword(PopupPassword));
                    userId = (int)cmd.ExecuteScalar();
                }

                if (IsEditing)
                {
                    string updateStaffQuery = @"
                        UPDATE Сотрудники
                        SET Фамилия = @lastName, Имя = @firstName, Отчество = @middleName, Роль = @role, Стаж = @experience
                        WHERE ID_сотрудник = @staffId";
                    using var cmd = new NpgsqlCommand(updateStaffQuery, conn);
                    cmd.Parameters.AddWithValue("lastName", PopupLastName);
                    cmd.Parameters.AddWithValue("firstName", PopupFirstName);
                    cmd.Parameters.AddWithValue("middleName", PopupMiddleName);
                    cmd.Parameters.AddWithValue("role", PopupRole);
                    cmd.Parameters.AddWithValue("experience", PopupExperience);
                    cmd.Parameters.AddWithValue("staffId", SelectedStaffEntry.StaffId);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    string insertStaffQuery = @"
                        INSERT INTO Сотрудники (Фамилия, Имя, Отчество, ID_пользователя, Роль, Стаж)
                        VALUES (@lastName, @firstName, @middleName, @userId, @role, @experience)";
                    using var cmd = new NpgsqlCommand(insertStaffQuery, conn);
                    cmd.Parameters.AddWithValue("lastName", PopupLastName);
                    cmd.Parameters.AddWithValue("firstName", PopupFirstName);
                    cmd.Parameters.AddWithValue("middleName", PopupMiddleName);
                    cmd.Parameters.AddWithValue("userId", userId);
                    cmd.Parameters.AddWithValue("role", PopupRole);
                    cmd.Parameters.AddWithValue("experience", PopupExperience);
                    cmd.ExecuteNonQuery();
                }
                IsPopupOpen = false;
                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadStaffEntries();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelPopup()
        {
            IsPopupOpen = false;
        }

        private void DeleteStaff()
        {
            if (SelectedStaffEntry == null)
            {
                MessageBox.Show("Выберите сотрудника для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить сотрудника {SelectedStaffEntry.LastName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                if (SelectedStaffEntry.UserId.HasValue)
                {
                    var cmd = new NpgsqlCommand("DELETE FROM Авторизация WHERE ID_пользователя = @id", conn);
                    cmd.Parameters.AddWithValue("id", SelectedStaffEntry.UserId.Value);
                    cmd.ExecuteNonQuery();
                }

                var cmd2 = new NpgsqlCommand("DELETE FROM Сотрудники WHERE ID_сотрудник = @id", conn);
                cmd2.Parameters.AddWithValue("id", SelectedStaffEntry.StaffId);
                cmd2.ExecuteNonQuery();

                MessageBox.Show("Сотрудник удалён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadStaffEntries();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearPopupFields()
        {
            PopupLastName = string.Empty;
            PopupFirstName = string.Empty;
            PopupMiddleName = string.Empty;
            PopupRole = string.Empty;
            PopupExperience = 0;
            PopupEmail = string.Empty;
            PopupPassword = string.Empty;
        }

        public class StaffEntry
        {
            public int StaffId { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
            public int? UserId { get; set; }
            public string Role { get; set; }
            public int Experience { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string PasswordHash { get; set; }
        }
    }
}
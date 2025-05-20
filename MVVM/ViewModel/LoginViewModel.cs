using Npgsql;
using SchoolManager.Core;
using SchoolManager.MVVM.View;
using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace SchoolManager.MVVM.ViewModel
{
    class LoginViewModel : ObservableObject
    {
        private string _email;
        private string _password;
        private bool _isLoginButtonEnabled;

        private bool VerifyPassword(string enteredPassword, string storedHashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(enteredPassword, storedHashedPassword);
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
                UpdateLoginButtonState();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
                UpdateLoginButtonState();
                System.Diagnostics.Debug.WriteLine($"Password updated: {_password}, IsLoginButtonEnabled: {IsLoginButtonEnabled}");
            }
        }

        public bool IsLoginButtonEnabled
        {
            get => _isLoginButtonEnabled;
            set
            {
                _isLoginButtonEnabled = value;
                OnPropertyChanged(nameof(IsLoginButtonEnabled));
                System.Diagnostics.Debug.WriteLine($"IsLoginButtonEnabled updated: {IsLoginButtonEnabled}");
            }
        }

        public ICommand LoginCommand { get; set; }

        private readonly MainViewModel _mainViewModel;
        private readonly LoginView _loginView;

        public LoginViewModel(MainViewModel mainViewModel, LoginView loginView)
        {
            _mainViewModel = mainViewModel;
            _loginView = loginView;
            LoginCommand = new RelayCommand(o => Login(), CanLogin);
            UpdateLoginButtonState();
        }

        private bool CanLogin(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   IsValidEmail(Email);
        }

        private void Login()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new NpgsqlCommand("SELECT ID_пользователя, Пароль FROM Авторизация WHERE Почта = @Email", conn);
                    cmd.Parameters.AddWithValue("Email", Email);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var userId = reader["ID_пользователя"];
                            string storedHashedPassword = reader["Пароль"].ToString();

                            if (VerifyPassword(Password, storedHashedPassword))
                            {
                                Logger.Log($"Пользователь {Email} успешно вошел в систему.");
                                reader.Close();

                                cmd.CommandText = "SELECT Роль, Фамилия, Имя, Отчество FROM Сотрудники WHERE ID_пользователя = @userId";
                                cmd.Parameters.Clear();
                                cmd.Parameters.AddWithValue("userId", userId);
                                using (var roleReader = cmd.ExecuteReader())
                                {
                                    if (roleReader.Read())
                                    {
                                        string role = roleReader["Роль"].ToString().Trim();
                                        string lastName = roleReader["Фамилия"].ToString();
                                        string firstName = roleReader["Имя"].ToString();
                                        string middleName = roleReader["Отчество"]?.ToString() ?? "";

                                        System.Diagnostics.Debug.WriteLine($"Before OnLoginSuccess (Employee): Role='{role}' (length: {role.Length}), LastName={lastName}, FirstName={firstName}, MiddleName={middleName}");
                                        _mainViewModel.OnLoginSuccess(role, lastName, firstName, middleName);
                                        MessageBox.Show("Добро пожаловать!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                        _loginView.Close();
                                    }
                                    else
                                    {
                                        roleReader.Close();
                                        cmd.CommandText = "SELECT Фамилия, Имя, Отчество FROM Преподаватели WHERE ID_пользователя = @userId";
                                        cmd.Parameters.Clear();
                                        cmd.Parameters.AddWithValue("userId", userId);
                                        using (var teacherReader = cmd.ExecuteReader())
                                        {
                                            if (teacherReader.Read())
                                            {
                                                string role = "Преподаватель";
                                                string lastName = teacherReader["Фамилия"].ToString();
                                                string firstName = teacherReader["Имя"].ToString();
                                                string middleName = teacherReader["Отчество"]?.ToString() ?? "";

                                                System.Diagnostics.Debug.WriteLine($"Before OnLoginSuccess (Teacher): Role='{role}' (length: {role.Length}), LastName={lastName}, FirstName={firstName}, MiddleName={middleName}");
                                                _mainViewModel.OnLoginSuccess(role, lastName, firstName, middleName);
                                                MessageBox.Show("Добро пожаловать!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                                _loginView.Close();
                                            }
                                            else
                                            {
                                                MessageBox.Show("Доступ разрешён только сотрудникам или преподавателям!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Logger.Log($"Неудачная попытка входа для пользователя {Email}: неверный пароль.");
                                MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            Logger.Log($"Неудачная попытка входа для пользователя {Email}: пользователь не найден.");
                            MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Ошибка при входе: {ex.Message}");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OnGoogleLoginSuccess(string role, string lastName, string firstName, string middleName)
        {
            role = role?.Trim();
            System.Diagnostics.Debug.WriteLine($"OnGoogleLoginSuccess: Role='{role}' (length: {role?.Length ?? 0})");
            _mainViewModel.OnLoginSuccess(role, lastName, firstName, middleName);
            _loginView.Close();
        }

        private void UpdateLoginButtonState()
        {
            bool isEmailValid = IsValidEmail(Email);
            System.Diagnostics.Debug.WriteLine($"Email: {Email}, Password: {Password}, IsEmailValid: {isEmailValid}");
            IsLoginButtonEnabled = !string.IsNullOrWhiteSpace(Email) &&
                                  !string.IsNullOrWhiteSpace(Password) &&
                                  isEmailValid;
            (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{3,}$");
        }
    }
}
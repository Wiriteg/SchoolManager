using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Npgsql;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Windows;
using System.Windows.Input;
using System.Configuration;
using System.Windows.Controls;
using Google;
using System.Net.Http;

namespace SchoolManager.MVVM.View
{
    public partial class LoginView : Window
    {
        private static readonly string[] Scopes = { "profile", "email" };
        private static readonly string ApplicationName = "Schedule";

        public LoginView()
        {
            InitializeComponent();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModel.LoginViewModel viewModel)
            {
                viewModel.Password = (sender as PasswordBox)?.Password;
            }
        }

        private async void GoogleLoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = "502904959367-t72imchnab2ssfdpf7j32ipuuvu3j5uf.apps.googleusercontent.com",
                        ClientSecret = "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"
                    },
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore("SchoolManager.MVVM"));

                if (credential != null)
                {
                    var service = new Oauth2Service(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ApplicationName
                    });

                    var request = service.Userinfo.Get();
                    var userInfo = await request.ExecuteAsync();

                    if (userInfo != null)
                    {
                        await AuthenticateWithDatabase(userInfo.Email, userInfo.Name);
                    }
                    else
                    {
                        ShowCustomMessage("Не удалось получить данные пользователя. Попробуйте снова.", "Ошибка авторизации");
                    }
                }
            }
            catch (HttpRequestException)
            {
                ShowCustomMessage("Отсутствует подключение к интернету. Проверьте соединение и повторите попытку.", "Ошибка сети");
            }
            catch (GoogleApiException ex)
            {
                ShowCustomMessage($"Ошибка Google API: {ex.Message}", "Ошибка авторизации");
            }
            catch (Exception ex)
            {
                ShowCustomMessage($"Произошла непредвиденная ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void ShowCustomMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private Task AuthenticateWithDatabase(string email, string name)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new NpgsqlCommand("SELECT ID_пользователя FROM Авторизация WHERE Почта = @email", conn);
                    cmd.Parameters.AddWithValue("email", email);
                    var userId = cmd.ExecuteScalar();

                    if (userId != null)
                    {
                        cmd.CommandText = "SELECT Роль, Фамилия, Имя, Отчество FROM Сотрудники WHERE ID_пользователя = @userId";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string role = reader["Роль"].ToString();
                                string lastName = reader["Фамилия"].ToString();
                                string firstName = reader["Имя"].ToString();
                                string middleName = reader["Отчество"]?.ToString() ?? "";

                                if (DataContext is ViewModel.LoginViewModel viewModel)
                                {
                                    viewModel.OnGoogleLoginSuccess(role, lastName, firstName, middleName);
                                    this.Close();
                                }
                                MessageBox.Show("Добро пожаловать!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show("Доступ разрешён только сотрудникам!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Пользователь с данной почтой не зарегистрирован. Обратитесь к администратору.", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return Task.CompletedTask;
        }
    }
}
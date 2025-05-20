using Npgsql;
using SchoolManager.Core;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Windows;
using System.Windows.Input;

namespace SchoolManager.MVVM.ViewModel
{
    public class HomeViewModel : ObservableObject
    {
        private string _lastPublishedMessage;
        public string LastPublishedMessage
        {
            get => _lastPublishedMessage;
            set
            {
                _lastPublishedMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand SwitchToScheduleCommand { get; set; }
        public ICommand SwitchToEditScheduleCommand { get; set; }
        public ICommand SwitchToClassManagementCommand { get; set; }
        public ICommand SwitchToTeacherManagementCommand { get; set; }
        public ICommand SwitchToReportGenerationCommand { get; set; }
        public ICommand SwitchToStaffManagementCommand { get; set; }
        public ICommand SwitchToEditScoreCommand { get; set; }
        public int TotalLessons
        {
            get => _totalLessons;
            set
            {
                _totalLessons = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<string> UpcomingLessons
        {
            get => _upcomingLessons;
            set
            {
                _upcomingLessons = value;
                OnPropertyChanged();
            }
        }

        private readonly MainViewModel _mainViewModel;
        private int _totalLessons;
        private ObservableCollection<string> _upcomingLessons;
        public bool IsGuestPanelVisible => true;
        public bool IsTeacherPanelVisible => _mainViewModel.CanAccessTeacherFeatures;
        public bool IsStaffPanelVisible => _mainViewModel.CanAccessEmployeeFeatures;
        public bool IsAdminPanelVisible => _mainViewModel.CanAccessAdminFeatures;

        public HomeViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoadLastPublishedDate();

            SwitchToScheduleCommand = new RelayCommand(o =>
            {
                try
                {
                    _mainViewModel.ScheduleViewCommand.Execute(null);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при переключении на вкладку Schedule: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            });

            SwitchToEditScheduleCommand = new RelayCommand(o =>
            {
                try
                {
                    _mainViewModel.EditScheduleViewCommand.Execute(null);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при переключении на вкладку EditSchedule: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            });

            SwitchToClassManagementCommand = new RelayCommand(o =>
            {
                try
                {
                    _mainViewModel.ClassManagementViewCommand.Execute(null);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при переключении на вкладку Class Management: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            });

            SwitchToTeacherManagementCommand = new RelayCommand(o =>
            {
                try
                {
                    _mainViewModel.TeacherManagementViewCommand.Execute(null);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при переключении на вкладку Teacher Management: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            });

            SwitchToStaffManagementCommand = new RelayCommand(o =>
            {
                try
                {
                    _mainViewModel.EditStaffViewCommand.Execute(null);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при переключении на вкладку Staff Management: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            });

            SwitchToEditScoreCommand = new RelayCommand(o =>
            {
                try
                {
                    _mainViewModel.PerformanceAttendanceViewCommand.Execute(null);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при переключении на вкладку Edit Score: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            });
        }

        public void LoadLastPublishedDate()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"]?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    System.Diagnostics.Debug.WriteLine("Ошибка: строка подключения 'SchoolDbConnection' не найдена в App.config.");
                    LastPublishedMessage = "Download error";
                    return;
                }

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new NpgsqlCommand("SELECT DISTINCT Дата FROM Расписание WHERE Опубликовано = TRUE ORDER BY Дата DESC LIMIT 1", conn);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        var date = (DateTime)result;
                        LastPublishedMessage = $"Опубликовано на: {date.ToString("dd.MM.yyyy")}";
                    }
                    else
                    {
                        LastPublishedMessage = "Schedule has not been published";
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка базы данных в HomeViewModel: {ex.Message}");
                LastPublishedMessage = "Download error";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Неизвестная ошибка в HomeViewModel: {ex.Message}");
                LastPublishedMessage = "Download error";
            }
        }

        public void LoadStatistics()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SchoolDbConnection"].ConnectionString;
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM Расписание WHERE Дата >= CURRENT_DATE", conn))
                    {
                        TotalLessons = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    using (var cmd = new NpgsqlCommand("SELECT Название FROM Предметы LIMIT 5", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        UpcomingLessons = new ObservableCollection<string>();
                        while (reader.Read())
                        {
                            UpcomingLessons.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
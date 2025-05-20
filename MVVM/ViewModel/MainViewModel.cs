using SchoolManager.Core;
using SchoolManager.Models;
using SchoolManager.MVVM.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SchoolManager.MVVM.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        private static readonly string[] EmployeeRoles = { "Директор", "Заместитель директора по УВР", "Заместитель директора по ВР" };
        private static readonly string AdminRole = "Администратор";
        private static readonly string TeacherRole = "Преподаватель";

        public bool CanAccessAdminFeatures => UserRole == AdminRole;
        public bool CanAccessEmployeeFeatures => EmployeeRoles.Contains(UserRole) || UserRole == AdminRole;
        public bool CanAccessTeacherFeatures => UserRole == TeacherRole || CanAccessEmployeeFeatures;
        public bool CanAccessGuestFeatures => true;

        public bool IsStudentPanelVisible => true;
        public bool IsTeacherPanelVisible => CanAccessTeacherFeatures;
        public bool IsStaffPanelVisible => CanAccessEmployeeFeatures;
        public bool IsAdminPanelVisible => CanAccessAdminFeatures;

        public HomeViewModel HomeVM { get; set; }
        public ScheduleViewModel ScheduleVM { get; set; }
        public EditScheduleViewModel EditScheduleVM { get; set; }
        public EditStaffViewModel EditStaffVM { get; set; }
        public ClassManagementViewModel ClassManagementVM { get; set; }
        public TeacherManagementViewModel TeacherManagementVM { get; set; }
        public PerformanceAttendanceViewModel PerformanceAttendanceVM { get; set; }

        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand ScheduleViewCommand { get; set; }
        public RelayCommand EditScheduleViewCommand { get; set; }
        public RelayCommand EditStaffViewCommand { get; set; }
        public RelayCommand ClassManagementViewCommand { get; set; }
        public RelayCommand TeacherManagementViewCommand { get; set; }
        public RelayCommand ReportGenerationViewCommand { get; set; }
        public RelayCommand PerformanceAttendanceViewCommand { get; set; }
        public RelayCommand LoginCommand { get; set; }
        public RelayCommand LogoutCommand { get; set; }
        public RelayCommand CloseUserProfileCommand { get; set; }
        public ObservableCollection<MenuButtonItem> MenuButtons { get; set; }

        private readonly HomeView _homeView;
        private readonly ScheduleView _scheduleView;
        private readonly EditScheduleView _editScheduleView;
        private readonly EditStaffView _editStaffView;
        private readonly ClassManagementView _classManagementView;
        private readonly TeacherManagementView _teacherManagementView;
        public readonly PerformanceAttendanceView _performanceAttendanceView;

        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
                if (_currentView == _editScheduleView && ScheduleVM.SelectedDate.HasValue)
                {
                    EditScheduleVM.SelectedDate = ScheduleVM.SelectedDate;
                }
                else if (_currentView == _scheduleView && EditScheduleVM.SelectedDate.HasValue)
                {
                    ScheduleVM.SelectedDate = EditScheduleVM.SelectedDate;
                    ScheduleVM.RefreshSchedule();
                }
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                _isLoggedIn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAccessAdminFeatures));
                OnPropertyChanged(nameof(CanAccessEmployeeFeatures));
                OnPropertyChanged(nameof(CanAccessTeacherFeatures));
                OnPropertyChanged(nameof(CanAccessGuestFeatures));
                OnPropertyChanged(nameof(IsTeacherPanelVisible));
                OnPropertyChanged(nameof(IsStaffPanelVisible));
                OnPropertyChanged(nameof(IsAdminPanelVisible));
                (EditScheduleViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (EditStaffViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
                UpdateMenuItems();
            }
        }

        private string _userRole;
        public string UserRole
        {
            get => _userRole;
            set
            {
                _userRole = value?.Trim();
                System.Diagnostics.Debug.WriteLine($"UserRole set to: {_userRole ?? "null"}, CanAccessAdminFeatures: {CanAccessAdminFeatures}, IsAdminPanelVisible: {IsAdminPanelVisible}");
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAccessAdminFeatures));
                OnPropertyChanged(nameof(CanAccessEmployeeFeatures));
                OnPropertyChanged(nameof(CanAccessTeacherFeatures));
                OnPropertyChanged(nameof(CanAccessGuestFeatures));
                OnPropertyChanged(nameof(IsTeacherPanelVisible));
                OnPropertyChanged(nameof(IsStaffPanelVisible));
                OnPropertyChanged(nameof(IsAdminPanelVisible));
                UpdateMenuItems();
                (EditScheduleViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (EditStaffViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _lastName;
        public string LastName
        {
            get => _lastName;
            set
            {
                _lastName = value;
                OnPropertyChanged();
            }
        }

        private string _firstName;
        public string FirstName
        {
            get => _firstName;
            set
            {
                _firstName = value;
                OnPropertyChanged();
            }
        }

        private string _middleName;
        public string MiddleName
        {
            get => _middleName;
            set
            {
                _middleName = value;
                OnPropertyChanged();
            }
        }

        private bool _isUserProfileVisible;
        public bool IsUserProfileVisible
        {
            get => _isUserProfileVisible;
            set
            {
                _isUserProfileVisible = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<MenuItemViewModel> _menuItems;
        public ObservableCollection<MenuItemViewModel> MenuItems
        {
            get => _menuItems;
            set
            {
                _menuItems = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            HomeVM = new HomeViewModel(this);
            ScheduleVM = new ScheduleViewModel();
            EditScheduleVM = new EditScheduleViewModel();
            EditStaffVM = new EditStaffViewModel(this);
            ClassManagementVM = new ClassManagementViewModel();
            TeacherManagementVM = new TeacherManagementViewModel();
            PerformanceAttendanceVM = new PerformanceAttendanceViewModel(this);

            _homeView = new HomeView { DataContext = HomeVM };
            _scheduleView = new ScheduleView { DataContext = ScheduleVM };
            _editScheduleView = new EditScheduleView { DataContext = EditScheduleVM };
            _editStaffView = new EditStaffView(this);
            _classManagementView = new ClassManagementView { DataContext = ClassManagementVM };
            _teacherManagementView = new TeacherManagementView { DataContext = TeacherManagementVM };
            _performanceAttendanceView = new PerformanceAttendanceView(this);

            CurrentView = _homeView;
            IsLoggedIn = false;
            UserRole = null;
            IsUserProfileVisible = false;

            System.Diagnostics.Debug.WriteLine($"MainViewModel initialized. UserRole: {UserRole ?? "null"}");

            HomeViewCommand = new RelayCommand(o =>
            {
                CurrentView = _homeView;
                UpdateRadioButtons("HomeRadioButton");
            });

            ScheduleViewCommand = new RelayCommand(o =>
            {
                CurrentView = _scheduleView;
                UpdateRadioButtons("ScheduleRadioButton");
            });

            EditScheduleViewCommand = new RelayCommand(o =>
            {
                CurrentView = _editScheduleView;
                EditScheduleVM.Initialize();
                UpdateRadioButtons("EditScheduleRadioButton");
            }, CanAccessEmployeeOrAdmin);

            EditStaffViewCommand = new RelayCommand(o =>
            {
                CurrentView = _editStaffView;
                UpdateRadioButtons("EditStaffRadioButton");
            }, CanAccessAdminOnly);

            ClassManagementViewCommand = new RelayCommand(o =>
            {
                CurrentView = _classManagementView;
                UpdateRadioButtons("ClassManagementRadioButton");
            }, CanAccessEmployeeOrAdmin);

            TeacherManagementViewCommand = new RelayCommand(o =>
            {
                CurrentView = _teacherManagementView;
                UpdateRadioButtons("TeacherManagementRadioButton");
            }, CanAccessEmployeeOrAdmin);

            PerformanceAttendanceViewCommand = new RelayCommand(o =>
            {
                CurrentView = _performanceAttendanceView;
                UpdateRadioButtons("PerformanceAttendanceRadioButton");
            }, CanAccessTeacherOrAbove);

            ReportGenerationViewCommand = new RelayCommand(o =>
            {
            }, CanAccessAdminOnly);

            LoginCommand = new RelayCommand(o =>
            {
                if (IsLoggedIn)
                {
                    IsUserProfileVisible = true;
                }
                else
                {
                    OpenLoginWindow(o);
                }
            });

            LogoutCommand = new RelayCommand(o =>
            {
                IsLoggedIn = false;
                UserRole = null;
                LastName = null;
                FirstName = null;
                MiddleName = null;
                IsUserProfileVisible = false;
                CurrentView = _homeView;
                UpdateRadioButtons("HomeRadioButton");
            });

            CloseUserProfileCommand = new RelayCommand(o =>
            {
                IsUserProfileVisible = false;
            });

            InitializeMenuItems();
        }

        private void InitializeMenuItems()
        {
            MenuItems = new ObservableCollection<MenuItemViewModel>
            {
                new MenuItemViewModel
                {
                    Name = "HomeRadioButton",
                    DisplayText = "Home",
                    IconSource = "/Images/Home.png",
                    Command = HomeViewCommand,
                    IsChecked = true,
                    VisibilityBinding = "TrueProperty"
                },
                new MenuItemViewModel
                {
                    Name = "ScheduleRadioButton",
                    DisplayText = "Schedule",
                    IconSource = "/Images/Schedule.png",
                    Command = ScheduleViewCommand,
                    VisibilityBinding = "TrueProperty"
                },
                new MenuItemViewModel
                {
                    Name = "PerformanceAttendanceRadioButton",
                    DisplayText = "Score Management",
                    IconSource = "/Images/Performance.png",
                    Command = PerformanceAttendanceViewCommand,
                    VisibilityBinding = "CanAccessTeacherFeatures"
                },
                new MenuItemViewModel
                {
                    Name = "EditScheduleRadioButton",
                    DisplayText = "Edit Schedule",
                    IconSource = "/Images/EditSchedule.png",
                    Command = EditScheduleViewCommand,
                    VisibilityBinding = "CanAccessEmployeeFeatures"
                },
                new MenuItemViewModel
                {
                    Name = "ClassManagementRadioButton",
                    DisplayText = "Class Management",
                    IconSource = "/Images/Class.png",
                    Command = ClassManagementViewCommand,
                    VisibilityBinding = "CanAccessEmployeeFeatures"
                },
                new MenuItemViewModel
                {
                    Name = "TeacherManagementRadioButton",
                    DisplayText = "Teacher Management",
                    IconSource = "/Images/Teacher.png",
                    Command = TeacherManagementViewCommand,
                    VisibilityBinding = "CanAccessEmployeeFeatures"
                },
                new MenuItemViewModel
                {
                    Name = "EditStaffRadioButton",
                    DisplayText = "Staff Management",
                    IconSource = "/Images/Staff.png",
                    Command = EditStaffViewCommand,
                    VisibilityBinding = "CanAccessAdminFeatures"
                },
                new MenuItemViewModel
                {
                    Name = "Indevelopment",
                    DisplayText = "In development",
                    IconSource = "/Images/Development.png",
                    VisibilityBinding = "TrueProperty"
                }
            };
            UpdateMenuItems();
        }

        public void UpdateMenuItems()
        {
            if (MenuItems == null) return;

            var items = MenuItems.ToList();
            var inDevelopmentItem = items.FirstOrDefault(x => x.Name == "Indevelopment");
            if (inDevelopmentItem != null)
            {
                items.Remove(inDevelopmentItem);
            }

            var expectedItems = new List<MenuItemViewModel>
            {
                new MenuItemViewModel
                {
                    Name = "HomeRadioButton",
                    DisplayText = "Home",
                    IconSource = "/Images/Home.png",
                    Command = HomeViewCommand,
                    IsChecked = items.Any(x => x.Name == "HomeRadioButton" && x.IsChecked)
                },
                new MenuItemViewModel
                {
                    Name = "ScheduleRadioButton",
                    DisplayText = "Schedule",
                    IconSource = "/Images/Schedule.png",
                    Command = ScheduleViewCommand,
                    IsChecked = items.Any(x => x.Name == "ScheduleRadioButton" && x.IsChecked)
                },
                new MenuItemViewModel
                {
                    Name = "PerformanceAttendanceRadioButton",
                    DisplayText = "Score Management",
                    IconSource = "/Images/Performance.png",
                    Command = PerformanceAttendanceViewCommand,
                    IsChecked = items.Any(x => x.Name == "PerformanceAttendanceRadioButton" && x.IsChecked)
                },
                new MenuItemViewModel
                {
                    Name = "EditScheduleRadioButton",
                    DisplayText = "Edit Schedule",
                    IconSource = "/Images/EditSchedule.png",
                    Command = EditScheduleViewCommand,
                    IsChecked = items.Any(x => x.Name == "EditScheduleRadioButton" && x.IsChecked)
                },
                new MenuItemViewModel
                {
                    Name = "ClassManagementRadioButton",
                    DisplayText = "Class Management",
                    IconSource = "/Images/Class.png",
                    Command = ClassManagementViewCommand,
                    IsChecked = items.Any(x => x.Name == "ClassManagementRadioButton" && x.IsChecked)
                },
                new MenuItemViewModel
                {
                    Name = "TeacherManagementRadioButton",
                    DisplayText = "Teacher Management",
                    IconSource = "/Images/Teacher.png",
                    Command = TeacherManagementViewCommand,
                    IsChecked = items.Any(x => x.Name == "TeacherManagementRadioButton" && x.IsChecked)
                },
                new MenuItemViewModel
                {
                    Name = "EditStaffRadioButton",
                    DisplayText = "Staff Management",
                    IconSource = "/Images/Staff.png",
                    Command = EditStaffViewCommand,
                    IsChecked = items.Any(x => x.Name == "EditStaffRadioButton" && x.IsChecked)
                }
            };

            if (!IsLoggedIn)
            {
                foreach (var item in expectedItems)
                {
                    if (item.Name == "HomeRadioButton" || item.Name == "ScheduleRadioButton")
                    {
                        item.VisibilityBinding = "TrueProperty";
                    }
                    else
                    {
                        item.VisibilityBinding = "FalseProperty";
                    }
                }
            }
            else if (UserRole == TeacherRole)
            {
                foreach (var item in expectedItems)
                {
                    if (item.Name == "HomeRadioButton" || item.Name == "ScheduleRadioButton")
                    {
                        item.VisibilityBinding = "TrueProperty";
                    }
                    else if (item.Name == "PerformanceAttendanceRadioButton")
                    {
                        item.VisibilityBinding = "CanAccessTeacherFeatures";
                    }
                    else
                    {
                        item.VisibilityBinding = "FalseProperty";
                    }
                }
            }
            else if (EmployeeRoles.Contains(UserRole))
            {
                foreach (var item in expectedItems)
                {
                    if (item.Name == "HomeRadioButton" || item.Name == "ScheduleRadioButton")
                    {
                        item.VisibilityBinding = "TrueProperty";
                    }
                    else if (item.Name == "PerformanceAttendanceRadioButton")
                    {
                        item.VisibilityBinding = "CanAccessTeacherFeatures";
                    }
                    else if (item.Name == "EditScheduleRadioButton" || item.Name == "ClassManagementRadioButton" || item.Name == "TeacherManagementRadioButton")
                    {
                        item.VisibilityBinding = "CanAccessEmployeeFeatures";
                    }
                    else
                    {
                        item.VisibilityBinding = "FalseProperty";
                    }
                }
            }
            else if (UserRole == AdminRole)
            {
                foreach (var item in expectedItems)
                {
                    if (item.Name == "HomeRadioButton" || item.Name == "ScheduleRadioButton")
                    {
                        item.VisibilityBinding = "TrueProperty";
                    }
                    else if (item.Name == "PerformanceAttendanceRadioButton")
                    {
                        item.VisibilityBinding = "CanAccessTeacherFeatures";
                    }
                    else if (item.Name == "EditScheduleRadioButton" || item.Name == "ClassManagementRadioButton" || item.Name == "TeacherManagementRadioButton")
                    {
                        item.VisibilityBinding = "CanAccessEmployeeFeatures";
                    }
                    else if (item.Name == "EditStaffRadioButton")
                    {
                        item.VisibilityBinding = "CanAccessAdminFeatures";
                    }
                }
            }

            if (inDevelopmentItem != null)
            {
                expectedItems.Add(inDevelopmentItem);
            }

            MenuItems.Clear();
            foreach (var item in expectedItems)
            {
                MenuItems.Add(item);
                System.Diagnostics.Debug.WriteLine($"MenuItem added: {item.Name}, VisibilityBinding: {item.VisibilityBinding}");
            }
        }

        private bool CanAccessAdminOnly(object parameter)
        {
            if (!IsLoggedIn || string.IsNullOrEmpty(UserRole))
                return false;

            return CanAccessAdminFeatures;
        }

        private bool CanAccessEmployeeOrAdmin(object parameter)
        {
            if (!IsLoggedIn || string.IsNullOrEmpty(UserRole))
                return false;

            return CanAccessEmployeeFeatures;
        }

        private bool CanAccessTeacherOrAbove(object parameter)
        {
            if (!IsLoggedIn || string.IsNullOrEmpty(UserRole))
                return false;

            return CanAccessTeacherFeatures;
        }

        public void UpdateRadioButtons(string activeButtonName)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateRadioButtons called with activeButtonName: {activeButtonName}");
            foreach (var item in MenuItems)
            {
                item.IsChecked = item.Name == activeButtonName;
                System.Diagnostics.Debug.WriteLine($"MenuItem: {item.Name}, IsChecked: {item.IsChecked}");
            }
        }

        private void OpenLoginWindow(object parameter)
        {
            var loginView = new LoginView();
            loginView.DataContext = new LoginViewModel(this, loginView);
            loginView.ShowDialog();
        }

        public void OnLoginSuccess(string role, string lastName, string firstName, string middleName)
        {
            IsLoggedIn = true;
            UserRole = role?.Trim();
            LastName = lastName;
            FirstName = firstName;
            MiddleName = middleName;
            RefreshAdminPanelVisibility();
            System.Diagnostics.Debug.WriteLine($"Login successful. UserRole set to: {UserRole}, LastName: {LastName}, FirstName: {FirstName}, MiddleName: {MiddleName}, IsAdminPanelVisible: {IsAdminPanelVisible}");
        }

        public void RefreshAdminPanelVisibility()
        {
            OnPropertyChanged(nameof(IsTeacherPanelVisible));
            OnPropertyChanged(nameof(IsStaffPanelVisible));
            OnPropertyChanged(nameof(IsAdminPanelVisible));
            OnPropertyChanged(nameof(CanAccessAdminFeatures));
            OnPropertyChanged(nameof(CanAccessEmployeeFeatures));
            OnPropertyChanged(nameof(CanAccessTeacherFeatures));
            UpdateMenuItems();
        }

        public void UpdateScheduleViewDate(DateTime? date)
        {
            if (ScheduleVM != null)
            {
                ScheduleVM.SelectedDate = date;
                ScheduleVM.RefreshSchedule();
                CurrentView = _scheduleView;
                UpdateRadioButtons("ScheduleRadioButton");
                System.Diagnostics.Debug.WriteLine($"UpdateScheduleViewDate called. SelectedDate: {ScheduleVM.SelectedDate}, CurrentView set to ScheduleView");
            }
        }
    }

    public class MenuItemViewModel : ObservableObject
    {
        public string Name { get; set; }
        public string DisplayText { get; set; }
        public string IconSource { get; set; }
        public RelayCommand Command { get; set; }
        public string VisibilityBinding { get; set; }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }
    }
}
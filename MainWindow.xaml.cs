using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SchoolManager.MVVM.View;
using SchoolManager.MVVM.ViewModel;

namespace SchoolManager
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            DataContext = new MainViewModel();
            DataContextChanged += MainWindow_DataContextChanged;
        }

        private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow DataContext changed to: {DataContext?.GetType().Name}");
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            _viewModel = DataContext as MainViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsAdminPanelVisible) || e.PropertyName == nameof(MainViewModel.CurrentView))
            {
                UpdateAdminPanelVisibility(_viewModel);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as MainViewModel;
            if (_viewModel == null)
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: DataContext is not MainViewModel!");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"MainWindow Loaded. UserRole: {_viewModel.UserRole ?? "null"}, IsAdminPanelVisible: {_viewModel.IsAdminPanelVisible}");
            if (_viewModel.EditScheduleVM != null)
            {
                _viewModel.EditScheduleVM.Initialize();
            }
            _viewModel.RefreshAdminPanelVisibility();
            UpdateAdminPanelVisibility(_viewModel);
        }

        private void UpdateAdminPanelVisibility(MainViewModel viewModel)
        {
            var contentControl = FindName("MainContentControl") as ContentControl;
            if (contentControl == null)
            {
                System.Diagnostics.Debug.WriteLine("MainContentControl not found!");
                return;
            }
            if (contentControl.Content is not HomeView)
            {
                viewModel.HomeViewCommand.Execute(null);
                System.Diagnostics.Debug.WriteLine("Switched to HomeView to update AdminPanel visibility.");
                contentControl.LayoutUpdated += ContentControl_LayoutUpdated;
                return;
            }
            ApplyAdminPanelVisibility(contentControl);
        }

        private void ContentControl_LayoutUpdated(object sender, EventArgs e)
        {
            var contentControl = sender as ContentControl;
            if (contentControl != null)
            {
                contentControl.LayoutUpdated -= ContentControl_LayoutUpdated;
                if (contentControl.Content is HomeView)
                {
                    ApplyAdminPanelVisibility(contentControl);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ContentControl content is not HomeView after layout update!");
                }
            }
        }

        private void ApplyAdminPanelVisibility(ContentControl contentControl)
        {
            if (contentControl.Content is HomeView homeView)
            {
                var adminPanel = homeView.FindName("AdminPanel") as StackPanel;
                if (adminPanel != null)
                {
                    adminPanel.Visibility = _viewModel.IsAdminPanelVisible ? Visibility.Visible : Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine($"AdminPanel Visibility manually set to: {adminPanel.Visibility}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AdminPanel not found in HomeView!");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("HomeView not found in ContentControl after switching!");
            }
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.CloseUserProfileCommand.Execute(null);
            }
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
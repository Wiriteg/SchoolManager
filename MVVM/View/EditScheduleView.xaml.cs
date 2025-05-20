using SchoolManager.MVVM.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SchoolManager.MVVM.View
{
    public partial class EditScheduleView : UserControl
    {
        public EditScheduleView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (DataContext is EditScheduleViewModel viewModel)
                {
                    viewModel.Initialize();
                }
            };

            ItemsControl.AddHandler(Expander.ExpandedEvent, new RoutedEventHandler(OnExpanderExpanded));
        }

        private void OnExpanderExpanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Expander expander && DataContext is EditScheduleViewModel viewModel)
            {
                if (!viewModel.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    expander.IsExpanded = false;
                    e.Handled = true;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is string className)
            {
                var itemsControl = this.FindName("ItemsControl") as ItemsControl;
                if (itemsControl != null)
                {
                    foreach (var item in itemsControl.Items)
                    {
                        var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                        if (container != null)
                        {
                            var expander = container.FindName("ClassExpander") as Expander;
                            if (expander != null && (expander.DataContext as string) == className)
                            {
                                if (DataContext is EditScheduleViewModel viewModel)
                                {
                                    if (!viewModel.SelectedDate.HasValue)
                                    {
                                        MessageBox.Show("Выберите дату!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                        return;
                                    }
                                    viewModel.SaveScheduleCommand.Execute(className);
                                    expander.IsExpanded = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
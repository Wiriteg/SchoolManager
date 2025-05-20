using SchoolManager.MVVM.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SchoolManager.MVVM.View
{
    public partial class ScheduleView : UserControl
    {
        public ScheduleView()
        {
            InitializeComponent();
        }

        private void DataScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                var scrollViewer = (ScrollViewer)sender;
                if (e.Delta > 0)
                    scrollViewer.LineLeft();
                else
                    scrollViewer.LineRight();
                HeaderScroll.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset);
                e.Handled = true;
            }
        }

        private void DataScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        private void ScheduleView_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScheduleViewModel;
            if (viewModel == null || viewModel.Classes == null) return;

            var headerGrid = FindName("HeaderGrid") as Grid;
            if (headerGrid != null && headerGrid.ColumnDefinitions.Count == 0)
            {
                foreach (var className in viewModel.Classes)
                {
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150, GridUnitType.Pixel) });
                }
            }
        }
    }

    public static class VisualTreeHelperExtensions
    {
        public static T FindVisualChild<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
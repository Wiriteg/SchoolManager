using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using SchoolManager.MVVM.ViewModel;
using System.Windows.Media;

namespace SchoolManager.MVVM.View
{
    public partial class EditStaffView : UserControl
    {
        public EditStaffView(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = mainViewModel.EditStaffVM;
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = (sender as DependencyObject)?.FindVisualParent<ScrollViewer>();
            if (scrollViewer == null) return;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (e.Delta > 0)
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - 50);
                else
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + 50);
            }
            else
            {
                if (e.Delta > 0)
                    scrollViewer.LineUp();
                else
                    scrollViewer.LineDown();
            }

            e.Handled = true;
        }

        private void PopupScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                var scrollViewer = sender as ScrollViewer;
                if (scrollViewer != null)
                {
                    if (e.Delta > 0)
                        scrollViewer.LineLeft();
                    else
                        scrollViewer.LineRight();
                    e.Handled = true;
                }
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                var childResult = FindVisualChild<T>(child);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }
    }
}
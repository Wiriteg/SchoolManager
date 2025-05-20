using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace SchoolManager.MVVM.View
{
    public partial class TeacherManagementView : UserControl
    {
        public TeacherManagementView()
        {
            InitializeComponent();
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
    }
}
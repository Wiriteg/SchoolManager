using System.Windows;
using System.Windows.Media;

namespace SchoolManager.MVVM.Model
{
    public static class VisualTreeExtensions
    {
        public static T FindVisualParent<T>(this DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}
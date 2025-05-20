using System.Windows.Controls;
using System.Windows.Input;

namespace SchoolManager.MVVM.View
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
        }

        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MainScrollViewer == null) return;

            if (e.Delta > 0)
                MainScrollViewer.LineUp();
            else
                MainScrollViewer.LineDown();

            e.Handled = true;
        }
    }
}
using System.Windows.Input;

namespace SchoolManager.Models
{
    public class MenuButtonItem
    {
        public string Name { get; set; }
        public string IconPath { get; set; }
        public ICommand Command { get; set; }
        public bool IsVisible { get; set; } = true;
    }
}
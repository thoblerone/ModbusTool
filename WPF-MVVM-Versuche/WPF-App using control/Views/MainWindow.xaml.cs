using WPF_App_using_control.ViewModels;

namespace WPF_App_using_control.Views
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainWindowViewModel();

            DataContext = vm;
        }
    }
}

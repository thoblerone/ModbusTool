using ModbusWpf.Common.ViewModels;

namespace ModbusWpf.Common.Views
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

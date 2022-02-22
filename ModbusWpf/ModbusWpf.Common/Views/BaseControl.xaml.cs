using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ModbusWpf.Common.Views
{
    /// <summary>
    /// Interaktionslogik für MyNewBaseFormControl.xaml
    /// </summary>
    public partial class BaseControl
    {
        public BaseControl()
        {
            InitializeComponent();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox theBox)
                return;

            theBox.ScrollIntoView(theBox.SelectedItem);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ModbusWpf.Common.ViewModels;

namespace ModbusWpf.Client.ViewModels
{
    class ClientViewModel : BaseViewModel
    {
        public ClientViewModel() : base()
        {
        }

        protected override Task InitializeAsync()
        {
            MasterOptionsVisibility = Visibility.Visible;
            SlaveOptionsVisibility = Visibility.Collapsed;

            return base.InitializeAsync();
        }

    }
}

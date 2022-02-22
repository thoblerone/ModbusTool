using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ModbusWpf.Common.ViewModels;

namespace ModbusWpf.Server.ViewModels
{
    public class ServerViewModel : BaseViewModel
    {
        public ServerViewModel() : base()
        {
        }

        protected override Task InitializeAsync()
        {
            MasterOptionsVisibility = Visibility.Collapsed;
            SlaveOptionsVisibility = Visibility.Visible;
            
            return base.InitializeAsync();
        }

    }
}

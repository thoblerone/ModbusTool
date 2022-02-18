using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using Catel.IoC;
using Catel.MVVM;
using Modbus.Common;
using ModbusWpf.Common.Helpers;

namespace ModbusWpf.Common.ViewModels
{
    class BaseFormViewModel : ViewModelBase
    {
        protected Socket Socket { get; set; }

        private bool LogPaused { get; set; } = false;

        #region Properties
        private ushort _startAddress;
        protected ushort StartAddress
        {
            get
            {
                return _startAddress;
            }
            set
            {
                // TODO: CurrentTab.StartAddress = value;
                // TODO: var tab = tabControl1.SelectedTab;
                // TODO: tab.Text = value.ToString();
                _startAddress = value;
            }
        }

        private ushort _dataLength;
        protected ushort DataLength
        {
            get
            {
                return _dataLength;
            }
            set
            {
                _dataLength = value;
                // TODO: CurrentTab.DataLength = value;
            }
        }

        private bool _showDataLength;
        public bool ShowDataLength
        {
            get => _showDataLength;
            set
            {
                _showDataLength = value;
                // TODO: foreach (DataTab tab in tabPage1.Controls)
                // TODO: {
                // TODO:     tab.ShowDataLength = value;
                // TODO: }
                // TODO: foreach (DataTab tab in tabPage2.Controls)
                // TODO: {
                // TODO:     tab.ShowDataLength = value;
                // TODO: }
            }
        }

        public IPAddress IPAddress { get; set; } = IPAddress.None;

        public int TCPPort { get; set; }

        public byte SlaveId { get; set; }

        public int SlaveDelay { get; set; }

        public string PortName { get; set; }

        public int Baud { get; set; }

        public Parity Parity { get; set; }

        public int DataBits { get; set; }

        public StopBits StopBits { get; set; }

        private DisplayFormat _displayFormat = DisplayFormat.Integer;
        public DisplayFormat DisplayFormat
        {
            get => _displayFormat;
            set => _displayFormat = value;
            // TODO: get { return _displayFormat; }
            // TODO: set
            // TODO: {
            // TODO:     switch (value)
            // TODO:     {
            // TODO:         case DisplayFormat.LED:
            // TODO:             radioButtonLED.Checked = true;
            // TODO:             break;
            // TODO:         case DisplayFormat.Binary:
            // TODO:             radioButtonBinary.Checked = true;
            // TODO:             break;
            // TODO:         case DisplayFormat.Hex:
            // TODO:             radioButtonHex.Checked = true;
            // TODO:             break;
            // TODO:         case DisplayFormat.Integer:
            // TODO:             radioButtonInteger.Checked = true;
            // TODO:             break;
            // TODO:         case DisplayFormat.FloatReverse:
            // TODO:             radioButtonReverseFloat.Checked = true;
            // TODO:             break;
            // TODO:     }
            // TODO:     _displayFormat = value;
            // TODO:     CurrentTab.DisplayFormat = DisplayFormat;
            // TODO:     RefreshData();
            // TODO: }
        }

        public CommunicationMode CommunicationMode { get; set; } = CommunicationMode.TCP;

        public bool EnableTcpOptions => CommunicationMode is CommunicationMode.TCP or CommunicationMode.UDP;
        public bool EnableRtuOptions => CommunicationMode == CommunicationMode.RTU;

        public Visibility IpAddressVisibility { get; protected set; } = Visibility.Visible;

        public Visibility SlaveOptionsVisibility { get; protected set; } = Visibility.Visible;

        #endregion // Properties
        #region Constructors 
        public BaseFormViewModel() : this(null)
        {
        }

        public BaseFormViewModel(IRegisterDataService registerDataService)
        {
            if (registerDataService is null)
            {
                registerDataService = ServiceLocator.Default.TryResolveType<IRegisterDataService>();
                if (registerDataService is null)
                {
                    registerDataService = new RegisterDataService();
                    ServiceLocator.Default.RegisterInstance(registerDataService);
                }
            }

            DonateCommand = new TaskCommand(OnDonateCommandExecuteAsync);

            LoadUserData();
        }

        #endregion // Constructors

        protected override Task OnClosingAsync()
        {
            SaveUserData();

            return base.OnClosingAsync();
        }

        #region SettingsHandling

        private void LoadUserData()
        {
            if (Enum.TryParse(Properties.Settings.Default.CommunicationMode, out CommunicationMode mode))
                CommunicationMode = mode;
            if (Enum.TryParse(Properties.Settings.Default.DisplayFormat, out DisplayFormat format))
                DisplayFormat = format;
            
            if (IPAddress.TryParse(Properties.Settings.Default.IPAddress, out var ipAddress))
                IPAddress = ipAddress;
            
            TCPPort = Properties.Settings.Default.TCPPort;
            PortName = Properties.Settings.Default.PortName;
            Baud = Properties.Settings.Default.Baud;
            Parity = Properties.Settings.Default.Parity;
            StartAddress = Properties.Settings.Default.StartAddress;
            DataLength = Properties.Settings.Default.DataLength;
            SlaveId = Properties.Settings.Default.SlaveId;
            SlaveDelay = Properties.Settings.Default.SlaveDelay;
            DataBits = Properties.Settings.Default.DataBits;
            StopBits = Properties.Settings.Default.StopBits;
        }

        private void SaveUserData()
        {
            Properties.Settings.Default.CommunicationMode = CommunicationMode.ToString();
            Properties.Settings.Default.IPAddress = IPAddress.ToString();
            Properties.Settings.Default.DisplayFormat = DisplayFormat.ToString();
            Properties.Settings.Default.TCPPort = TCPPort;
            Properties.Settings.Default.PortName = PortName;
            Properties.Settings.Default.Baud = Baud;
            Properties.Settings.Default.Parity = Parity;
            Properties.Settings.Default.StartAddress = StartAddress;
            Properties.Settings.Default.DataLength = DataLength;
            Properties.Settings.Default.SlaveId = SlaveId;
            Properties.Settings.Default.SlaveDelay = SlaveDelay;
            Properties.Settings.Default.DataBits = DataBits;
            Properties.Settings.Default.StopBits = StopBits;
            Properties.Settings.Default.Save();
        }
        #endregion

        #region Commands

        public TaskCommand DonateCommand { get; }


        public string[] ComPortItemsSource => SerialPort.GetPortNames();

        public int[] ComPortBaudRates => new[]
        { 
            128000,
            115200,
            57600,
            38400,
            19200,
            14400,
            9600,
            7200,
            4800,
            2400,
            1800,
            1200,
            600,
            300,
            150
        };

        private async Task OnDonateCommandExecuteAsync()
        {
            string url = "https://paypal.me/classicdiy?country.x=CA&locale.x=en_US";
            System.Diagnostics.Process.Start(url);
        }

        #endregion
    }
}

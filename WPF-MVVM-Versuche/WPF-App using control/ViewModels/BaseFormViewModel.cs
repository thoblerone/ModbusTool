using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
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

        protected string PortName
        {
            get;
            set;
            //TODO: get
            //TODO: {
            //TODO:     return comboBoxSerialPorts.Text;
            //TODO: }
            //TODO: set
            //TODO: {
            //TODO:     comboBoxSerialPorts.Text = value;
            //TODO: }
        }

        protected int Baud
        {
            get;
            set;
            // TODO: get
            // TODO: {
            // TODO:     return Int32.Parse(comboBoxBaudRate.Text);
            // TODO: }
            // TODO: set
            // TODO: {
            // TODO:     comboBoxBaudRate.SelectedItem = Convert.ToString(value);
            // TODO: }
        }

        protected Parity Parity
        { get; set;
            // TODO: get
            // TODO: {
            // TODO:     var parity = Parity.None;
            // TODO:     if (comboBoxParity.SelectedItem.Equals(Parity.None.ToString()))
            // TODO:     {
            // TODO:         parity = Parity.None;
            // TODO:     }
            // TODO:     else if (comboBoxParity.SelectedItem.Equals(Parity.Odd.ToString()))
            // TODO:     {
            // TODO:         parity = Parity.Odd;
            // TODO:     }
            // TODO:     else if (comboBoxParity.SelectedItem.Equals(Parity.Even.ToString()))
            // TODO:     {
            // TODO:         parity = Parity.Even;
            // TODO:     }
            // TODO:     else if (comboBoxParity.SelectedItem.Equals(Parity.Mark.ToString()))
            // TODO:     {
            // TODO:         parity = Parity.Mark;
            // TODO:     }
            // TODO:     else if (comboBoxParity.SelectedItem.Equals(Parity.Space.ToString()))
            // TODO:     {
            // TODO:         parity = Parity.Space;
            // TODO:     }
            // TODO:     return parity;
            // TODO: }
            // TODO: set
            // TODO: {
            // TODO:     comboBoxParity.SelectedItem = Convert.ToString(value);
            // TODO: }
        }

        protected int DataBits
        {
            get;
            set;
            //TODO: get
            //TODO: {
            //TODO:     int bits = 0;
            //TODO:     switch (comboBoxDataBits.SelectedIndex)
            //TODO:     {
            //TODO:         case 0:
            //TODO:             bits = 7;
            //TODO:             break;
            //TODO:         case 1:
            //TODO:             bits = 8;
            //TODO:             break;
            //TODO:     }
            //TODO:     return bits;
            //TODO: }
            //TODO: set
            //TODO: {
            //TODO:     switch (value)
            //TODO:     {
            //TODO:         case 7:
            //TODO:             comboBoxDataBits.SelectedIndex = 0;
            //TODO:             break;
            //TODO:         case 8:
            //TODO:             comboBoxDataBits.SelectedIndex = 1;
            //TODO:             break;
            //TODO:     }
            //TODO: }
        }

        protected StopBits StopBits
        {
            get;
            set;
            // TODO: get
            // TODO: {
            // TODO:     StopBits bits = StopBits.None;
            // TODO:     switch (comboBoxStopBits.SelectedIndex)
            // TODO:     {
            // TODO:         case 0:
            // TODO:             bits = StopBits.None;
            // TODO:             break;
            // TODO:         case 1:
            // TODO:             bits = StopBits.One;
            // TODO:             break;
            // TODO:         case 2:
            // TODO:             bits = StopBits.OnePointFive;
            // TODO:             break;
            // TODO:         case 3:
            // TODO:             bits = StopBits.Two;
            // TODO:             break;
            // TODO:     }
            // TODO:     return bits;
            // TODO: }
            // TODO: set
            // TODO: {
            // TODO:     switch (value)
            // TODO:     {
            // TODO:         case StopBits.None:
            // TODO:             comboBoxStopBits.SelectedIndex = 0;
            // TODO:             break;
            // TODO:         case StopBits.One:
            // TODO:             comboBoxStopBits.SelectedIndex = 1;
            // TODO:             break;
            // TODO:         case StopBits.OnePointFive:
            // TODO:             comboBoxStopBits.SelectedIndex = 2;
            // TODO:             break;
            // TODO:         case StopBits.Two:
            // TODO:             comboBoxStopBits.SelectedIndex = 3;
            // TODO:             break;
            // TODO:     }
            // TODO: }
        }

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

        #endregion

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

            LoadUserData();

        }

        protected override Task OnClosingAsync()
        {
            SaveUserData();
            return base.OnClosingAsync();
        }

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
    }
}

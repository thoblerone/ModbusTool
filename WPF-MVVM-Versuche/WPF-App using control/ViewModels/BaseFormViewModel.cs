using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Catel;
using Catel.IoC;
using Catel.MVVM;
using Catel.Services;
using Modbus.Common;
using ModbusWpf.Common.Helpers;
using ModbusWpf.Common.Properties;

namespace ModbusWpf.Common.ViewModels
{
    class BaseFormViewModel : ViewModelBase
    {
        // used in derived master and slave classes
        protected Socket Socket { get; set; }

        private IDispatcherService _dispatcherService;

        #region Properties

        public bool LogPaused { get; set; } = false;

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

        public IPAddress IpAddress { get; set; } = IPAddress.None;

        public int TcpPort { get; set; }

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

        public ObservableCollection<string> CommLogEntries { get; }

        public int SelectedCommLogIndex { get; set; }

        #endregion // Properties

        #region Constructors 
        public BaseFormViewModel() : this(null, null)
        {
        }

        public BaseFormViewModel(IDispatcherService dispatcherService, IRegisterDataService registerDataService)
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

            if (dispatcherService is null)
            {
                dispatcherService = ServiceLocator.Default.ResolveType<IDispatcherService>();
            }

            _dispatcherService = dispatcherService;

            DonateCommand = new TaskCommand(OnDonateCommandExecuteAsync);
            LogClearCommand = new TaskCommand(OnLogClearCommandExecuteAsync);

            SlaveListenCommand = new TaskCommand(OnSlaveListenCommandExecuteAsync);
            SlaveDisconnectCommand = new TaskCommand(OnSlaveDisconnectCommandExecuteAsync);

            CommLogEntries = new ObservableCollection<string>();

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
            if (Enum.TryParse(Settings.Default.CommunicationMode, out CommunicationMode mode))
                CommunicationMode = mode;
            if (Enum.TryParse(Settings.Default.DisplayFormat, out DisplayFormat format))
                DisplayFormat = format;
            
            if (IPAddress.TryParse(Settings.Default.IPAddress, out var ipAddress))
                IpAddress = ipAddress;
            
            TcpPort = Settings.Default.TCPPort;
            PortName = Settings.Default.PortName;
            Baud = Settings.Default.Baud;
            Parity = Settings.Default.Parity;
            StartAddress = Settings.Default.StartAddress;
            DataLength = Settings.Default.DataLength;
            SlaveId = Settings.Default.SlaveId;
            SlaveDelay = Settings.Default.SlaveDelay;
            DataBits = Settings.Default.DataBits;
            StopBits = Settings.Default.StopBits;
        }

        private void SaveUserData()
        {
            Settings.Default.CommunicationMode = CommunicationMode.ToString();
            Settings.Default.IPAddress = IpAddress.ToString();
            Settings.Default.DisplayFormat = DisplayFormat.ToString();
            Settings.Default.TCPPort = TcpPort;
            Settings.Default.PortName = PortName;
            Settings.Default.Baud = Baud;
            Settings.Default.Parity = Parity;
            Settings.Default.StartAddress = StartAddress;
            Settings.Default.DataLength = DataLength;
            Settings.Default.SlaveId = SlaveId;
            Settings.Default.SlaveDelay = SlaveDelay;
            Settings.Default.DataBits = DataBits;
            Settings.Default.StopBits = StopBits;
            Settings.Default.Save();
        }
        #endregion // SettingsHandling

        #region Commands

        public TaskCommand SlaveDisconnectCommand { get; }
        private async Task OnSlaveDisconnectCommandExecuteAsync()
        {
            throw new NotImplementedException("Implement in sub class");
        }

        public TaskCommand SlaveListenCommand { get; }
        protected virtual async Task OnSlaveListenCommandExecuteAsync()
        {
            throw new NotImplementedException("Implement in sub class");
        }

        public TaskCommand DonateCommand { get; }
        private async Task OnDonateCommandExecuteAsync()
        {
            string url = "https://paypal.me/classicdiy?country.x=CA&locale.x=en_US";
            Process.Start(url);

            await Task.CompletedTask;
        }


        public TaskCommand LogClearCommand { get; }

        private async Task OnLogClearCommandExecuteAsync()
        {
           CommLogEntries.Clear();

           await Task.CompletedTask;
        }
        #endregion // Commands

        #region Logging

        public delegate void AppendLogDelegate(string log);

        protected void DriverIncommingData(byte[] data, int len)
        {
            if (LogPaused)
                return;

            var hex = new StringBuilder(len);
            for (int i = 0; i < len; i++)
            {
                hex.AppendFormat("{0:x2} ", data[i]);
            }
            AppendLog($"RX: {hex}");
        }

        protected void DriverOutgoingData(byte[] data)
        {
            if (LogPaused)
                return;
            var hex = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
                hex.AppendFormat("{0:x2} ", b);
            AppendLog($"TX: {hex}");
        }

        protected void AppendLog(string log)
        {
            if (LogPaused)
                return;

            if (!Dispatcher.CurrentDispatcher.CheckAccess())
            {
                // if required, recursive call on dispatcher thread
                _dispatcherService.Invoke(new AppendLogDelegate(AppendLog), log);
                return;
            }

            var now = DateTime.Now;
            var logEntry = $">{now.ToLongTimeString()}: {log}";
            CommLogEntries.Add(logEntry);
            SelectedCommLogIndex = CommLogEntries.Count - 1;
            SelectedCommLogIndex = - 1;

            // don't let the log file get huge to conserve memory
            if (CommLogEntries.Count > 15000)
            {
                _dispatcherService.InvokeAsync(() =>
                {
                    while(CommLogEntries.Count > 10000)
                        CommLogEntries.RemoveAt(0);
                });
            }
        }

        #endregion

    }
}

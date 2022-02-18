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

        private readonly IDispatcherService _dispatcherService;

        #region Properties

        public bool LogPaused { get; set; } = false;

        private ushort _startAddress;
        protected ushort StartAddress
        {
            get => _startAddress;
            set
            {
                _startAddress = value;
                if (SelectedDataTabItem is not null)
                    SelectedDataTabItem.StartAddress = value;
            }
        }

        private ushort _dataLength;
        protected ushort DataLength
        {
            get => _dataLength;
            set
            {
                _dataLength = value;
                if (SelectedDataTabItem is not null)
                    SelectedDataTabItem.StartAddress = value;
            }
        }

        private bool _showDataLength;
        public bool ShowDataLength
        {
            get => _showDataLength;
            set
            {
                _showDataLength = value;

                foreach (var dataTabItem in DataTabItems)
                {
                    if (dataTabItem is not null)
                        dataTabItem.ShowDataLength = value;
                }
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
        private DataTabControlViewModel _selectedDataTabItem;

        public DisplayFormat DisplayFormat
        {
            get => _displayFormat;
            set
            {
                _displayFormat = value;
                if (SelectedDataTabItem is not null)
                    SelectedDataTabItem.DisplayFormat = value;
            }
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

        public ObservableCollection<DataTabControlViewModel> DataTabItems { get; }
        public ObservableCollection<string> CommLogEntries { get; }

        public int SelectedCommLogIndex { get; set; }

        public DataTabControlViewModel SelectedDataTabItem
        {
            get => _selectedDataTabItem;
            set
            {
                _selectedDataTabItem = value;

                if (_selectedDataTabItem.IsDummyTab)
                {
                    _selectedDataTabItem.IsDummyTab = false;
                    _selectedDataTabItem.DataLength = DataLength;
                    _selectedDataTabItem.StartAddress = StartAddress;
                    _selectedDataTabItem.ShowDataLength = ShowDataLength;
                    _selectedDataTabItem.DisplayFormat = DisplayFormat;
                    _selectedDataTabItem.ApplyAddressSelectionCommand.Execute();

                    DataTabItems.Add(new DataTabControlViewModel(){IsDummyTab = true});

                    RaisePropertyChanged(nameof(SelectedDataTabItem));

                    AppendLog($"New data tab starting at {StartAddress}");
                }
            }
        }

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

            dispatcherService ??= ServiceLocator.Default.ResolveType<IDispatcherService>();

            _dispatcherService = dispatcherService;

            DonateCommand = new TaskCommand(OnDonateCommandExecuteAsync);
            LogClearCommand = new TaskCommand(OnLogClearCommandExecuteAsync);

            SlaveListenCommand = new TaskCommand(OnSlaveListenCommandExecuteAsync);
            SlaveDisconnectCommand = new TaskCommand(OnSlaveDisconnectCommandExecuteAsync);
            CloseDataTabItemCommand = new TaskCommand<DataTabControlViewModel>(OnCloseDataTabItemCommandExecuteAsync);

            CommLogEntries = new ();
            DataTabItems = new();
#if DEBUG
            //if (CatelEnvironment.IsInDesignMode)
            {
                AppendLog("first design mode log entry");
                AppendLog("second design mode log entry");

                DataTabItems.Add(new DataTabControlViewModel(registerDataService) {StartAddress = 1000, DisplayFormat = DisplayFormat.Integer, DataLength = 32});
                //DataTabItems.Add(new DataTabControlViewModel(registerDataService) {StartAddress = 2000, DisplayFormat = DisplayFormat.FloatReverse, DataLength = 32});
                //DataTabItems.Add(new DataTabControlViewModel(registerDataService) {StartAddress = 4000, DisplayFormat = DisplayFormat.Hex, DataLength = 16});
                //DataTabItems.Add(new DataTabControlViewModel(registerDataService) {StartAddress = 5000, DisplayFormat = DisplayFormat.LED, DataLength = 2});

                DataTabItems.Add(new DataTabControlViewModel(){IsDummyTab = true});
            }
#endif

            LoadUserData();
        }

        #endregion // Constructors

        protected override Task OnClosingAsync()
        {
            SaveUserData();

            return base.OnClosingAsync();
        }

        protected override async Task InitializeAsync()
        {
            foreach (var dataVm in DataTabItems)
            {
                await dataVm.InitializeViewModelAsync().ConfigureAwait(false);
            }

            await base.InitializeAsync().ConfigureAwait(false);
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


        public TaskCommand<DataTabControlViewModel> CloseDataTabItemCommand { get; }

        private async Task OnCloseDataTabItemCommandExecuteAsync(DataTabControlViewModel tabItem)
        {
            var closingIndex = DataTabItems.IndexOf(tabItem);
            // don't close the special "..." tab, which is the last
            if (closingIndex == DataTabItems.Count - 1)
            //if (tabItem.Header == "...")
                return;

            AppendLog($"Closing data tab item {tabItem?.StartAddress} ({tabItem?.DisplayFormat})");

            var selectedIndex = DataTabItems.IndexOf(SelectedDataTabItem);

            // if the currently selected tab was close,
            // select the previous one, if there is any
            // else select the next one ("..." is always there)
            // if that is the last one, a new tab page is created
            if (selectedIndex == closingIndex)
            {
                if (selectedIndex > 0)
                {
                    SelectedDataTabItem = DataTabItems[selectedIndex - 1];
                    RaisePropertyChanged(nameof(SelectedDataTabItem));
                }
                else
                {
                    SelectedDataTabItem = DataTabItems[selectedIndex + 1];
                    RaisePropertyChanged(nameof(SelectedDataTabItem));
                }
            }

            var oldTab = DataTabItems[closingIndex];

            await oldTab.CloseViewModelAsync(null).ConfigureAwait(false);
            DataTabItems.RemoveAt(closingIndex);

        }
        #endregion // Commands

        #region Logging

        public delegate void AppendLogDelegate(string log);

        protected void DriverIncomingData(byte[] data, int len)
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

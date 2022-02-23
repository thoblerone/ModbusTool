using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Catel;
using Catel.IoC;
using Catel.MVVM;
using Catel.Services;
using Microsoft.Win32;
using Modbus.Common;
using ModbusWpf.Common.Helpers;
using ModbusWpf.Common.Models;
using ModbusWpf.Common.Properties;

namespace ModbusWpf.Common.ViewModels
{
    public class BaseViewModel : ViewModelBase
    {
        // used in derived master and slave classes
        protected Socket _socket;
        protected SerialPort _uart;


        protected readonly IDispatcherService _dispatcherService;

        #region Properties

        public bool LogPaused { get; set; } = false;

        public ushort StartAddress { get; set; }

        public ushort DataLength { get; set; }

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

        private DataTabControlViewModel _selectedDataTabItem;

        public DisplayFormat DisplayFormat { get; set; } = DisplayFormat.Integer;

        public CommunicationMode CommunicationMode { get; set; } = CommunicationMode.TCP;

        public bool EnableTcpOptions
        {
            get { return 
                CommunicationMode is CommunicationMode.TCP or CommunicationMode.UDP &&
                HasConnected == false; }
        }

        public bool EnableRtuOptions
        {
            get { return 
                CommunicationMode == CommunicationMode.RTU &&
                HasConnected == false; }
        }

        public Visibility IpAddressVisibility { get; protected set; } = Visibility.Visible;

        public Visibility SlaveOptionsVisibility { get; protected set; } = Visibility.Visible;
        public Visibility MasterOptionsVisibility { get; protected set; } = Visibility.Visible;

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

                if (_selectedDataTabItem.DisplayFormat is null)
                {
                    _selectedDataTabItem.DataLength = DataLength;
                    _selectedDataTabItem.StartAddress = StartAddress;
                    _selectedDataTabItem.ShowDataLength = ShowDataLength;
                    _selectedDataTabItem.DisplayFormat = DisplayFormat;
                    _selectedDataTabItem.ApplyAddressSelectionCommand.Execute();

                    DataTabItems.Add(new DataTabControlViewModel(){DisplayFormat = null});

                    RaisePropertyChanged(nameof(SelectedDataTabItem));

                    AppendLog($"New {DisplayFormat} data tab starting at {StartAddress}");
                }
            }
        }

        public bool HasConnected { get; set; }
        public bool ClientPollingActive { get; set; }
        public int ClientPollTime { get; set; }

        public virtual string IconPath => "m 37.21,15.38 v 0.17 H 70.74 V 42.09 H 36.95 l 0.01,0.26 v 9.61 c 0,0.08 0,0.16 -0.01,0.24 -0.09,2.07 -0.77,3.54 -2.06,4.39 -1.29,0.86 -2.99,0.94 -5.08,0.23 C 29.58,56.74 29.38,56.63 29.2,56.49 19.1,48.57 12.14,40.65 2.04,32.72 L 1.9,32.6 C 0.22,31.08 -0.24,29.49 0.11,27.9 0.42,26.5 1.4,25.3 2.71,24.27 L 27.47,2.3 c 1.21,-0.95 2.48,-1.69 3.67,-2.05 1.07,-0.32 2.11,-0.35 3.07,0 1.05,0.38 1.88,1.15 2.42,2.39 0.38,0.89 0.59,2.03 0.59,3.46 v 9.05 c 0,0.09 0,0.16 -0.01,0.23 z M 85.67,82.73 V 82.56 H 52.14 V 56.03 h 33.79 l -0.01,-0.26 v -9.61 c 0,-0.08 0,-0.17 0.01,-0.24 0.09,-2.07 0.77,-3.54 2.06,-4.39 1.29,-0.86 2.99,-0.94 5.08,-0.23 0.23,0.08 0.43,0.19 0.61,0.33 10.1,7.92 17.06,15.85 27.15,23.78 l 0.14,0.12 c 1.68,1.52 2.14,3.11 1.79,4.7 -0.31,1.4 -1.29,2.6 -2.6,3.63 L 95.41,95.82 c -1.21,0.95 -2.48,1.69 -3.67,2.05 -1.07,0.32 -2.11,0.35 -3.07,0 -1.05,-0.38 -1.88,-1.15 -2.42,-2.39 -0.38,-0.89 -0.59,-2.03 -0.59,-3.46 v -9.06 c 0,-0.08 0,-0.15 0.01,-0.23 z";
        public virtual Color IconColor => Color.FromRgb(0xff, 0x62, 0x74);

        #endregion // Properties

        #region Constructors 
        public BaseViewModel() : this(null, null)
        {
        }

        public BaseViewModel(IDispatcherService dispatcherService, IRegisterDataService registerDataService)
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

            SlaveListenCommand = new TaskCommand(OnSlaveListenCommandExecuteAsync, () => !HasConnected);
            MasterListenCommand = new TaskCommand(OnMasterListenCommandExecuteAsync, () => !HasConnected);
            DisconnectCommand = new TaskCommand(OnDisconnectCommandExecuteAsync, () => HasConnected);
            CloseDataTabItemCommand = new TaskCommand<DataTabControlViewModel>(OnCloseDataTabItemCommandExecuteAsync);

            ExportCurrentTabDataCommand = new TaskCommand(OnExportCurrentTabDataCommandExecuteAsync);
            ImportCurrentTabDataCommand = new TaskCommand(OnImportCurrentTabDataCommandExecuteAsync);

            CommLogEntries = new ();
            DataTabItems = new();

            DataTabItems.Add(new DataTabControlViewModel(registerDataService)
            {
                StartAddress = Settings.Default.StartAddress,
                DataLength = Settings.Default.DataLength,
                DisplayFormat = Settings.Default.DisplayFormat
            });

#if DEBUG
            if (CatelEnvironment.IsInDesignMode)
            {
                AppendLog("first design mode log entry");
                AppendLog("second design mode log entry");

                DataTabItems.Add(new DataTabControlViewModel(registerDataService) {StartAddress = 1000, DisplayFormat = DisplayFormat.FloatReverse, DataLength = 32});
                DataTabItems.Add(new DataTabControlViewModel(registerDataService) {StartAddress = 1500, DisplayFormat = DisplayFormat.Hex, DataLength = 16});
                DataTabItems.Add(new DataTabControlViewModel(registerDataService) {StartAddress = 2000, DisplayFormat = DisplayFormat.LED, DataLength = 2});
            }
#endif

            DataTabItems.Add(new DataTabControlViewModel() { DisplayFormat = null });
            
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
            Title = $"{nameof(BaseViewModel)} ({Assembly.GetExecutingAssembly().GetName().Version})";

            foreach (var dataVm in DataTabItems)
            {
                await dataVm.InitializeViewModelAsync().ConfigureAwait(false);
            }

            SelectedDataTabItem = DataTabItems.FirstOrDefault();

            await base.InitializeAsync().ConfigureAwait(false);
        }

        #region SettingsHandling

        private void LoadUserData()
        {
            if (Enum.TryParse(Settings.Default.CommunicationMode, out CommunicationMode mode))
                CommunicationMode = mode;
            
            if (IPAddress.TryParse(Settings.Default.IPAddress, out var ipAddress))
                IpAddress = ipAddress;

            TcpPort = Settings.Default.TCPPort;
            PortName = Settings.Default.PortName;
            Baud = Settings.Default.Baud;
            Parity = Settings.Default.Parity;
            StartAddress = Settings.Default.StartAddress;
            DisplayFormat = Settings.Default.DisplayFormat;
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
            Settings.Default.DisplayFormat = DisplayFormat;
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


        public TaskCommand ImportCurrentTabDataCommand { get; }

        private async Task OnImportCurrentTabDataCommandExecuteAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                AddExtension = true,
                DefaultExt = ".csv",
                Multiselect = false
            };

            var registerDataService = ServiceLocator.Default.TryResolveType<IRegisterDataService>();

            if (registerDataService is null)
                return;

            if (openFileDialog.ShowDialog() != true)
                return;


            var registerImportModel = new RegisterDisplayModel(registerDataService, 0);

            using (var s = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
            {
                using (var r = new StreamReader(s))
                {
                    var fileContent = r.ReadToEnd();
                    var registerNumberValuePairs =
                        fileContent.Split(new []{CultureInfo.CurrentUICulture.TextInfo.ListSeparator}, StringSplitOptions.None);

                    var first = true;
                    foreach (var registerValuePair in registerNumberValuePairs)
                    {
                        DisplayFormat fmt;

                        var v = registerValuePair.Split(':');
                        var address = int.Parse(v[0]);
                        var strValue = v[1];

                        registerImportModel.RegisterNumber = address;

                        if (strValue.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                        {
                            fmt = DisplayFormat.Hex;
                            registerImportModel.HexString = strValue;
                        }
                        else if (v[1].Contains(CultureInfo.CurrentUICulture.NumberFormat.CurrencyDecimalSeparator)) // found decimal point -> must be 2 register float
                        // bug: there's no way do distinguish between FloatReverse and the not yet implemented Float"Normal" representation
                        {
                            fmt = DisplayFormat.FloatReverse;
                            registerImportModel.FloatReverseString = strValue;
                        }
                        else if (v[1].Length > 6) //must be binary or LED, bug: the data format does not allow to distinguish
                        {
                            fmt = DisplayFormat.Binary;
                            registerImportModel.BinaryString = strValue;
                        }
                        else
                        {
                            fmt = DisplayFormat.Integer;
                            registerImportModel.TargetRegisterValue = Convert.ToUInt16(v[1], 10);
                        }

                        if (first)
                        {
                            SelectedDataTabItem.DisplayFormat = fmt;
                            SelectedDataTabItem.StartAddress = registerImportModel.RegisterNumber;
                            first = false;
                        }
                    }

                    r.Close();
                    SelectedDataTabItem.DataLength = Convert.ToUInt16(registerNumberValuePairs.Length);

                    if (SelectedDataTabItem.DisplayFormat == DisplayFormat.FloatReverse)
                        SelectedDataTabItem.DataLength *= 2;

                    SelectedDataTabItem.ApplyAddressSelectionCommand.Execute();
                }

                s.Close();
            }

            await Task.CompletedTask;
        }

        public TaskCommand ExportCurrentTabDataCommand { get; }

        private async Task OnExportCurrentTabDataCommandExecuteAsync()
        {
            if (SelectedDataTabItem?.RegisterModels.FirstOrDefault() is null)
                return;

            // var length = SelectedDataTabItem.DataLength;
            // var startAddress = SelectedDataTabItem.StartAddress;
            var startAddress = SelectedDataTabItem.RegisterModels.First().RegisterNumber;

            string suffix = DisplayFormat switch
            {
                DisplayFormat.Integer => "_Decimal_",
                DisplayFormat.Hex => "_HEX_",
                DisplayFormat.Binary => "_Binary_",
                DisplayFormat.LED => "_LED_",
                DisplayFormat.FloatReverse => "_FloatReverse_",
                _ => throw new ArgumentOutOfRangeException()
            };
            var filename = "ModbusExport_" + startAddress + suffix + DateTime.Now.ToString("yyyyMMddHHmm") + ".csv";

            var saveFileDialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = ".csv",
                FileName = filename,
                OverwritePrompt = true
            };

            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            using (var s = saveFileDialog.OpenFile())
            {
                using (var w = new StreamWriter(s))
                {
                    var regLast = SelectedDataTabItem.RegisterModels.Last();

                    foreach (var register in SelectedDataTabItem.RegisterModels)
                    {
                        w.Write(register.RegisterNumber);
                        await w.WriteAsync(':').ConfigureAwait(false);
                        var data = register.TargetRegisterValue;//_registerData[StartAddress + x];
                        switch (SelectedDataTabItem.DisplayFormat)
                        {
                            case DisplayFormat.Integer:
                                w.Write(register.TargetRegisterValue.ToString());
                                break;
                            case DisplayFormat.Hex:
                                await w.WriteAsync($"0x{register.HexString}").ConfigureAwait(false);
                                break;
                            case DisplayFormat.Binary:
                            case DisplayFormat.LED:
                                await w.WriteAsync(register.BinaryString).ConfigureAwait(false);
                                break;
                            case DisplayFormat.FloatReverse:
                                await w.WriteAsync(register.FloatReverseString).ConfigureAwait(false);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (register.RegisterNumber < regLast.RegisterNumber)
                            await w.WriteAsync(CultureInfo.CurrentUICulture.TextInfo.ListSeparator).ConfigureAwait(false);
                    }

                    await w.FlushAsync().ConfigureAwait(false);
                    w.Close();
                }

                s.Close();
            }

            await Task.CompletedTask;
        }

        public TaskCommand MasterListenCommand { get; }
        protected virtual async Task OnMasterListenCommandExecuteAsync()
        {
            throw new NotImplementedException("Implement in sub class");
        }
        public TaskCommand DisconnectCommand { get; }
        protected virtual async Task OnDisconnectCommandExecuteAsync()
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

            _dispatcherService.Invoke(() =>
            {
                var now = DateTime.Now;
                var logEntry = $">{now.ToLongTimeString()}: {log}";
                CommLogEntries.Add(logEntry);
                SelectedCommLogIndex = CommLogEntries.Count - 1;
                SelectedCommLogIndex = -1;

                // don't let the log file get huge to conserve memory
                if (CommLogEntries.Count > 15000)
                {
                    while (CommLogEntries.Count > 10000)
                        CommLogEntries.RemoveAt(0);
                }
            });
        }

        #endregion

    }
}

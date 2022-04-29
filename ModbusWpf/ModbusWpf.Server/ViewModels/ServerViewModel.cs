using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Catel.IoC;
using Catel.Services;
using Modbus.Common;
using Modbus.Ioc.Interfaces;
using ModbusLib;
using ModbusLib.Protocols;
using ModbusWpf.Common.Helpers;
using ModbusWpf.Common.ViewModels;

namespace ModbusWpf.Server.ViewModels
{
    public class ServerViewModel : BaseViewModel
    {
        #region Constuctors
        public ServerViewModel() : this(null, null)
        {
        }

        public ServerViewModel(IDispatcherService dispatcherService, IRegisterDataService registerDataService) 
            : base(dispatcherService, registerDataService)
        {
            // base class ensures the register data service is available
            _registerDataService = ServiceLocator.Default.ResolveType<IRegisterDataService>();
        }
        #endregion // Constructors

        #region Catel overrides
        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(false);

            Title = $"Modbus Server ({Assembly.GetExecutingAssembly().GetName().Version})";
            
            ClientOptionsVisibility = Visibility.Collapsed;
            ServerOptionsVisibility = Visibility.Visible;
        }
        protected override async Task OnClosingAsync()
        {
            await DoDisconnectAsync().ConfigureAwait(false);

            AppendLog("Closed");

            await base.OnClosingAsync();
        }
        #endregion // Catel overrides

        #region Server Functionality
        private ICommServer _listener;
        private Thread _tcpServerThread;
        private readonly IRegisterDataService _registerDataService;

        protected override async Task OnServerListenCommandExecuteAsync()
        {
            try
            {
                switch (CommunicationMode)
                {
                    case CommunicationMode.RTU:
                        _uart = new SerialPort(PortName, Baud, Parity, DataBits, StopBits);
                        _uart.Open();
                        var rtuServer = new ModbusServer(new ModbusRtuCodec()) { Address = ServerId };
                        rtuServer.OutgoingData += LogOutgoingData;
                        rtuServer.IncommingData += LogIncomingData;

                        _listener = _uart.GetListener(rtuServer);
                        _listener.ServeCommand += listener_ServeCommand;
                        _listener.Start();

                        AppendLog($"Connected using RTU to {PortName}");
                        break;

                    case CommunicationMode.UDP:
                        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        _socket.Bind(new IPEndPoint(IPAddress.Any, TcpPort));
                        //create a server driver
                        var udpServer = new ModbusServer(new ModbusTcpCodec()) { Address = ServerId };
                        udpServer.OutgoingData += LogOutgoingData;
                        udpServer.IncommingData += LogIncomingData;
                        //listen for an incoming request
                        _listener = _socket.GetUdpListener(udpServer);
                        _listener.ServeCommand += listener_ServeCommand;
                        _listener.Start();
                        AppendLog($"Listening to UDP port {TcpPort}");
                        break;

                    case CommunicationMode.TCP:
                        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        _socket.Bind(new IPEndPoint(IPAddress.Any, TcpPort));
                        _socket.Listen(10);

                        //create a server driver
                        _tcpServerThread = new Thread(TcpThreadWorker)
                        {
                            Name = $"{nameof(ServerViewModel)}.{nameof(TcpThreadWorker)}"
                        };
                        _tcpServerThread.Start();
                        AppendLog($"Listening to TCP port {TcpPort}");
                        break;
                }
            }
            catch (Exception ex)
            {
                AppendLog(ex.Message);
                return;
            }

            HasConnected = true;

            await Task.CompletedTask;
        }

        protected override async Task OnDisconnectCommandExecuteAsync()
        {
            await DoDisconnectAsync().ConfigureAwait(false);
            
            AppendLog("Disconnected");
        }

        private async Task DoDisconnectAsync()
        {
            if (_listener != null)
            {
                _listener.Abort();
                _listener = null;
            }

            if (_uart != null)
            {
                _uart.Close();
                _uart.Dispose();
                _uart = null;
            }

            if (_tcpServerThread?.IsAlive == true)
            {
                if (_tcpServerThread.Join(2000) == false)
                {
                    _socket?.Close(0);
                    _tcpServerThread.Abort();
                }

                _tcpServerThread = null;
            }

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }

            HasConnected = false;

            await Task.CompletedTask;
        }


        /// <summary>
        /// Running thread handler
        /// </summary>
        protected void TcpThreadWorker()
        {
            var server = new ModbusServer(new ModbusTcpCodec()) { Address = ServerId };
            server.IncommingData += LogIncomingData;
            server.OutgoingData += LogOutgoingData;
            try
            {
                while (_tcpServerThread.ThreadState == ThreadState.Running)
                {
                    //wait for an incoming connection
                    _listener = _socket.GetTcpListener(server);
                    _listener.ServeCommand += listener_ServeCommand;
                    _listener.Start();
                    AppendLog("Accepted connection.");
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                AppendLog(msg);
            }

        }

        private void listener_ServeCommand(object sender, ServeCommandEventArgs e)
        {
            var command = (ModbusCommand)e.Data.UserData;

            Thread.Sleep(ServerDelay);

            //take the proper function command handler
            switch (command.FunctionCode)
            {
                case ModbusCommand.FuncReadCoils:
                case ModbusCommand.FuncReadInputDiscretes:
                case ModbusCommand.FuncReadInputRegisters:
                case ModbusCommand.FuncReadMultipleRegisters:
                case ModbusCommand.FuncReadCustom:
                    DoRead(command);
                    break;

                case ModbusCommand.FuncWriteCoil:
                case ModbusCommand.FuncForceMultipleCoils:
                case ModbusCommand.FuncWriteMultipleRegisters:
                case ModbusCommand.FuncWriteSingleRegister:
                    DoWrite(command);
                    break;
                default:
                    AppendLog($"Illegal Function, expecting a valid function code {command.FunctionCode}.");
                    //return an exception
                    command.ExceptionCode = ModbusCommand.ErrorIllegalFunction;
                    break;
            }
        }

        private void DoRead(ModbusCommand command)
        {
            for (int i = 0; i < command.Count; i++)
                command.Data[i] = _registerDataService[command.Offset + i];

            AppendLog($"Sent data: Function code:{command.FunctionCode}, length = {command.Count}.");

        }

        private void DoWrite(ModbusCommand command)
        {
            var dataAddress = command.Offset;
            if (command.Count + dataAddress > _registerDataService.RegisterData.Length)
            {
                AppendLog($"Received data exceeds maintained range, Received address: {dataAddress}, length={command.Count}.");
                return;
            }
            for (int i = 0; i < command.Data.Length; i++)
            {
                _registerDataService[i + dataAddress] = command.Data[i];
            }

            AppendLog($"Received data: Function code: {command.FunctionCode}, length = {command.Data.Length}.");
        }
        #endregion // Server Functionality

        #region BaseViewModel overrides
        public override string IconPath => "m 66.558767,0.61082333 c -2.57334,0 -4.805682,1.48020397 -5.895765,3.63285297 -5.853112,0.68289 -11.297996,3.402935 -15.444017,7.7447467 -4.472252,4.683125 -7.026193,10.84506 -7.191293,17.34985 -0.0061,0.23204 -0.0088,0.46426 -0.0088,0.69763 v 0.84077 c -1.884892,0.33285 -3.323312,2.00528 -3.323312,4.01268 v 2.78329 c 0,1.44092 0.743317,2.70663 1.858801,3.4308 -0.01667,0.16986 -0.02842,0.34117 -0.02842,0.51315 v 6.52931 c 0,2.90116 2.361054,5.2617 5.262211,5.2617 H 42.0264 c 0.004,0.11403 0.01251,0.22883 0.0186,0.34313 7.94e-4,0.0135 0.0013,0.0263 0.0021,0.0398 0.0336,0.59637 0.09625,1.19866 0.189653,1.80402 0.01323,0.0865 0.02937,0.17238 0.04392,0.2589 0.03016,0.18018 0.06272,0.36061 0.09819,0.54105 0.02672,0.13812 0.0546,0.27659 0.08423,0.41497 0.02249,0.10424 0.04698,0.20839 0.07132,0.31264 1.477434,6.40688 6.01798,12.76618 11.629263,16.52406 l -5.29e-4,5.35832 -6.495211,1.99988 c -1.632479,-0.76861 -3.53872,-0.86624 -5.239991,-0.26458 l -8.399486,2.96881 c -5.29e-4,0 -0.0011,2.5e-4 -0.0016,5.3e-4 l -18.470702,6.52775 C 7.3990531,93.119783 1.7735031,100.56877 1.2236991,109.2135 l -0.145728,2.29082 -0.49557701,7.77678 -0.569991,8.96535 c -0.108479,1.70788 0.500435,3.40285 1.67121601,4.65036 0.07303,0.0778 0.148063,0.15357 0.224793,0.22686 1.150143,1.10437 2.699796,1.7322 4.299996,1.7322 h 19.5094029 84.031559 19.50941 c 1.70709,0 3.35639,-0.71444 4.52479,-1.96009 1.17051,-1.24593 1.77918,-2.94094 1.67069,-4.6483 L 134.244,109.21402 c -0.54953,-8.64473 -6.17535,-16.093467 -14.33246,-18.976097 l -26.871743,-9.4971 c -1.60496,-0.56779 -3.3925,-0.51295 -4.96042,0.14005 -0.0947,0.0394 -0.18826,0.0811 -0.2806,0.12505 l -5.44928,-1.6769 -1.048,-0.32349 -5.3e-4,-5.42189 c 0.11986,-0.0804 0.23725,-0.16447 0.35605,-0.24702 0.0135,-0.009 0.0268,-0.0184 0.0403,-0.0279 5.46444,-3.81979 9.65607,-9.89785 11.15229,-15.90601 0.007,-0.0286 0.0138,-0.0567 0.0207,-0.0853 0.0643,-0.26221 0.12302,-0.52381 0.17673,-0.78549 0.0109,-0.0526 0.022,-0.10521 0.0326,-0.15812 0.11245,-0.56912 0.20037,-1.13659 0.26148,-1.70016 0.01,-0.0857 0.0166,-0.17085 0.0248,-0.25631 0.0177,-0.18918 0.0328,-0.378 0.0444,-0.56638 0.006,-0.10001 0.0128,-0.19921 0.0171,-0.29869 0.002,-0.0476 0.006,-0.096 0.008,-0.14366 h 0.24029 c 2.90116,0 5.2617,-2.36106 5.2617,-5.26221 v -6.52932 c 0,-0.17118 -0.0113,-0.34123 -0.0279,-0.51056 1.117873,-0.72364 1.862943,-1.99087 1.862943,-3.43338 v -2.78329 c 0,-2.0074 -1.438163,-3.6801 -3.323313,-4.01268 v -0.84078 c 0,-0.23309 -0.003,-0.46535 -0.009,-0.6966 -0.0524,-2.06877 -0.34701,-4.12279 -0.87591,-6.10505 -1.39703,-4.801893 -2.89296,-6.82378 -3.48351,-7.709605 C 88.905327,9.2619123 82.143767,5.1119593 74.807927,4.2457143 73.718107,2.0920053 71.484747,0.61079333 68.910617,0.61079333 Z m 0,3.85661297 h 2.35179 c 1.27053,0 2.33953,0.867287 2.65359,2.040185 0.002,0.0085 0.003,0.01685 0.005,0.02532 0.0307,0.119591 0.0534,0.235309 0.0687,0.348816 0.0151,0.113506 0.0222,0.225301 0.0222,0.335896 v 8.4077557 c 0,1.516592 -1.23364,2.75022 -2.7497,2.75022 h -2.35179 c -1.5166,0 -2.74971,-1.233893 -2.74971,-2.75022 V 7.2176543 c 0,-0.110596 0.007,-0.222138 0.0222,-0.33538 0,-5.29e-4 5.3e-4,-10e-4 5.3e-4,-10e-4 0.0151,-0.11377 0.038,-0.228972 0.0687,-0.348299 0.002,-0.0079 0.003,-0.01609 0.005,-0.02429 0.313,-1.173428 1.38226,-2.041219 2.65359,-2.041219 z M 40.382071,41.745783 h 1.637625 v 7.80676 H 41.78767 c -0.775229,0 -1.405599,-0.63089 -1.405599,-1.40612 z m 53.064976,0 h 1.63401 v 6.40064 c 0,0.77523 -0.63063,1.40612 -1.40611,1.40612 h -0.2279 z m -47.570739,0.0109 h 43.715679 v 9.70896 c -5.3e-4,0.005 -0.001,0.01 -0.001,0.015 v 1.29863 c 0,0.0418 -0.003,0.0838 -0.003,0.12557 -0.002,0.18494 -0.007,0.37028 -0.016,0.55707 -0.005,0.0905 -0.0123,0.18158 -0.0186,0.27234 -0.0108,0.14948 -0.0232,0.2993 -0.0388,0.44958 -0.009,0.0881 -0.0186,0.17673 -0.0295,0.2651 -0.0217,0.17913 -0.0466,0.35855 -0.0749,0.53847 -0.008,0.0537 -0.0155,0.107 -0.0248,0.16071 -1.09802,6.52357 -6.45336,13.56167 -12.48348,16.54008 -0.0246,0.0122 -0.0493,0.0248 -0.0739,0.0367 -0.45482,0.22199 -0.91344,0.41968 -1.37408,0.59324 -0.0587,0.022 -0.11774,0.0429 -0.17621,0.0641 -0.1696,0.0614 -0.33916,0.11913 -0.50902,0.17364 -0.0638,0.0204 -0.12745,0.0419 -0.19068,0.061 -0.22252,0.068 -0.44514,0.13151 -0.66818,0.18707 l -0.262,0.0651 c -0.16721,0.0418 -0.33083,0.0815 -0.49196,0.12092 -2.29235,0.55906 -3.97186,0.87953 -5.54798,0.87953 -0.23786,0 -0.47751,-0.008 -0.71934,-0.0217 -1.4941,-0.0857 -3.10974,-0.43181 -5.375896,-1.00304 l -0.0155,-0.004 c -0.217223,-0.0548 -0.43365,-0.11781 -0.650608,-0.18449 -0.04789,-0.0148 -0.09604,-0.0306 -0.143661,-0.046 l -0.05736,-0.0186 c -0.05318,-0.0172 -0.10805,-0.0373 -0.161232,-0.0553 -0.107156,-0.0362 -0.214534,-0.0717 -0.321426,-0.11059 -0.068,-0.0243 -0.135885,-0.0493 -0.203089,-0.0744 -0.01323,-0.005 -0.0263,-0.0107 -0.03927,-0.0155 -0.45085,-0.17304 -0.899591,-0.36796 -1.34462,-0.58756 -0.0069,-0.003 -0.01405,-0.007 -0.02066,-0.0103 C 51.795131,68.393683 45.87642,59.924353 45.87642,52.832193 Z m 31.567069,33.92092 v 3.68143 c 5.3e-4,1.49066 0.95774,2.78694 2.38331,3.22668 l 1.25212,0.38551 1.59939,0.49454 -14.94586,7.18871 -7.671365,-3.6897 -7.274491,-3.49901 2.853057,-0.88005 c 1.425046,-0.43974 2.381766,-1.73705 2.381766,-3.22771 l 5.29e-4,-3.62665 c 0.580496,0.2405 1.16645,0.4516 1.757,0.62993 0.284692,0.0865 0.541304,0.15857 0.773079,0.21704 7.93e-4,2.7e-4 0.0013,2.5e-4 0.0021,5.3e-4 0.0021,5.3e-4 0.0041,0.002 0.0062,0.002 2.695045,0.67971 4.687195,1.09078 6.654375,1.13946 0.0918,0.002 0.18362,0.003 0.27543,0.003 0.0394,2.6e-4 0.0784,0.002 0.11782,0.002 h 5.3e-4 c 2.08148,0 4.13246,-0.40902 6.97012,-1.11466 l 0.26149,-0.0651 c 0.26193,-0.0651 0.52404,-0.13898 0.78651,-0.21756 0.0688,-0.0206 0.13714,-0.0432 0.20567,-0.0646 0.2114,-0.0664 0.42267,-0.13673 0.63407,-0.21187 0.055,-0.0196 0.11033,-0.0388 0.16537,-0.0589 0.27067,-0.099 0.54143,-0.202 0.81183,-0.31471 z m -32.850193,8.53023 c 0.499256,-0.021 1.00339,0.0785 1.454174,0.29559 l 0.613915,0.29507 c 0.0056,0.003 0.01021,0.007 0.01551,0.009 l 17.144694,8.24652 -6.773752,4.44263 c -0.521494,0.3421 -1.12667,0.52296 -1.749764,0.52296 -0.705644,-2.6e-4 -1.375651,-0.22664 -1.936832,-0.65474 l -14.687476,-11.2081 5.038453,-1.78077 c 0.283368,-0.10011 0.581527,-0.15586 0.881083,-0.16847 z m 46.028713,0.002 c 0.38342,-0.0158 0.76949,0.0392 1.13223,0.16743 l 5.03897,1.78077 -14.68799,11.2081 c -0.56145,0.42836 -1.23119,0.65474 -1.93683,0.65474 -0.6231,0 -1.22827,-0.18061 -1.74977,-0.52245 l -6.7753,-4.44365 17.13694,-8.24291 c 2.7e-4,-2.6e-4 8e-4,-5.3e-4 0.001,-5.3e-4 l 0.63872,-0.30696 c 0.0283,-0.0137 0.0577,-0.0237 0.0863,-0.0367 0.35177,-0.15557 0.73228,-0.24209 1.1157,-0.25786 z m -66.831578,7.20676 V 131.00022 H 6.2084471 c -0.655373,0 -1.263812,-0.26344 -1.713074,-0.74207 -0.338138,-0.36037 -0.549561,-0.8035 -0.6165,-1.28055 -0.02223,-0.15901 -0.02867,-0.3217 -0.01809,-0.48627 l 0.569991,-8.96535 0.496094,-7.77626 0.145211,-2.29134 c 0.282045,-4.43759 2.192713,-8.49183 5.2208689,-11.486117 0.151607,-0.14975 0.305607,-0.2966 0.462505,-0.4408 0.313795,-0.28866 0.638873,-0.5666 0.9741,-0.83251 0.167481,-0.13308 0.337274,-0.2634 0.510047,-0.39067 0.345545,-0.25426 0.701212,-0.49691 1.066601,-0.72657 1.095904,-0.68924 2.278956,-1.26523 3.534668,-1.70894 z m 87.886631,10e-4 6.94944,2.45618 c 6.69819,2.36749 11.31793,8.484737 11.76931,15.585087 l 1.21026,19.03347 c 0.0262,0.41169 -0.052,0.81091 -0.22427,1.16995 -0.10346,0.21537 -0.24073,0.41669 -0.4098,0.59687 -0.44926,0.47889 -1.0577,0.74207 -1.71307,0.74207 H 111.67695 Z M 82.794987,110.81072 h 14.80736 c 0.99431,0 1.79989,0.80585 1.79989,1.79989 v 5.76244 c 0,0.99378 -0.80584,1.79989 -1.79989,1.79989 h -14.80736 c -0.99431,0 -1.79989,-0.80585 -1.79989,-1.79989 v -5.76244 c 0,-0.9943 0.80585,-1.79989 1.79989,-1.79989 z";
        public override Color IconColor => Color.FromRgb(0x74, 0xff, 0x62);
        #endregion // BaseViewModel overrides
    }
}

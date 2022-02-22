using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Catel.IoC;
using Catel.Services;
using Modbus.Common;
using ModbusLib;
using ModbusLib.Protocols;
using ModbusWpf.Common.Helpers;
using ModbusWpf.Common.ViewModels;

namespace ModbusWpf.Server.ViewModels
{
    public class ServerViewModel : BaseViewModel
    {
        public ServerViewModel() : this(null, null)
        {
        }

        public ServerViewModel(IDispatcherService dispatcherService, IRegisterDataService registerDataService) 
            : base(dispatcherService, registerDataService)
        {
            // base class ensures the register data service is available
            _registerDataService = ServiceLocator.Default.ResolveType<IRegisterDataService>();
        }

        protected override Task InitializeAsync()
        {
            MasterOptionsVisibility = Visibility.Collapsed;
            SlaveOptionsVisibility = Visibility.Visible;
            
            return base.InitializeAsync();
        }

        private ICommServer _listener;
        private Thread _tcpServerThread;
        private readonly IRegisterDataService _registerDataService;

        protected override async Task OnSlaveListenCommandExecuteAsync()
        {
            try
            {
                switch (CommunicationMode)
                {
                    case CommunicationMode.RTU:
                        _uart = new SerialPort(PortName, Baud, Parity, DataBits, StopBits);
                        _uart.Open();
                        var rtuServer = new ModbusServer(new ModbusRtuCodec()) { Address = SlaveId };
                        rtuServer.OutgoingData += DriverOutgoingData;
                        rtuServer.IncommingData += DriverIncomingData;

                        _listener = _uart.GetListener(rtuServer);
                        _listener.ServeCommand += listener_ServeCommand;
                        _listener.Start();

                        AppendLog($"Connected using RTU to {PortName}");
                        break;

                    case CommunicationMode.UDP:
                        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        _socket.Bind(new IPEndPoint(IPAddress.Any, TcpPort));
                        //create a server driver
                        var udpServer = new ModbusServer(new ModbusTcpCodec()) { Address = SlaveId };
                        udpServer.OutgoingData += DriverOutgoingData;
                        udpServer.IncommingData += DriverIncomingData;
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
            var server = new ModbusServer(new ModbusTcpCodec()) { Address = SlaveId };
            server.IncommingData += DriverIncomingData;
            server.OutgoingData += DriverOutgoingData;
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

            Thread.Sleep(SlaveDelay);

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
                command.Data[i] = _registerDataService.RegisterData[command.Offset + i].RegisterValue;

            AppendLog($"Sent data: Function code:{command.FunctionCode}.");

        }

        private void DoWrite(ModbusCommand command)
        {
            var dataAddress = command.Offset;
            if (dataAddress < StartAddress || dataAddress > StartAddress + DataLength)
            {
                AppendLog($"Received address is not within viewable range, Received address:{dataAddress}.");
                return;
            }
            if (command.Count + dataAddress > _registerDataService.RegisterData.Length)
            {
                AppendLog($"Received address is not within viewable range, Received address:{dataAddress}.");
                return;
            }

            //command.Data.CopyTo(_registerData, dataAddress);
            for (int i = 0; i < command.Data.Length; i++)
            {
                _registerDataService.RegisterData[i + dataAddress].RegisterValue = command.Data[i];
            }

            AppendLog($"Received data: Function code:{command.FunctionCode}.");
        }

        protected override async Task OnClosingAsync()
        {
            await DoDisconnectAsync().ConfigureAwait(false);

            AppendLog("Closed");

            await base.OnClosingAsync();
        }
    }
}

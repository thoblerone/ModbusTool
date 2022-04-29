using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Modbus.Common;
using Modbus.Ioc.Interfaces;
using ModbusLib;
using ModbusLib.Protocols;

namespace Modbus.Ioc.Services
{
    public class ModbusDataService
    {
        #region Server Functionality
        private ICommServer _listener;
        private Thread _tcpServerThread;
        private readonly IRegisterDataService _registerDataService;
        private Socket _socket;
        private SerialPort _uart;

        public CommunicationMode CommunicationMode { get; set; } = CommunicationMode.TCP;
        public string PortName { get; set; }

        public int Baud { get; set; }

        public Parity Parity { get; set; }

        public int DataBits { get; set; }

        public StopBits StopBits { get; set; }
        public byte ServerId { get; set; }
        public int TcpPort { get; set; }
        public bool HasConnected { get; set; }
        public int ServerDelay { get; set; }

        protected async Task OnServerListenCommandExecuteAsync()
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
                            Name = $"{nameof(ModbusDataService)}.{nameof(TcpThreadWorker)}"
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

    }
}
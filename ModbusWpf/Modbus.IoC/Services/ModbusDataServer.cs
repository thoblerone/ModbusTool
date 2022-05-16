using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Catel.IoC;
using Catel.Logging;
using Modbus.Ioc.Interfaces;
using Modbus.Ioc.Models;
using ModbusLib;
using ModbusLib.Protocols;

namespace Modbus.Ioc.Services
{
    public class ModbusDataServer : IModbusDataServer
    {
        public ModbusDataServer() : this (null, null) { }
        public ModbusDataServer(IModbusRegisterDataService modbusRegisterData) : this(modbusRegisterData, null){}

        public ModbusDataServer(IModbusRegisterDataService modbusRegisterData, ILog logger)
        {
            Logger = logger ?? LogManager.GetCurrentClassLogger();

            // if the register data service is null, either take the
            // pre-registered one or instantiate the default implementation
            modbusRegisterData ??= ServiceLocator.Default.ResolveType<IModbusRegisterDataService>();
            if (modbusRegisterData is null)
            {
                Logger.Warning($"no {nameof(IModbusRegisterDataService)} was specified for the {nameof(ModbusDataServer)} constructor. Creating and registering a default implementation.");
                modbusRegisterData =  new ModbusRegisterDataService();
                ServiceLocator.Default.RegisterInstance(modbusRegisterData);
            }
            ModbusRegisterDataService = modbusRegisterData;

        }


        #region Server Functionality
        private ICommServer _listener;
        private Thread _tcpServerThread;
        private IModbusRegisterDataService ModbusRegisterDataService { get; }

        private ILog Logger { get; }

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
        public bool HasConnected { get; private set; }
        public int ServerDelay { get; set; }
        public int TcpIdleTimeoutSeconds { get; set; }

        public void ConnectAndListen()
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

                        //AppendLog($"Connected using RTU to {PortName}");
                        Logger.Status($"{nameof(ModbusDataServer)} connected using RTU to {PortName}");
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
                        Logger.Status($"{nameof(ModbusDataServer)} is listening to UDP port {TcpPort}");
                        break;

                    case CommunicationMode.TCP:
                        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        _socket.Bind(new IPEndPoint(IPAddress.Any, TcpPort));
                        _socket.Listen(10);

                        //create a server driver
                        _tcpServerThread = new Thread(TcpThreadWorker)
                        {
                            Name = $"{nameof(ModbusDataServer)}.{nameof(TcpThreadWorker)}"
                        };
                        _tcpServerThread.Start();
                        //AppendLog($"Listening to TCP port {TcpPort}");
                        Logger.Status($"{nameof(ModbusDataServer)} is listening to TCP port {TcpPort}");
                        break;
                }
            }
            catch (Exception ex)
            {
                // AppendLog(ex.Message);
                Logger.Error(ex.Message);
                return;
            }

            HasConnected = true;
        }

        public void Disconnect()
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

            Logger.Status($"{nameof(ModbusDataServer)} disconnected");
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
                    _listener = _socket.GetTcpListener(server, TcpIdleTimeoutSeconds);
                    _listener.ServeCommand += listener_ServeCommand;
                    _listener.Start();
                    // AppendLog("Accepted connection.");
                    Logger.Info($"{nameof(ModbusDataServer)} accepted a connection.");
                    Thread.Sleep(1);
                }
            }
            catch (ThreadAbortException _)
            {
                Logger.Info($"{nameof(ModbusDataServer)} TCP worker thread aborted.");
            }
            catch (Exception ex) 
            {
                string msg = ex.Message;
                // AppendLog(msg);
                Logger.Error(msg);
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
                    var msg = $"Illegal Function, expecting a valid function code {command.FunctionCode}.";
                    Logger.Error(msg);
                    //return an exception
                    command.ExceptionCode = ModbusCommand.ErrorIllegalFunction;
                    break;
            }
        }

        private void DoRead(ModbusCommand command)
        {
            for (var i = 0; i < command.Count; i++)
                command.Data[i] = ModbusRegisterDataService[command.Offset + i];

            Logger.Info($"{nameof(ModbusDataServer)} sent data: function code = {command.FunctionCode}, length = {command.Count}.");

        }

        private void DoWrite(ModbusCommand command)
        {
            var dataAddress = command.Offset;
            if (command.Count + dataAddress > ModbusRegisterDataService.RegisterData.Length)
            {
                var msgErr = $"{nameof(ModbusDataServer)} received data exceeds maintained range, received address: {dataAddress}, length={command.Count}.";
                Logger.Error(msgErr);
                return;
            }
            for (var i = 0; i < command.Data.Length; i++)
            {
                ModbusRegisterDataService[i + dataAddress] = command.Data[i];
            }

            var msgData = $"{nameof(ModbusDataServer)} received data: function code = {command.FunctionCode}, length = {command.Data.Length}.";
            Logger.Info(msgData);
        }
        #endregion // Server Functionality

        #region Transmission Logging
        public bool LogTransmissionsPaused { get; set; }
        protected void LogIncomingData(byte[] data, int len)
        {
            if (LogTransmissionsPaused)
                return;

            var hex = new StringBuilder(len);
            for (int i = 0; i < len; i++)
            {
                hex.AppendFormat("{0:x2} ", data[i]);
            }
            Logger.Info($"{nameof(ModbusDataServer)} RX: {hex}");
        }

        protected void LogOutgoingData(byte[] data)
        {
            if (LogTransmissionsPaused)
                return;
            var hex = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
                hex.AppendFormat("{0:x2} ", b);
            Logger.Info($"{nameof(ModbusDataServer)} TX: {hex}");
        }
        #endregion
    }
}
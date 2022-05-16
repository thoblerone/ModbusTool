using System.IO.Ports;
using Modbus.Ioc.Models;

namespace Modbus.Ioc.Interfaces
{
    public interface IModbusDataServer
    {
        CommunicationMode CommunicationMode { get; set; }
        string PortName { get; set; }
        int Baud { get; set; }
        Parity Parity { get; set; }
        int DataBits { get; set; }
        StopBits StopBits { get; set; }
        byte ServerId { get; set; }
        int TcpPort { get; set; }
        bool HasConnected { get; }
        int ServerDelay { get; set; }
        int TcpIdleTimeoutSeconds { get; set; }
        bool LogTransmissionsPaused { get; set; }
        
        void ConnectAndListen();
        void Disconnect();
    }
}
using System.IO.Ports;
using System.Threading.Tasks;
using Modbus.Common;

namespace Modbus.Ioc.Interfaces
{
    public interface IModbusDataService
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
        bool LogTransmissionsPaused { get; set; }
        Task ConnectAndListenAsync();
        Task DoDisconnectAsync();
    }
}
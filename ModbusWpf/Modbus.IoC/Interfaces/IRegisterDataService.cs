using System.Threading.Tasks;
using Modbus.Ioc.Models;

namespace Modbus.Ioc.Interfaces
{

    public interface IRegisterDataService
    {
        IRegisterDataValue[] RegisterData { get; set; }
        ushort this[int index] { get; set; }
    }

    public delegate void RegisterValueChangedHandler(ushort oldValue, ushort newValue);

    public interface IRegisterDataValue
    {
        ushort RegisterValue { get; set; }
        event RegisterValueChangedHandler RegisterValueChanged;
    }

}
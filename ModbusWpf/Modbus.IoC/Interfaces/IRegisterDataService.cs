using System.Threading.Tasks;
using Modbus.Ioc.Models;

namespace Modbus.Ioc.Interfaces
{

    public interface IModbusRegisterDataService
    {
        IRegisterDataValue[] RegisterData { get; set; }
        ushort this[int index] { get; set; }

        float GetFloatValue(int index);
        void SetFloatValue(int index, float value);
        float GetFloatReverseValue(int index);
        void SetFloatReverseValue(int index, float value);
        int GetIntValue(int index);
        void SetIntValue(int index, int value);

    }

    public delegate void RegisterValueChangedHandler(ushort oldValue, ushort newValue);

    public interface IRegisterDataValue
    {
        ushort RegisterValue { get; set; }
        event RegisterValueChangedHandler RegisterValueChanged;
    }

}
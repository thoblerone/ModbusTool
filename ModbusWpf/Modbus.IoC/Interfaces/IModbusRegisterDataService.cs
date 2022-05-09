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
        int GetInt32Value(int index);
        void SetInt32Value(int index, int value);

    }
}
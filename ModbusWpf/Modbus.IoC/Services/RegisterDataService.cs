using System;
using System.Globalization;
using System.Threading.Tasks;
using Modbus.Ioc.Interfaces;
using Modbus.Ioc.Models;

namespace Modbus.Ioc.Services
{
    public class ModbusRegisterDataService : IModbusRegisterDataService
    {
        public static int NumRegistersHeld = 65600;

        public ModbusRegisterDataService()
        {
            RegisterData = new IRegisterDataValue[NumRegistersHeld];

            Parallel.For(0, NumRegistersHeld, i =>
            {
                RegisterData[i] = new RegisterDataValue((ushort)i);
            });
        }

        public IRegisterDataValue[] RegisterData { get; set; }

        public ushort this[int index]
        {
            get => RegisterData[index].RegisterValue;
            set => RegisterData[index].RegisterValue = value;
        }

        public float GetFloatValue(int index)
        {
            if (index >= RegisterData.Length - 1)
                return float.NaN;

            var dataUshort1 = RegisterData[index].RegisterValue;
            var dataUshort2 = RegisterData[index + 1].RegisterValue;

            var bytes = new byte[4];

            bytes[0] = (byte)(dataUshort1 % 255);
            bytes[1] = (byte)((dataUshort1 >> 8) % 255);
            bytes[2] = (byte)(dataUshort2 % 255);
            bytes[3] = (byte)((dataUshort2 >> 8) % 255);

            return System.BitConverter.ToSingle(bytes, 0);
        }

        public void SetFloatValue(int index, float value)
        {
            if (index >= RegisterData.Length - 1)
                throw new IndexOutOfRangeException();

            var bytes = BitConverter.GetBytes(value);

            RegisterData[index].RegisterValue = (ushort)((bytes[1] << 8) + bytes[0]);
            RegisterData[index + 1].RegisterValue = (ushort)((bytes[3] << 8) + bytes[2]);
        }

        public float GetFloatReverseValue(int index)
        {
            if (index >= RegisterData.Length - 1)
                return float.NaN;

            var dataUshort1 = RegisterData[index].RegisterValue;
            var dataUshort2 = RegisterData[index + 1].RegisterValue;

            var bytes = new byte[4];

            bytes[2] = (byte)(dataUshort1 % 256);
            bytes[3] = (byte)((dataUshort1 >> 8) % 256);
            bytes[0] = (byte)(dataUshort2 % 256);
            bytes[1] = (byte)((dataUshort2 >> 8) % 256);

            return System.BitConverter.ToSingle(bytes, 0);
        }

        public void SetFloatReverseValue(int index, float value)
        {
            if (index >= RegisterData.Length - 1)
                throw new IndexOutOfRangeException();

            var bytes = BitConverter.GetBytes(value);

            RegisterData[index].RegisterValue =   (ushort)((bytes[1] << 8) + bytes[0]);
            RegisterData[index+1].RegisterValue = (ushort)((bytes[2] << 8) + bytes[3]);
        }

        public int GetIntValue(int index)
        {
            if (index >= RegisterData.Length - 1)
                throw new IndexOutOfRangeException();

            var dataUshort1 = RegisterData[index].RegisterValue;
            var dataUshort2 = RegisterData[index + 1].RegisterValue;

            return (dataUshort1 << 16) + dataUshort2;
        }

        public void SetIntValue(int index, int value)
        {
            if (index >= RegisterData.Length - 1)
                throw new IndexOutOfRangeException();

            var bytes = BitConverter.GetBytes(value);

            RegisterData[index].RegisterValue = (ushort)((bytes[1] << 8) + bytes[0]);
            RegisterData[index + 1].RegisterValue = (ushort)((bytes[2] << 8) + bytes[3]);
        }
    }
}
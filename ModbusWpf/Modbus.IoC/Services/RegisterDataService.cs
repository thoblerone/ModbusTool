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
    }
}
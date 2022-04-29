using System.Threading.Tasks;
using Modbus.Ioc.Interfaces;

namespace Modbus.Ioc.Models
{
    public class RegisterDataService : IRegisterDataService
    {
        public static int NumRegistersHeld = 65600;

        public RegisterDataService()
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
using System.Threading.Tasks;
using Catel.Data;

namespace ModbusWpf.Common.Helpers
{
    public interface IRegisterDataService
    {
        RegisterDataValue[] RegisterData { get; set; }
    }


    public class RegisterDataValue : ModelBase
    {
        public RegisterDataValue(ushort value)
        {
            RegisterValue = value;
        }

        public delegate void RegisterValueChangedHandler(ushort oldValue, ushort newValue);
        public event RegisterValueChangedHandler RegisterValueChanged;

        private ushort _registerValue;

        public ushort RegisterValue
        {
            get => _registerValue;
            set
            {
                if (value != _registerValue)
                {
                    var oldValue = _registerValue;

                    _registerValue = value;

                    var handler = RegisterValueChanged;
                    handler?.Invoke(oldValue, value);
                }
            }
        }
    }

    public class RegisterDataService : IRegisterDataService
    {
        public static int NumRegistersHeld = 65600;

        public RegisterDataService()
        {
            RegisterData = new RegisterDataValue[NumRegistersHeld];

            Parallel.For(0, NumRegistersHeld, i =>
            {
                RegisterData[i] = new RegisterDataValue((ushort)i);
            });
        }

        public RegisterDataValue[] RegisterData { get; set; }
    }
}
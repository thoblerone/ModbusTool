using System.Threading.Tasks;
using Catel.Data;

namespace ModbusWpf.Common.Helpers
{
    public interface IRegisterDataService
    {
        IRegisterDataValue[] RegisterData { get; set; }
        ushort this[int index] { get; set; }
    }

    public delegate void RegisterValueChangedHandler(ushort oldValue, ushort newValue);

    public interface IRegisterDataValue
    {
        event RegisterValueChangedHandler RegisterValueChanged;
        ushort RegisterValue { get; set; }
        string ToString();
    }

    public class RegisterDataValue : ModelBase, IRegisterDataValue
    {
        public RegisterDataValue(ushort value)
        {
            RegisterValue = value;
        }

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
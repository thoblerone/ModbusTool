using Catel.Data;
using Modbus.Ioc.Interfaces;

namespace Modbus.Ioc.Models
{
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
}

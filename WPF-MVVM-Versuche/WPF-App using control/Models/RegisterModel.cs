using System;
using System.ComponentModel;
using Catel.Data;
using Modbus.Common;
using WPF_App_using_control.Helpers;


namespace WPF_App_using_control.Models
{
    public enum RepresentationKinds
    {
        FloatReverse,
        Led,
        Binary,
        Hex,
        Integer,
        TODISCUSS_Float,
        TODISCUSS_Text
    }


    /// <summary>
    /// This class represents the information
    /// how to display the register data
    /// </summary>
    public class RegisterDisplayModel : ModelBase
    {
        private int _registerNumber;

        public RegisterDisplayModel(IRegisterDataService registerDataService, int registerNumber)
        {
            RegisterDataService = registerDataService;
            RegisterNumber = registerNumber;
            RegisterDataService.RegisterData[_registerNumber].RegisterValueChanged += OnRegisterValueChanged;
        }

        /// <summary>
        /// The lower register number for the current data representation
        /// (which actually may need more than one 16 bit register, as of float)
        /// </summary>
        public int RegisterNumber
        {
            get => _registerNumber;
            set
            {
                var oldValue = _registerNumber;
                if (value != oldValue)
                {
                    if (RegisterDataService is not null)
                    {
                        RegisterDataService.RegisterData[_registerNumber].RegisterValueChanged -= OnRegisterValueChanged;
                        if (RegisterNumber < RegisterDataService.RegisterData.Length - 1)
                            RegisterDataService.RegisterData[_registerNumber+1].RegisterValueChanged -= OnNextRegisterValueChanged;
                    }

                    _registerNumber = value;

                    if (RegisterDataService is not null)
                    {
                        RegisterDataService.RegisterData[_registerNumber].RegisterValueChanged += OnRegisterValueChanged;
                        if (RegisterNumber < RegisterDataService.RegisterData.Length - 1)
                            RegisterDataService.RegisterData[_registerNumber+1].RegisterValueChanged += OnNextRegisterValueChanged;
                    }
                }
            }
        }

        private void OnRegisterValueChanged(ushort oldValue, ushort newValue)
        {
            RaisePropertyChanged(nameof(StringRepresentation));
            RaisePropertyChanged(nameof(BoolValue));
            RaisePropertyChanged(nameof(TargetRegisterValue));
            RaisePropertyChanged(nameof(FloatReverseValue));
            RaisePropertyChanged(nameof(FloatValue));
        }

        // for floating point representations (requiring two registers)
        private void OnNextRegisterValueChanged(ushort oldValue, ushort newValue)
        {
            RaisePropertyChanged(nameof(StringRepresentation));
            RaisePropertyChanged(nameof(FloatReverseValue));
            RaisePropertyChanged(nameof(FloatValue));
        }

        public int CoilNumber { get; set; }

        public DisplayFormat RepresentationKind { get; set; }

        public IRegisterDataService RegisterDataService { get; }

        public override string ToString() => StringRepresentation;

        public string StringRepresentation
        {
            get
            {
                switch (RepresentationKind)
                {
                    case DisplayFormat.LED:
                        return $"{RepresentationKind}: {BoolValue}";

                    case DisplayFormat.Binary:
                        return $"{RepresentationKind}: {Convert.ToString(TargetRegisterValue, 2).PadLeft(16, '0')}";

                    case DisplayFormat.Hex:
                        return $"{RepresentationKind}: {TargetRegisterValue:x4}";

                    case DisplayFormat.Integer:
                        return $"{RepresentationKind}: {TargetRegisterValue}";

                    case DisplayFormat.FloatReverse:
                        return $"{RepresentationKind}: {FloatReverseValue}";

                    //case DisplayFormat.TODO_Float:
                    //    break;
                    //case DisplayFormat.TODO_Text:
                    //    break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool BoolValue
        {
            get
            {
                ushort dataByte = RegisterDataService.RegisterData[RegisterNumber].RegisterValue;

                return (dataByte & (1 << CoilNumber)) != 0;
            }
            set
            {
                if (value)
                {
                    RegisterDataService.RegisterData[RegisterNumber].RegisterValue |= (ushort) (1 << CoilNumber);
                }
                else
                {
                    RegisterDataService.RegisterData[RegisterNumber].RegisterValue &= (ushort) ~(1 << CoilNumber);
                }
                //RaisePropertyChanged(nameof(StringRepresentation));
                //RaisePropertyChanged(nameof(TargetRegisterValue));
            }
        }

        public ushort TargetRegisterValue
        {
            get => RegisterDataService.RegisterData[RegisterNumber].RegisterValue;
            set => RegisterDataService.RegisterData[RegisterNumber].RegisterValue = value;
        }

        public float FloatValue
        {
            get
            {
                if (RegisterNumber >= RegisterDataService.RegisterData.Length-1)
                    return float.NaN;

                ushort dataUshort1 = RegisterDataService.RegisterData[RegisterNumber].RegisterValue;
                ushort dataUshort2 = RegisterDataService.RegisterData[RegisterNumber + 1].RegisterValue;

                var bytes = new byte[4];

                bytes[0] = (byte) (dataUshort1 % 255);
                bytes[1] = (byte) ((dataUshort1 >> 8) % 255);
                bytes[2] = (byte) (dataUshort2 % 255);
                bytes[3] = (byte) ((dataUshort2 >> 8) % 255);

                return System.BitConverter.ToSingle(bytes, 0);
            }
        }

        public float FloatReverseValue
        {
            get
            {
                if (RegisterNumber >= RegisterDataService.RegisterData.Length - 1)
                    return float.NaN;
                ushort dataUshort1 = RegisterDataService.RegisterData[RegisterNumber].RegisterValue;
                ushort dataUshort2 = RegisterDataService.RegisterData[RegisterNumber + 1].RegisterValue;

                var bytes = new byte[4];

                bytes[2] = (byte) (dataUshort1 % 256);
                bytes[3] = (byte) ((dataUshort1 >> 8) % 256);
                bytes[0] = (byte) (dataUshort2 % 256);
                bytes[1] = (byte) ((dataUshort2 >> 8) % 256);

                return System.BitConverter.ToSingle(bytes, 0);
            }
        }
    }
}

using System;
using System.Globalization;
using Catel.Data;
using Modbus.Common;
using ModbusWpf.Common.Helpers;

namespace ModbusWpf.Common.Models
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
        /// (which actually may need consecutive registers, as of float)
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
                        if (RegisterDataService.RegisterData.Length < value + 1)
                            throw new IndexOutOfRangeException();

                        // remove register value changed handlers
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
            switch (RepresentationKind)
            {
                case DisplayFormat.LED:
                    RaisePropertyChanged(nameof(BoolValue));
                    break;
                case DisplayFormat.Binary:
                    RaisePropertyChanged(nameof(BinaryString));
                    break;
                case DisplayFormat.Hex:
                    RaisePropertyChanged(nameof(HexString));
                    break;
                case DisplayFormat.Integer:
                    RaisePropertyChanged(nameof(TargetRegisterValue));
                    break;
                case DisplayFormat.FloatReverse:
                    RaisePropertyChanged(nameof(FloatReverseString));
                    break;
            }
            RaisePropertyChanged(nameof(StringRepresentation));
            //RaisePropertyChanged(nameof(FloatString));
        }

        // for floating point representations (requiring two registers)
        private void OnNextRegisterValueChanged(ushort oldValue, ushort newValue)
        {
            if (RepresentationKind == DisplayFormat.FloatReverse)
            {
                RaisePropertyChanged(nameof(StringRepresentation));
                RaisePropertyChanged(nameof(FloatReverseString));
                //RaisePropertyChanged(nameof(FloatString));
            }
        }

        public int CoilNumber { get; set; }

        public DisplayFormat RepresentationKind { get; set; }

        public IRegisterDataService RegisterDataService { get; }

        public override string ToString() => StringRepresentation;

        public string StringRepresentation
        {
            get
            {
                return RepresentationKind switch
                {
                    DisplayFormat.LED => $"{RepresentationKind}: {BoolValue}",
                    DisplayFormat.Binary => $"{RepresentationKind}: {Convert.ToString(TargetRegisterValue, 2).PadLeft(16, '0')}",
                    DisplayFormat.Hex => $"{RepresentationKind}: {TargetRegisterValue:x4}",
                    DisplayFormat.Integer => $"{RepresentationKind}: {TargetRegisterValue}",
                    DisplayFormat.FloatReverse => $"{RepresentationKind}: {FloatReverseString}",
                    //case DisplayFormat.TODO_Float:
                    //    break;
                    //case DisplayFormat.TODO_Text:
                    //    break;
                    _ => throw new ArgumentOutOfRangeException(),
                };
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
            }
        }

        public ushort TargetRegisterValue
        {
            get => RegisterDataService.RegisterData[RegisterNumber].RegisterValue;
            set => RegisterDataService.RegisterData[RegisterNumber].RegisterValue = value;
        }

        public string FloatString
        {
            get
            {
                if (RegisterNumber >= RegisterDataService.RegisterData.Length-1)
                    return float.NaN.ToString();

                ushort dataUshort1 = RegisterDataService.RegisterData[RegisterNumber].RegisterValue;
                ushort dataUshort2 = RegisterDataService.RegisterData[RegisterNumber + 1].RegisterValue;

                var bytes = new byte[4];

                bytes[0] = (byte) (dataUshort1 % 255);
                bytes[1] = (byte) ((dataUshort1 >> 8) % 255);
                bytes[2] = (byte) (dataUshort2 % 255);
                bytes[3] = (byte) ((dataUshort2 >> 8) % 255);

                return System.BitConverter.ToSingle(bytes, 0).ToString("e3");
            }
            set
            {
                if (RegisterNumber >= RegisterDataService.RegisterData.Length - 1)
                    throw new IndexOutOfRangeException();

                if (!float.TryParse(value, NumberStyles.Any, CultureInfo.CurrentUICulture, out var fVal))
                    fVal = 0;

                var bytes = BitConverter.GetBytes(fVal);

                RegisterDataService.RegisterData[RegisterNumber].RegisterValue = (ushort)((bytes[1] << 8) + bytes[0]);
                RegisterDataService.RegisterData[RegisterNumber + 1].RegisterValue = (ushort)((bytes[3] << 8) + bytes[2]);
            }
        }

        public string FloatReverseString
        {
            get
            {
                if (RegisterNumber >= RegisterDataService.RegisterData.Length - 1)
                    return float.NaN.ToString();

                ushort dataUshort1 = RegisterDataService.RegisterData[RegisterNumber].RegisterValue;
                ushort dataUshort2 = RegisterDataService.RegisterData[RegisterNumber + 1].RegisterValue;

                var bytes = new byte[4];

                bytes[2] = (byte) (dataUshort1 % 256);
                bytes[3] = (byte) ((dataUshort1 >> 8) % 256);
                bytes[0] = (byte) (dataUshort2 % 256);
                bytes[1] = (byte) ((dataUshort2 >> 8) % 256);

                return System.BitConverter.ToSingle(bytes, 0).ToString("f3",  CultureInfo.CurrentUICulture);
            }
            set
            {
                if (RegisterNumber >= RegisterDataService.RegisterData.Length - 1)
                    throw new IndexOutOfRangeException();

                if (!float.TryParse(value, out var fVal))
                    fVal = 0;

                var bytes = BitConverter.GetBytes(fVal);

                RegisterDataService.RegisterData[RegisterNumber].RegisterValue = (ushort) ((bytes[3] << 8) + bytes[2]);
                RegisterDataService.RegisterData[RegisterNumber + 1].RegisterValue = (ushort) ((bytes[1] << 8) + bytes[0]);
            }
        }

        public string BinaryString
        {
            get => Convert.ToString(RegisterDataService.RegisterData[RegisterNumber].RegisterValue, 2).PadLeft(16, '0');
            set => RegisterDataService.RegisterData[RegisterNumber].RegisterValue = Convert.ToUInt16(value, 2);
        }
        public string HexString
        {
            get => RegisterDataService.RegisterData[RegisterNumber].RegisterValue.ToString("x4");
            set => RegisterDataService.RegisterData[RegisterNumber].RegisterValue = Convert.ToUInt16(value, 16);
        }
    }
}

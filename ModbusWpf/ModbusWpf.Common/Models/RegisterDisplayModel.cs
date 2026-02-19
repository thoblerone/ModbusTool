using System;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using Catel.Data;
using Modbus.Ioc.Interfaces;
using ModbusWpf.Common.Helpers;

namespace ModbusWpf.Common.Models
{
    /// <summary>
    /// This class represents the information
    /// how to display the register data
    /// </summary>
    public class RegisterDisplayModel : ModelBase
    {
        private int _registerNumber;

        public RegisterDisplayModel(IModbusRegisterDataService modbusRegisterDataService, int registerNumber)
        {
            ModbusRegisterDataService = modbusRegisterDataService;
            RegisterNumber = registerNumber;
            ModbusRegisterDataService.RegisterData[_registerNumber].RegisterValueChanged += OnRegisterValueChanged;
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
                    if (ModbusRegisterDataService is not null)
                    {
                        if (ModbusRegisterDataService.RegisterData.Length < value + 1)
                            throw new IndexOutOfRangeException();

                        // remove register value changed handlers
                        ModbusRegisterDataService.RegisterData[_registerNumber].RegisterValueChanged -= OnRegisterValueChanged;

                        if (RegisterNumber < ModbusRegisterDataService.RegisterData.Length - 1)
                            ModbusRegisterDataService.RegisterData[_registerNumber+1].RegisterValueChanged -= OnNextRegisterValueChanged;
                    }

                    _registerNumber = value;

                    if (ModbusRegisterDataService is not null)
                    {
                        ModbusRegisterDataService.RegisterData[_registerNumber].RegisterValueChanged += OnRegisterValueChanged;
                        if (RegisterNumber < ModbusRegisterDataService.RegisterData.Length - 1)
                            ModbusRegisterDataService.RegisterData[_registerNumber+1].RegisterValueChanged += OnNextRegisterValueChanged;
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
                case DisplayFormat.UInt16:
                    RaisePropertyChanged(nameof(TargetRegisterValue));
                    break;
                case DisplayFormat.FloatReverse:
                    RaisePropertyChanged(nameof(FloatReverseString));
                    break;
                //TODO: Questionable
                //case DisplayFormat.Float:
                //    RaisePropertyChanged(nameof(FloatString));
                //    break;
                case DisplayFormat.Int32:
                    RaisePropertyChanged(nameof(Int32String));
                    break;
                case DisplayFormat.Text:
                    RaisePropertyChanged(nameof(TextString));
                    break;
            }
            RaisePropertyChanged(nameof(StringRepresentation));
            //RaisePropertyChanged(nameof(FloatString));
        }

        // for floating point representations (requiring two registers)
        private void OnNextRegisterValueChanged(ushort oldValue, ushort newValue)
        {
            // TODO: Questionable
            if (RepresentationKind is DisplayFormat.FloatReverse /*or DisplayFormat.Float*/ or DisplayFormat.Int32)
            {
                RaisePropertyChanged(nameof(StringRepresentation));
                RaisePropertyChanged(nameof(FloatReverseString));
                // TODO: Questionable
                //RaisePropertyChanged(nameof(FloatString));
                RaisePropertyChanged(nameof(Int32String));
            }
        }

        public int CoilNumber { get; set; }

        public DisplayFormat RepresentationKind { get; set; }

        public IModbusRegisterDataService ModbusRegisterDataService { get; }

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
                    DisplayFormat.UInt16 => $"{RepresentationKind}: {TargetRegisterValue}",
                    DisplayFormat.FloatReverse => $"{RepresentationKind}: {FloatReverseString}",
                    //TODO: Questionable
                    //DisplayFormat.Float => $"{RepresentationKind}: {FloatString}",
                    DisplayFormat.Int32=> $"{RepresentationKind}: {Int32String}",
                    DisplayFormat.Text=> $"{RepresentationKind}: {TextString}",
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
                ushort dataByte = ModbusRegisterDataService[RegisterNumber];

                return (dataByte & (1 << CoilNumber)) != 0;
            }
            set
            {
                if (value)
                {
                    ModbusRegisterDataService[RegisterNumber] |= (ushort) (1 << CoilNumber);
                }
                else
                {
                    ModbusRegisterDataService[RegisterNumber] &= (ushort) ~(1 << CoilNumber);
                }
            }
        }

        public ushort TargetRegisterValue
        {
            get => ModbusRegisterDataService[RegisterNumber];
            set => ModbusRegisterDataService[RegisterNumber] = value;
        }

        // TODO: Questionable 
        //public string FloatString
        //{
        //    get
        //    {
        //        if (RegisterNumber >= ModbusRegisterDataService.RegisterData.Length-1)
        //            return float.NaN.ToString();

        //        ushort dataUshort1 = ModbusRegisterDataService[RegisterNumber];
        //        ushort dataUshort2 = ModbusRegisterDataService[RegisterNumber + 1];

        //        var bytes = new byte[4];

        //        bytes[0] = (byte) (dataUshort1 % 256);
        //        bytes[1] = (byte) ((dataUshort1 >> 8) % 256);
        //        bytes[2] = (byte) (dataUshort2 % 256);
        //        bytes[3] = (byte) ((dataUshort2 >> 8) % 256);

        //        return System.BitConverter.ToSingle(bytes, 0).ToString("e3");
        //    }
        //    set
        //    {
        //        if (RegisterNumber >= ModbusRegisterDataService.RegisterData.Length - 1)
        //            throw new IndexOutOfRangeException();

        //        if (!float.TryParse(value, NumberStyles.Any, CultureInfo.CurrentUICulture, out var fVal))
        //            fVal = 0;

        //        var bytes = BitConverter.GetBytes(fVal);

        //        ModbusRegisterDataService[RegisterNumber] = (ushort)((bytes[1] << 8) + bytes[0]);
        //        ModbusRegisterDataService[RegisterNumber + 1] = (ushort)((bytes[3] << 8) + bytes[2]);
        //    }
        //}

        public string FloatReverseString
        {
            get
            {
                if (RegisterNumber >= ModbusRegisterDataService.RegisterData.Length - 1)
                    return float.NaN.ToString();

                ushort dataUshort1 = ModbusRegisterDataService[RegisterNumber];
                ushort dataUshort2 = ModbusRegisterDataService[RegisterNumber + 1];

                var bytes = new byte[4];

                bytes[2] = (byte) (dataUshort1 % 256);
                bytes[3] = (byte) ((dataUshort1 >> 8) % 256);
                bytes[0] = (byte) (dataUshort2 % 256);
                bytes[1] = (byte) ((dataUshort2 >> 8) % 256);

                return System.BitConverter.ToSingle(bytes, 0).ToString("f3",  CultureInfo.CurrentUICulture);
            }
            set
            {
                if (RegisterNumber >= ModbusRegisterDataService.RegisterData.Length - 1)
                    throw new IndexOutOfRangeException();

                if (!float.TryParse(value, out var fVal))
                {
                    // special handling for +/- Infinity and NaN
                    var valueUpperTrimmed = value.Trim().ToUpperInvariant();
                    if (valueUpperTrimmed.Equals(CultureInfo.CurrentUICulture.NumberFormat.PositiveInfinitySymbol.ToUpperInvariant()))
                    {
                        fVal = float.PositiveInfinity;
                    }
                    else if(valueUpperTrimmed.Equals(CultureInfo.CurrentUICulture.NumberFormat.NegativeInfinitySymbol.ToUpperInvariant()))
                    {
                        fVal = float.NegativeInfinity;
                    }
                    else if (valueUpperTrimmed.Equals(CultureInfo.CurrentUICulture.NumberFormat.NaNSymbol.ToUpperInvariant()))
                    {
                        fVal = float.NaN;
                    }
                    else
                    {
                        fVal = 0;
                    }
                }

                var bytes = BitConverter.GetBytes(fVal);

                ModbusRegisterDataService[RegisterNumber] = (ushort) ((bytes[3] << 8) + bytes[2]);
                ModbusRegisterDataService[RegisterNumber + 1] = (ushort) ((bytes[1] << 8) + bytes[0]);
            }
        }

        public string Int32String
        {
            get
            {
                ushort dataUshort1 = ModbusRegisterDataService[RegisterNumber];
                ushort dataUshort2 = ModbusRegisterDataService[RegisterNumber + 1];

                var bytes = new byte[4];

                bytes[0] = (byte)(dataUshort2 % 256);
                bytes[1] = (byte)((dataUshort2 >> 8) % 256);
                bytes[2] = (byte)(dataUshort1 % 256);
                bytes[3] = (byte)((dataUshort1 >> 8) % 256);

                return System.BitConverter.ToInt32(bytes, 0).ToString();
            }
            set
            {
                if (RegisterNumber >= ModbusRegisterDataService.RegisterData.Length - 1)
                    throw new IndexOutOfRangeException();

                if (!int.TryParse(value, NumberStyles.Any, CultureInfo.CurrentUICulture, out var iVal))
                    iVal = 0;

                var bytes = BitConverter.GetBytes(iVal);

                ModbusRegisterDataService[RegisterNumber] = (ushort)((bytes[3] << 8) + bytes[2]);
                ModbusRegisterDataService[RegisterNumber + 1] = (ushort)((bytes[1] << 8) + bytes[0]);
            }
        }

        public string BinaryString
        {
            get => Convert.ToString(ModbusRegisterDataService[RegisterNumber], 2).PadLeft(16, '0');
            set => ModbusRegisterDataService[RegisterNumber] = Convert.ToUInt16(value, 2);
        }
        public string HexString
        {
            get => ModbusRegisterDataService[RegisterNumber].ToString("x4");
            set => ModbusRegisterDataService[RegisterNumber] = Convert.ToUInt16(value, 16);
        }

        public string TextString
        {
            get
            {
                var c = (char) ModbusRegisterDataService[RegisterNumber];
                if (c != (char) 0)
                {
                    return c.ToString();
                }
                else
                {
                    return "\\0";
                }
            }
            set
            {
                var c = (char)0;
                if (!string.IsNullOrEmpty(value) && value.Length >= 1 && !"\\0".Equals(value))
                    c = value[0];

                ModbusRegisterDataService[RegisterNumber] = Convert.ToUInt16(c);
            }
        }
    }
}

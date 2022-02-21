using System;
using System.Windows.Data;
using Modbus.Common;

namespace ModbusWpf.Common.Converters
{
    /// <summary>
    /// Can be used to select multiple radiobuttons
    /// according to a specific enum value
    ///
    /// .Convert returns bool true/false, if value and converterParameter match
    /// .ConvertBack returns the converterParameter if the value is true, else Binding.DoNothing
    ///  </summary>
    /// <example>
    ///   <RadioButton IsChecked="{Binding SomeEnumTypeProptery, ConverterParameter={x:Static entities:SomeEnum.SomeValue}, Converter={StaticResource EnumToBoolean}, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" GroupName="RadioButtonGroup1" Content="Radio Button Label"/>
    /// </example>
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
        }
    }
}
using System;
using System.Globalization;
using System.Windows.Data;
using Modbus.Common;
using ModbusWpf.Common.ViewModels;

namespace ModbusWpf.Common.Converters
{
    public class StartAddressDisplayFormatToHeaderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 1)
                return "[uups1 ...]";

            var displayFormat = values[0] as DisplayFormat?;

            if (values.Length < 2 || values[1] is not int startAddress)
                return "[uups2 ...]";

            if (displayFormat is null)
                return "...";

            return startAddress.ToString();

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
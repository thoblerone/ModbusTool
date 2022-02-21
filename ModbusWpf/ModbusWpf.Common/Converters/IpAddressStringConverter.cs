using System;
using System.Globalization;
using System.Net;
using System.Windows.Data;

namespace ModbusWpf.Common.Converters
{
    public class IpAddressStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not IPAddress address)
            {
                throw new ArgumentException("must specify a property of type IPAddress", nameof(value));
            }

            return address.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value as string))
                return IPAddress.None;

            if (!IPAddress.TryParse(value as string, out var address))
                throw new ArgumentException($"Could not parse '{value}' as type IPAddress", nameof(value));

            return address;
        }
    }
}
using System;
using System.Globalization;
using System.Windows.Data;
using Modbus.Common;

namespace ModbusWpf.Common.Converters
{
    public class StartAddressDisplayFormatToHeaderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 1)
            {
                /* this MultiBinding converter should specify two parameters like so:
                    <TextBlock><TextBlock.Text>
                        <MultiBinding Converter="{StaticResource StartAddressDisplayFormatToHeaderConverter}" UpdateSourceTrigger="PropertyChanged">
                            <Binding Path="DisplayFormat" UpdateSourceTrigger="PropertyChanged"/>
                            <Binding Path="StartAddress" UpdateSourceTrigger="PropertyChanged"/>
                        </MultiBinding>
                    </TextBlock.Text></TextBlock>
                */
                return "[uups1 ...]";
            }

            var displayFormat = values[0] as DisplayFormat?;
            if (displayFormat is null)
            {
                // the first parameter is not of enum type DisplayFormat
                return "...";
            }

            if (values.Length < 2 || values[1] is not int startAddress)
            {
                // the second argument type is not int
                return "[uups2 ...]";
            }

            return startAddress.ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
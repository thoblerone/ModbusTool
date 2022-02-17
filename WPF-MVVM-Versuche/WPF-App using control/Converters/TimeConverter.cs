using System;
using System.Globalization;
using System.Windows.Data;

namespace ModbusWpf.Common.Converters
{
    [ValueConversion(typeof(DateTime), typeof(string))]
    [Obsolete("Code is jst an placeholder example")]
    public class WeekdayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime date = (DateTime)value;
            return date.DayOfWeek.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using Modbus.Common;
using ModbusWpf.Common.Models;

namespace ModbusWpf.Common.Helpers
{
    class RegisterDisplayTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LedTemplate { get; set; }
        public DataTemplate BinaryTemplate { get; set; }
        public DataTemplate FloatReverseTemplate { get; set; }
        public DataTemplate HexTemplate { get; set; }
        public DataTemplate IntegerTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var model = item as RegisterDisplayModel;
            if (model is null) 
                return IntegerTemplate;

            return model.RepresentationKind switch
            {
                DisplayFormat.LED => LedTemplate,
                DisplayFormat.Binary => BinaryTemplate,
                DisplayFormat.FloatReverse => FloatReverseTemplate,
                DisplayFormat.Hex => HexTemplate,
                DisplayFormat.Integer => IntegerTemplate,
                _ => IntegerTemplate
            };
        }
    }
}

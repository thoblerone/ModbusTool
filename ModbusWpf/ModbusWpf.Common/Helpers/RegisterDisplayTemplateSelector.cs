using System.Windows;
using System.Windows.Controls;
using ModbusWpf.Common.Models;

namespace ModbusWpf.Common.Helpers
{
    class RegisterDisplayTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LedTemplate { get; set; }
        public DataTemplate BinaryTemplate { get; set; }
        public DataTemplate FloatReverseTemplate { get; set; }
        public DataTemplate FloatTemplate { get; set; }
        public DataTemplate HexTemplate { get; set; }
        public DataTemplate UshortTemplate { get; set; }

        public DataTemplate Int32Template { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var model = item as RegisterDisplayModel;
            if (model is null) 
                return UshortTemplate;

            return model.RepresentationKind switch
            {
                DisplayFormat.LED => LedTemplate,
                DisplayFormat.Binary => BinaryTemplate,
                DisplayFormat.FloatReverse => FloatReverseTemplate,
                //DisplayFormat.Float=> FloatTemplate,
                DisplayFormat.Hex => HexTemplate,
                DisplayFormat.UInt16 => UshortTemplate,
                DisplayFormat.Int32 => Int32Template,
                _ => UshortTemplate
            };
        }
    }
}

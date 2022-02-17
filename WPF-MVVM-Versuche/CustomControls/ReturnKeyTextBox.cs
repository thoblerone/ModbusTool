using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ModbusWpf.CustomControl
{
    /// <summary>
    /// A TextBox control that updates its source
    /// when the Enter key is pressed
    /// </summary>
    public class ReturnKeyTextBox : TextBox
    {
        public ReturnKeyTextBox()
        {
            // defaults for layout convenience
            VerticalContentAlignment = VerticalAlignment.Center;
            HorizontalContentAlignment = HorizontalAlignment.Right;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.Return)
            {
                GetBindingExpression(TextProperty)?.UpdateSource();
            }
        }
    }
}

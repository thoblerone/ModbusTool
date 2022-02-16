using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TemplatedCustomControl
{
    public class ReturnKeyTextBox : TextBox
    {
        public ReturnKeyTextBox()
        {
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

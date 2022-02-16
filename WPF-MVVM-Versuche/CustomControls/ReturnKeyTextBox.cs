using System.Windows.Controls;
using System.Windows.Input;

namespace TemplatedCustomControl
{
    public class ReturnKeyTextBox : TextBox
    {
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

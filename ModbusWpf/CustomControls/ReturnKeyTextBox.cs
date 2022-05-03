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

            LostFocus += (sender, args) => IsInEditMode = false;
        }

        /// <summary>
        /// Dependency property to get/set flag that the editing mode is active 
        /// </summary>  
        public static readonly DependencyProperty IsInEditModeProperty =
            DependencyProperty.Register(nameof(IsInEditMode), typeof(bool), typeof(ReturnKeyTextBox));
        public bool IsInEditMode
        {
            get => (bool)GetValue(IsInEditModeProperty);
            set
            {
                SetValue(IsInEditModeProperty, value);

                FontStyle = value ? FontStyles.Italic : FontStyles.Normal;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.Return)
            {
                GetBindingExpression(TextProperty)?.UpdateSource();
                    IsInEditMode = false;
            }
            else if (e.Key != Key.Tab)
            {
                IsInEditMode = true;
            }
        }
    }
}

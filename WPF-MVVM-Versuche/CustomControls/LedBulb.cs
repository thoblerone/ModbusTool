using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TemplatedCustomControl
{ 
    public class LedBulb : CheckBox
    {
        static LedBulb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LedBulb), new FrameworkPropertyMetadata(typeof(LedBulb)));
        }

        public static readonly DependencyProperty CheckedColorProperty =
            DependencyProperty.Register(nameof(CheckedColor), typeof(Brush), typeof(LedBulb), new PropertyMetadata(Brushes.Green));

        public Brush CheckedColor
        {
            get { return (Brush)GetValue(CheckedColorProperty); }
            set { SetValue(CheckedColorProperty, value); }
        }

        public static readonly DependencyProperty UncheckedColorProperty =
            DependencyProperty.Register(nameof(UncheckedColor), typeof(Brush), typeof(LedBulb), new PropertyMetadata(Brushes.Red));

        public Brush UncheckedColor
        {
            get { return (Brush)GetValue(UncheckedColorProperty); }
            set { SetValue(UncheckedColorProperty, value); }
        }
    }
}
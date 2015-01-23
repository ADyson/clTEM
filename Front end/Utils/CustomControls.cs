using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


// Basically taken from http://dutton.me.uk/2013/07/25/how-to-select-all-wpf-textbox-text-on-focus-using-an-attached-behavior/
namespace SimulationGUI.Utils
{
    public class TextBoxBehavior
    {
        public static bool GetSelectAllOnFocus(TextBox textBox)
        {
            return (bool)textBox.GetValue(SelectAllOnFocusProperty);
        }

        public static void SetSelectAllOnFocus(TextBox textBox, bool value)
        {
            textBox.SetValue(SelectAllOnFocusProperty, value);
        }

        public static readonly DependencyProperty SelectAllOnFocusProperty =
            DependencyProperty.RegisterAttached(
                "SelectAllOnFocus",
                typeof(bool),
                typeof(TextBoxBehavior),
                new UIPropertyMetadata(false, OnSelectAllOnFocusChanged));

        private static void OnSelectAllOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBox = d as TextBox;
            if (textBox == null) return;

            if (e.NewValue is bool == false) return;

            if ((bool)e.NewValue)
                textBox.GotFocus += SelectAll;
            else
                textBox.GotFocus -= SelectAll;
        }

        private static void SelectAll(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox == null) return;
            // Simple test to allow us to still use the mouse to drag and select.
            if (Mouse.LeftButton != MouseButtonState.Pressed)
                textBox.SelectAll();
        }
    }
}
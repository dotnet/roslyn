using System.Windows;
using System.Windows.Controls;

namespace MSBuildWorkspaceTester.Behaviors
{
    public static class TextBoxBehaviors
    {
        public static readonly DependencyProperty AlwaysScrollToEndProperty =
            DependencyProperty.RegisterAttached(
                name: "AlwaysScrollToEnd",
                propertyType: typeof(bool),
                ownerType: typeof(TextBoxBehaviors),
                defaultMetadata: new UIPropertyMetadata(
                    defaultValue: false,
                    propertyChangedCallback: OnAlwaysScrollToEndChanged));

        public static bool GetAlwaysScrollToEnd(DependencyObject obj)
            => (bool)obj.GetValue(AlwaysScrollToEndProperty);

        public static void SetAlwaysScrollToEnd(DependencyObject obj, bool value)
            => obj.SetValue(AlwaysScrollToEndProperty, value);

        private static void OnAlwaysScrollToEndChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.TextChanged += OnTextChanged;
                }
                else
                {
                    textBox.TextChanged -= OnTextChanged;
                }

                void OnTextChanged(object sender, TextChangedEventArgs args) => textBox.ScrollToEnd();
            }
        }
    }
}

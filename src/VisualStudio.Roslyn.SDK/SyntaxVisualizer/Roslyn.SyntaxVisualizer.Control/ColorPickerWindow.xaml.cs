using System;
using System.Windows;
using System.Windows.Media;

namespace Roslyn.SyntaxVisualizer.Control
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow.xaml
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        public Color Color { get; private set; }

        public ColorPickerWindow(Color color)
        {
            InitializeComponent();

            ColorPicker.OriginalColor = color;
            ColorPicker.Color = color;

            // Focus the color picker control on load
            Loaded += (s, e) => ColorPicker.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Color = ColorPicker.Color;
        }
    }
}

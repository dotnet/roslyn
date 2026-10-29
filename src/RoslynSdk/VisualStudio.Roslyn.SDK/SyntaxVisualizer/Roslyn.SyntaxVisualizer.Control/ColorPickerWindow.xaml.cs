// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Input;
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

            // Focus the first tab item when the window loads
            Loaded += (s, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Roslyn.SyntaxVisualizer.Control
{
    /// <summary>
    /// Interaction logic for ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color),
            typeof(Color),
            typeof(ColorPicker),
            new PropertyMetadata(Colors.Red, OnColorChanged));

        public static readonly DependencyProperty OriginalColorProperty = DependencyProperty.Register(
            nameof(OriginalColor),
            typeof(Color),
            typeof(ColorPicker),
            new PropertyMetadata(Colors.Red, OnOriginalColorChanged));

        public ColorPicker()
        {
            var vm = new ColorPickerViewModel();
            DataContext = vm;
            InitializeComponent();

            vm.PropertyChanged += (s, a) =>
            {
                switch (a.PropertyName)
                {
                    case nameof(ColorPickerViewModel.Color):
                        Color = vm.Color;
                        break;

                    case nameof(ColorPickerViewModel.OriginalColor):
                        OriginalColor = vm.OriginalColor;
                        break;
                }
            };
        }

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public Color OriginalColor
        {
            get => (Color)GetValue(OriginalColorProperty);
            set => SetValue(OriginalColorProperty, value);
        }

        private void SelectAllText(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private static void OnColorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker)o;
            var vm = (ColorPickerViewModel)colorPicker.DataContext;
            vm.Color = (Color)e.NewValue;
        }

        private static void OnOriginalColorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker)o;
            var vm = (ColorPickerViewModel)colorPicker.DataContext;
            vm.OriginalColor = (Color)e.NewValue;
        }
    }
}

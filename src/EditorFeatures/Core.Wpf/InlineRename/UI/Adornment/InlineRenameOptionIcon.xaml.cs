// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.InlineRename.Adornment
{
    /// <summary>
    /// Interaction logic for InlineRenameOptionIcon.xaml
    /// </summary>
    internal partial class InlineRenameOptionIcon : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty MonikerProperty = DependencyProperty.Register(
            "Moniker",
            typeof(ImageMoniker),
            typeof(InlineRenameOptionIcon));

        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            "IsSelected",
            typeof(bool),
            typeof(InlineRenameOptionIcon));

        public event EventHandler? SelectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsSelected
        {
            get => (bool)GetValue(SelectedProperty);
            set
            {
                if (value != IsSelected)
                {
                    SetValue(SelectedProperty, value);
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public ImageMoniker Moniker
        {
            get => (ImageMoniker)GetValue(MonikerProperty);
            set => SetValue(MonikerProperty, value);
        }

        public InlineRenameOptionIcon()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void ToggleSelect(object sender, RoutedEventArgs e)
        {
            this.IsSelected = !this.IsSelected;
        }
    }
}

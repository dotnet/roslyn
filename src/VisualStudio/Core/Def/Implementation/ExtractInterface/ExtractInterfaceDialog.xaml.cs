// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface
{
    /// <summary>
    /// Interaction logic for ExtractInterfaceDialog.xaml
    /// </summary>
    internal partial class ExtractInterfaceDialog : DialogWindow
    {
        private readonly ExtractInterfaceDialogViewModel _viewModel;

        /// <summary>
        /// For test purposes only. The integration tests need to know when the dialog is up and
        /// ready for automation.
        /// </summary>
        internal static event Action TEST_DialogLoaded;

        // Expose localized strings for binding
        public string ExtractInterfaceDialogTitle { get { return ServicesVSResources.Extract_Interface; } }
        public string NewInterfaceName { get { return ServicesVSResources.New_interface_name_colon; } }
        public string GeneratedName { get { return ServicesVSResources.Generated_name_colon; } }
        public string NewFileName { get { return ServicesVSResources.New_file_name_colon; } }
        public string SelectPublicMembersToFormInterface { get { return ServicesVSResources.Select_public_members_to_form_interface; } }
        public string SelectAll { get { return ServicesVSResources.Select_All; } }
        public string DeselectAll { get { return ServicesVSResources.Deselect_All; } }
        public string OK { get { return ServicesVSResources.OK; } }
        public string Cancel { get { return ServicesVSResources.Cancel; } }

        // Use C# Extract Interface helpTopic for C# and VB.
        internal ExtractInterfaceDialog(ExtractInterfaceDialogViewModel viewModel)
            : base(helpTopic: "vs.csharp.refactoring.extractinterface")
        {
            _viewModel = viewModel;
            SetCommandBindings();

            InitializeComponent();
            DataContext = viewModel;

            Loaded += ExtractInterfaceDialog_Loaded;
            IsVisibleChanged += ExtractInterfaceDialog_IsVisibleChanged;
        }

        private void ExtractInterfaceDialog_Loaded(object sender, RoutedEventArgs e)
        {
            interfaceNameTextBox.Focus();
            interfaceNameTextBox.SelectAll();
        }

        private void ExtractInterfaceDialog_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                IsVisibleChanged -= ExtractInterfaceDialog_IsVisibleChanged;
                TEST_DialogLoaded?.Invoke();
            }
        }

        private void SetCommandBindings()
        {
            CommandBindings.Add(new CommandBinding(
                new RoutedCommand(
                    "SelectAllClickCommand",
                    typeof(ExtractInterfaceDialog),
                    new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.S, ModifierKeys.Alt) })),
                Select_All_Click));

            CommandBindings.Add(new CommandBinding(
                new RoutedCommand(
                    "DeselectAllClickCommand",
                    typeof(ExtractInterfaceDialog),
                    new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.D, ModifierKeys.Alt) })),
                Deselect_All_Click));
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.TrySubmit())
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Select_All_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectAll();
        }

        private void Deselect_All_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DeselectAll();
        }

        private void SelectAllInTextBox(object sender, RoutedEventArgs e)
        {
            TextBox textbox = e.OriginalSource as TextBox;
            if (textbox != null && Mouse.LeftButton == MouseButtonState.Released)
            {
                textbox.SelectAll();
            }
        }

        private void OnListViewPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
                ToggleCheckSelection();
                e.Handled = true;
            }
        }

        private void OnListViewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ToggleCheckSelection();
                e.Handled = true;
            }
        }

        private void ToggleCheckSelection()
        {
            var selectedItems = Members.SelectedItems.OfType<ExtractInterfaceDialogViewModel.MemberSymbolViewModel>().ToArray();
            var allChecked = selectedItems.All(m => m.IsChecked);
            foreach (var item in selectedItems)
            {
                item.IsChecked = !allChecked;
            }
        }
    }
}

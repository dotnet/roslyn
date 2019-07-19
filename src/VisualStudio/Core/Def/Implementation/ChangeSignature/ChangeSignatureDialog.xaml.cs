// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    /// <summary>
    /// Interaction logic for ExtractInterfaceDialog.xaml
    /// </summary>
    internal partial class ChangeSignatureDialog : DialogWindow
    {
        private readonly ChangeSignatureDialogViewModel _viewModel;

        // Expose localized strings for binding
        public string ChangeSignatureDialogTitle { get { return ServicesVSResources.Change_Signature; } }
        public string Parameters { get { return ServicesVSResources.Parameters_colon2; } }
        public string PreviewMethodSignature { get { return ServicesVSResources.Preview_method_signature_colon; } }
        public string PreviewReferenceChanges { get { return ServicesVSResources.Preview_reference_changes; } }
        public string Remove { get { return ServicesVSResources.Re_move; } }
        public string Restore { get { return ServicesVSResources.Restore; } }
        public string OK { get { return ServicesVSResources.OK; } }
        public string Cancel { get { return ServicesVSResources.Cancel; } }

        public Brush ParameterText { get; }
        public Brush RemovedParameterText { get; }
        public Brush DisabledParameterForeground { get; }
        public Brush DisabledParameterBackground { get; }
        public Brush StrikethroughBrush { get; }

        // Use C# Reorder Parameters helpTopic for C# and VB.
        internal ChangeSignatureDialog(ChangeSignatureDialogViewModel viewModel)
            : base(helpTopic: "vs.csharp.refactoring.reorder")
        {
            _viewModel = viewModel;

            InitializeComponent();

            // Set these headers explicitly because binding to DataGridTextColumn.Header is not
            // supported.
            modifierHeader.Header = ServicesVSResources.Modifier;
            defaultHeader.Header = ServicesVSResources.Default_;
            typeHeader.Header = ServicesVSResources.Type;
            parameterHeader.Header = ServicesVSResources.Parameter;

            ParameterText = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : new SolidColorBrush(Color.FromArgb(0xFF, 0x1E, 0x1E, 0x1E));
            RemovedParameterText = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : new SolidColorBrush(Colors.Gray);
            DisabledParameterBackground = SystemParameters.HighContrast ? SystemColors.WindowBrush : new SolidColorBrush(Color.FromArgb(0xFF, 0xDF, 0xE7, 0xF3));
            DisabledParameterForeground = SystemParameters.HighContrast ? SystemColors.GrayTextBrush : new SolidColorBrush(Color.FromArgb(0xFF, 0xA2, 0xA4, 0xA5));
            Members.Background = SystemParameters.HighContrast ? SystemColors.WindowBrush : new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
            StrikethroughBrush = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : new SolidColorBrush(Colors.Red);

            DataContext = viewModel;

            Loaded += ChangeSignatureDialog_Loaded;
        }

        private void ChangeSignatureDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Members.Focus();
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

        private void MoveUp_Click(object sender, EventArgs e)
        {
            var oldSelectedIndex = Members.SelectedIndex;
            if (_viewModel.CanMoveUp && oldSelectedIndex >= 0)
            {
                _viewModel.MoveUp();
                Members.Items.Refresh();
                Members.SelectedIndex = oldSelectedIndex - 1;
            }

            SetFocusToSelectedRow();
        }

        private void MoveDown_Click(object sender, EventArgs e)
        {
            var oldSelectedIndex = Members.SelectedIndex;
            if (_viewModel.CanMoveDown && oldSelectedIndex >= 0)
            {
                _viewModel.MoveDown();
                Members.Items.Refresh();
                Members.SelectedIndex = oldSelectedIndex + 1;
            }

            SetFocusToSelectedRow();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CanRemove)
            {
                _viewModel.Remove();
                Members.Items.Refresh();
            }

            SetFocusToSelectedRow();
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CanRestore)
            {
                _viewModel.Restore();
                Members.Items.Refresh();
            }

            SetFocusToSelectedRow();
        }

        private void SetFocusToSelectedRow()
        {
            if (Members.SelectedIndex >= 0)
            {
                if (!(Members.ItemContainerGenerator.ContainerFromIndex(Members.SelectedIndex) is DataGridRow row))
                {
                    Members.ScrollIntoView(Members.SelectedItem);
                    row = Members.ItemContainerGenerator.ContainerFromIndex(Members.SelectedIndex) as DataGridRow;
                }

                if (row != null)
                {
                    FocusRow(row);
                }
            }
        }

        private void FocusRow(DataGridRow row)
        {
            var cell = row.FindDescendant<DataGridCell>();
            if (cell != null)
            {
                cell.Focus();
            }
        }

        private void MoveSelectionUp_Click(object sender, EventArgs e)
        {
            var oldSelectedIndex = Members.SelectedIndex;
            if (oldSelectedIndex > 0)
            {
                var potentialNewSelectedParameter = Members.Items[oldSelectedIndex - 1] as ChangeSignatureDialogViewModel.ParameterViewModel;
                if (!potentialNewSelectedParameter.IsDisabled)
                {
                    Members.SelectedIndex = oldSelectedIndex - 1;
                }
            }

            SetFocusToSelectedRow();
        }

        private void MoveSelectionDown_Click(object sender, EventArgs e)
        {
            var oldSelectedIndex = Members.SelectedIndex;
            if (oldSelectedIndex >= 0 && oldSelectedIndex < Members.Items.Count - 1)
            {
                Members.SelectedIndex = oldSelectedIndex + 1;
            }

            SetFocusToSelectedRow();
        }

        private void Members_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Members.SelectedIndex == -1)
            {
                Members.SelectedIndex = _viewModel.GetStartingSelectionIndex();
            }

            SetFocusToSelectedRow();
        }

        private void ToggleRemovedState(object sender, ExecutedRoutedEventArgs e)
        {
            if (_viewModel.CanRemove)
            {
                _viewModel.Remove();
            }
            else if (_viewModel.CanRestore)
            {
                _viewModel.Restore();
            }

            Members.Items.Refresh();
            SetFocusToSelectedRow();
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly ChangeSignatureDialog _dialog;

            public TestAccessor(ChangeSignatureDialog dialog)
            {
                _dialog = dialog;
            }

            public ChangeSignatureDialogViewModel ViewModel => _dialog._viewModel;

            public DataGrid Members => _dialog.Members;

            public DialogButton OKButton => _dialog.OKButton;

            public DialogButton CancelButton => _dialog.CancelButton;

            public DialogButton DownButton => _dialog.DownButton;

            public DialogButton UpButton => _dialog.UpButton;

            public DialogButton RemoveButton => _dialog.RemoveButton;

            public DialogButton RestoreButton => _dialog.RestoreButton;
        }
    }
}

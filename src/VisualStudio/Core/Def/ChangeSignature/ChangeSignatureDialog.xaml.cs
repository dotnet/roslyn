// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;

/// <summary>
/// Interaction logic for ChangeSignatureDialog.xaml
/// </summary>
internal partial class ChangeSignatureDialog : DialogWindow
{
    private readonly ChangeSignatureDialogViewModel _viewModel;

    // Expose localized strings for binding
    public static string ChangeSignatureDialogTitle { get { return ServicesVSResources.Change_Signature; } }
    public static string CurrentParameter { get { return ServicesVSResources.Current_parameter; } }
    public static string Parameters { get { return ServicesVSResources.Parameters_colon2; } }
    public static string PreviewMethodSignature { get { return ServicesVSResources.Preview_method_signature_colon; } }
    public static string PreviewReferenceChanges { get { return ServicesVSResources.Preview_reference_changes; } }
    public static string Remove { get { return ServicesVSResources.Re_move; } }
    public static string Restore { get { return ServicesVSResources.Restore; } }
    public static string Add { get { return ServicesVSResources.Add; } }
    public static string OK { get { return ServicesVSResources.OK; } }
    public static string Cancel { get { return ServicesVSResources.Cancel; } }
    public static string WarningTypeDoesNotBind { get { return ServicesVSResources.Warning_colon_type_does_not_bind; } }
    public static string WarningDuplicateParameterName { get { return ServicesVSResources.Warning_colon_duplicate_parameter_name; } }

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
        callsiteHeader.Header = ServicesVSResources.Callsite;
        indexHeader.Header = ServicesVSResources.Index;

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
        => Members.Focus();

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.TrySubmit())
        {
            DialogResult = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;

    private void MoveUp_Click(object sender, EventArgs e)
    {
        MoveUp_UpdateSelectedIndex();
        SetFocusToSelectedRow(false);
    }

    private void MoveUp_Click_FocusRow(object sender, EventArgs e)
    {
        MoveUp_UpdateSelectedIndex();
        SetFocusToSelectedRow(true);
    }

    private void MoveUp_UpdateSelectedIndex()
    {
        var oldSelectedIndex = Members.SelectedIndex;
        if (_viewModel.CanMoveUp && oldSelectedIndex >= 0)
        {
            _viewModel.MoveUp();
            Members.Items.Refresh();
            Members.SelectedIndex = oldSelectedIndex - 1;
        }
    }

    private void MoveDown_Click(object sender, EventArgs e)
    {
        MoveDown_UpdateSelectedIndex();
        SetFocusToSelectedRow(false);
    }

    private void MoveDown_Click_FocusRow(object sender, EventArgs e)
    {
        MoveDown_UpdateSelectedIndex();
        SetFocusToSelectedRow(true);
    }

    private void MoveDown_UpdateSelectedIndex()
    {
        var oldSelectedIndex = Members.SelectedIndex;
        if (_viewModel.CanMoveDown && oldSelectedIndex >= 0)
        {
            _viewModel.MoveDown();
            Members.Items.Refresh();
            Members.SelectedIndex = oldSelectedIndex + 1;
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.CanRemove)
        {
            _viewModel.Remove();
            Members.Items.Refresh();
        }

        SetFocusToSelectedRow(true);
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.CanRestore)
        {
            _viewModel.Restore();
            Members.Items.Refresh();
        }

        SetFocusToSelectedRow(true);
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var addParameterViewModel = _viewModel.CreateAddParameterDialogViewModel();
        var dialog = new AddParameterDialog(addParameterViewModel);
        var result = dialog.ShowModal();

        ChangeSignatureLogger.LogAddParameterDialogLaunched();

        if (result.HasValue && result.Value)
        {
            ChangeSignatureLogger.LogAddParameterDialogCommitted();

            var addedParameter = new AddedParameter(
                addParameterViewModel.TypeSymbol,
                addParameterViewModel.TypeName,
                addParameterViewModel.ParameterName,
                GetCallSiteKind(addParameterViewModel),
                addParameterViewModel.IsCallsiteRegularValue ? addParameterViewModel.CallSiteValue : string.Empty,
                addParameterViewModel.IsRequired,
                addParameterViewModel.IsRequired ? string.Empty : addParameterViewModel.DefaultValue,
                addParameterViewModel.TypeBinds);

            _viewModel.AddParameter(addedParameter);
        }

        SetFocusToSelectedRow(false);
    }

    private static CallSiteKind GetCallSiteKind(AddParameterDialogViewModel addParameterViewModel)
    {
        if (addParameterViewModel.IsCallsiteInferred)
            return CallSiteKind.Inferred;

        if (addParameterViewModel.IsCallsiteOmitted)
            return CallSiteKind.Omitted;

        if (addParameterViewModel.IsCallsiteTodo)
            return CallSiteKind.Todo;

        Debug.Assert(addParameterViewModel.IsCallsiteRegularValue);

        return addParameterViewModel.UseNamedArguments
            ? CallSiteKind.ValueWithName
            : CallSiteKind.Value;
    }

    private void SetFocusToSelectedRow(bool focusRow)
    {
        if (Members.SelectedIndex >= 0)
        {
            if (Members.ItemContainerGenerator.ContainerFromIndex(Members.SelectedIndex) is not DataGridRow row)
            {
                Members.ScrollIntoView(Members.SelectedItem);
                row = Members.ItemContainerGenerator.ContainerFromIndex(Members.SelectedIndex) as DataGridRow;
            }

            if (row != null && focusRow)
            {
                // This line is required primarily for accessibility purposes to ensure the screenreader always
                // focuses on individual rows rather than the parent DataGrid.
                Members.UpdateLayout();

                FocusRow(row);
            }
        }
    }

    private static void FocusRow(DataGridRow row)
    {
        var cell = row.FindDescendant<DataGridCell>();
        cell?.Focus();
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

        SetFocusToSelectedRow(true);
    }

    private void MoveSelectionDown_Click(object sender, EventArgs e)
    {
        var oldSelectedIndex = Members.SelectedIndex;
        if (oldSelectedIndex >= 0 && oldSelectedIndex < Members.Items.Count - 1)
        {
            Members.SelectedIndex = oldSelectedIndex + 1;
        }

        SetFocusToSelectedRow(true);
    }

    private void Members_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (Members.CurrentItem != null)
        {
            // When it has a valid value, CurrentItem is generally more up-to-date than SelectedIndex.
            // For example, if the user clicks on an out of view item in the parameter list (i.e. the
            // parameter list is long and the user scrolls to click another parameter farther down/up
            // in the list), CurrentItem will update immediately while SelectedIndex will not.
            Members.SelectedIndex = Members.Items.IndexOf(Members.CurrentItem);
        }

        if (Members.SelectedIndex == -1)
        {
            Members.SelectedIndex = _viewModel.GetStartingSelectionIndex();
        }

        SetFocusToSelectedRow(true);
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
        SetFocusToSelectedRow(true);
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor
    {
        private readonly ChangeSignatureDialog _dialog;

        public TestAccessor(ChangeSignatureDialog dialog)
            => _dialog = dialog;

        public ChangeSignatureDialogViewModel ViewModel => _dialog._viewModel;

        public DataGrid Members => _dialog.Members;

        public DialogButton OKButton => _dialog.OKButton;

        public DialogButton CancelButton => _dialog.CancelButton;

        public DialogButton DownButton => _dialog.DownButton;

        public DialogButton UpButton => _dialog.UpButton;

        public DialogButton AddButton => _dialog.AddButton;

        public DialogButton RemoveButton => _dialog.RemoveButton;

        public DialogButton RestoreButton => _dialog.RestoreButton;
    }
}

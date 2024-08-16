// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace;

/// <summary>
/// Interaction logic for MoveToNamespaceDialog.xaml
/// </summary>
internal partial class MoveToNamespaceDialog : DialogWindow
{
    private readonly MoveToNamespaceDialogViewModel _viewModel;

    public string MoveToNamespaceDialogTitle => ServicesVSResources.Move_to_namespace;
    public string NamespaceLabelText => ServicesVSResources.Target_Namespace_colon;
    public string OK => ServicesVSResources.OK;
    public string Cancel => ServicesVSResources.Cancel;

    internal MoveToNamespaceDialog(MoveToNamespaceDialogViewModel viewModel)
        : base()
    {
        _viewModel = viewModel;

        // Set focus to first tab control when the window is loaded
        Loaded += (s, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

        InitializeComponent();
        DataContext = viewModel;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;

    internal TestAccessor GetTestAccessor() => new(this);

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.CanSubmit)
        {
            DialogResult = true;
        }
    }

    internal readonly struct TestAccessor
    {
        private readonly MoveToNamespaceDialog _dialog;
        public TestAccessor(MoveToNamespaceDialog dialog)
            => _dialog = dialog;

        public Button OKButton => _dialog.OKButton;
        public Button CancelButton => _dialog.CancelButton;
        public ComboBox NamespaceBox => _dialog.NamespaceBox;

    }
}
